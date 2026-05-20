using Application.DTOs.Profile;
using Application.Interfaces;
using Domain.Enums;
using Domain.Repositories;

namespace Application.Services
{
    public sealed class ProfileStatisticsService : IProfileStatisticsService
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IFoodPointRepository _foodPointRepository;
        private readonly IFoodLotRepository _foodLotRepository;

        public ProfileStatisticsService(
            IAuthService authService,
            IUserRepository userRepository,
            IFoodPointRepository foodPointRepository,
            IFoodLotRepository foodLotRepository)
        {
            _authService = authService;
            _userRepository = userRepository;
            _foodPointRepository = foodPointRepository;
            _foodLotRepository = foodLotRepository;
        }

        public async Task<PartnerProfileStatisticsDto> GetCurrentPartnerStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var currentUser = await _authService.GetCurrentUserAsync(cancellationToken)
                ?? throw new UnauthorizedAccessException("Пользователь не авторизован.");

            if (currentUser.Role != UserRole.FoodPointRepresentative)
                throw new UnauthorizedAccessException("Статистика партнера доступна только партнеру.");

            var foodPoints = await _foodPointRepository.GetByOwnerIdAsync(currentUser.Id, cancellationToken);
            var pointStatistics = foodPoints
                .Select(MapFoodPointStatistics)
                .ToList();

            var activeLots = pointStatistics.Sum(x => x.ActiveLots);
            var totalLots = pointStatistics.Sum(x => x.TotalLots);
            var availableSets = pointStatistics.Sum(x => x.AvailableSets);

            return new PartnerProfileStatisticsDto
            {
                FoodPoints = pointStatistics,
                Kpis = new List<ProfileKpiDto>
                {
                    new("Моих точек", foodPoints.Count.ToString("N0"), "Точки питания партнера"),
                    new("Активных точек", foodPoints.Count(x => x.IsActive).ToString("N0"), "Готовы публиковать наборы"),
                    new("Активных лотов", activeLots.ToString("N0"), "По всем точкам"),
                    new("Опубликовано лотов", totalLots.ToString("N0"), "За все время"),
                    new("Доступно наборов", availableSets.ToString("N0"), "По активным лотам")
                }
            };
        }

        public async Task<AdminProfileStatisticsDto> GetAdminStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var currentUser = await _authService.GetCurrentUserAsync(cancellationToken)
                ?? throw new UnauthorizedAccessException("Пользователь не авторизован.");

            if (currentUser.Role != UserRole.Administrator)
                throw new UnauthorizedAccessException("Системная сводка доступна только администратору.");

            var users = await _userRepository.GetAllAsync(cancellationToken);
            var foodPoints = await _foodPointRepository.GetAllAsync(cancellationToken);
            var lots = await _foodLotRepository.GetAllAsync(cancellationToken);
            var activeLots = lots.Count(IsActiveLot);

            return new AdminProfileStatisticsDto
            {
                Kpis = new List<ProfileKpiDto>
                {
                    new("Пользователей", users.Count.ToString("N0"), "Всего аккаунтов"),
                    new("Партнеров", users.Count(x => x.Role == UserRole.FoodPointRepresentative).ToString("N0"), "Аккаунты представителей"),
                    new("Точек питания", foodPoints.Count.ToString("N0"), "Всего в системе"),
                    new("Активных лотов", activeLots.ToString("N0"), "Доступны в каталоге")
                },
                RoleDistribution = BuildRoleDistribution(users)
            };
        }

        private static PartnerFoodPointStatisticsDto MapFoodPointStatistics(Domain.Entities.FoodPoint foodPoint)
        {
            var lots = foodPoint.Lots.ToList();

            return new PartnerFoodPointStatisticsDto
            {
                FoodPointId = foodPoint.Id,
                Name = foodPoint.Name,
                Address = foodPoint.Address,
                Description = foodPoint.Description,
                Phone = foodPoint.Phone,
                IsActive = foodPoint.IsActive,
                CreatedAt = foodPoint.CreatedAt,
                TotalLots = lots.Count,
                ActiveLots = lots.Count(IsActiveLot),
                SoldOutLots = lots.Count(x => x.Status == LotStatus.SoldOut),
                ExpiredLots = lots.Count(x => x.Status == LotStatus.Expired),
                CancelledLots = lots.Count(x => x.Status == LotStatus.Cancelled),
                AvailableSets = lots.Where(IsActiveLot).Sum(x => x.AvailableQuantity)
            };
        }

        private static List<AdminRoleDistributionDto> BuildRoleDistribution(IReadOnlyCollection<Domain.Entities.User> users)
        {
            return Enum.GetValues<UserRole>()
                .Select(role =>
                {
                    var count = users.Count(x => x.Role == role);
                    return new AdminRoleDistributionDto
                    {
                        Role = role,
                        RoleText = GetRoleText(role),
                        Count = count,
                        Percentage = users.Count == 0 ? 0 : Math.Round(count * 100d / users.Count, 1)
                    };
                })
                .ToList();
        }

        private static bool IsActiveLot(Domain.Entities.FoodLot lot)
        {
            return lot.Status == LotStatus.Active &&
                lot.AvailableQuantity > 0 &&
                lot.PickupDeadline > DateTime.UtcNow &&
                lot.FoodPoint is null or { IsActive: true };
        }

        private static string GetRoleText(UserRole role)
        {
            return role switch
            {
                UserRole.StudentCustomer => "Покупатели",
                UserRole.FoodPointRepresentative => "Партнеры",
                UserRole.Administrator => "Администраторы",
                _ => role.ToString()
            };
        }
    }
}
