using Application.DTOs.Admin;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

namespace Application.Services
{
    public sealed class AdminService : IAdminService
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IFoodPointRepository _foodPointRepository;

        public AdminService(
            IAuthService authService,
            IUserRepository userRepository,
            IFoodPointRepository foodPointRepository)
        {
            _authService = authService;
            _userRepository = userRepository;
            _foodPointRepository = foodPointRepository;
        }

        public async Task EnsureAdministratorAccessAsync(CancellationToken cancellationToken = default)
        {
            var currentUser = await _authService.GetCurrentUserAsync(cancellationToken)
                ?? throw new UnauthorizedAccessException("Пользователь не авторизован.");

            if (currentUser.Role != UserRole.Administrator)
                throw new UnauthorizedAccessException("Административный модуль доступен только администратору.");
        }

        public async Task<List<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            await EnsureAdministratorAccessAsync(cancellationToken);

            var users = await _userRepository.GetAllAsync(cancellationToken);
            return users
                .Select(MapUser)
                .ToList();
        }

        private static AdminUserDto MapUser(User user)
        {
            return new AdminUserDto
            {
                Id = user.Id,
                Login = user.Login,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                IsActive = null
            };
        }

        public async Task<List<AdminFoodPointDto>> GetFoodPointsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureAdministratorAccessAsync(cancellationToken);

            var foodPoints = await _foodPointRepository.GetAllAsync(cancellationToken);
            return foodPoints
                .Select(MapFoodPoint)
                .ToList();
        }

        public async Task ActivateFoodPointAsync(Guid foodPointId, CancellationToken cancellationToken = default)
        {
            await EnsureAdministratorAccessAsync(cancellationToken);

            var foodPoint = await GetFoodPointOrThrowAsync(foodPointId, cancellationToken);
            foodPoint.Activate();

            await _foodPointRepository.UpdateAsync(foodPoint, cancellationToken);
            await _foodPointRepository.SaveChangesAsync(cancellationToken);
        }

        public async Task DeactivateFoodPointAsync(Guid foodPointId, CancellationToken cancellationToken = default)
        {
            await EnsureAdministratorAccessAsync(cancellationToken);

            var foodPoint = await GetFoodPointOrThrowAsync(foodPointId, cancellationToken);
            foodPoint.Deactivate();

            await _foodPointRepository.UpdateAsync(foodPoint, cancellationToken);
            await _foodPointRepository.SaveChangesAsync(cancellationToken);
        }

        private async Task<FoodPoint> GetFoodPointOrThrowAsync(Guid foodPointId, CancellationToken cancellationToken)
        {
            return await _foodPointRepository.GetByIdAsync(foodPointId, cancellationToken)
                ?? throw new KeyNotFoundException($"Точка питания '{foodPointId}' не найдена.");
        }

        private static AdminFoodPointDto MapFoodPoint(FoodPoint foodPoint)
        {
            return new AdminFoodPointDto
            {
                Id = foodPoint.Id,
                Name = foodPoint.Name,
                Address = foodPoint.Address,
                Phone = foodPoint.Phone,
                OwnerName = string.IsNullOrWhiteSpace(foodPoint.Owner?.FullName)
                    ? "Не указан"
                    : foodPoint.Owner.FullName,
                OwnerLogin = string.IsNullOrWhiteSpace(foodPoint.Owner?.Login)
                    ? string.Empty
                    : foodPoint.Owner.Login,
                IsActive = foodPoint.IsActive
            };
        }
    }
}
