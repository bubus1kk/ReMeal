using Application.DTOs.Lots;
using Application.DTOs.Users;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class LotService : ILotService
    {
        private readonly IFoodPointRepository _foodPointRepository;
        private readonly IFoodLotRepository _foodLotRepository;
        private readonly IAuthService _authService;

        public LotService(
            IFoodPointRepository foodPointRepository,
            IFoodLotRepository foodLotRepository,
            IAuthService authService)
        {
            _foodPointRepository = foodPointRepository;
            _foodLotRepository = foodLotRepository;
            _authService = authService;
        }

        public async Task<FoodLot> CreateLotAsync(
            CreateLotRequest request,
            CancellationToken cancellationToken = default)
        {
            var foodPoint = await GetOwnedFoodPointAsync(request.FoodPointId, cancellationToken);

            if (!foodPoint.IsActive)
                throw new InvalidOperationException("Нельзя создать лот для неактивной точки питания.");

            var lot = new FoodLot(
                foodPoint.Id,
                request.Title,
                request.Description,
                BuildLotComposition(request.Composition, request.Components),
                request.TotalQuantity,
                request.Price,
                request.PickupDeadline,
                request.ImagePath);

            if (request.Components is not null)
                lot.ReplaceComponents(CreateComponents(request.Components));

            await _foodLotRepository.AddAsync(lot, cancellationToken);
            await _foodLotRepository.SaveChangesAsync(cancellationToken);
            return lot;
        }

        public async Task<FoodLot> UpdateLotAsync(
            UpdateLotRequest request,
            CancellationToken cancellationToken = default)
        {
            var lot = await GetOwnedLotAsync(request.Id, cancellationToken);
            lot.Update(
                request.Title,
                request.Description,
                BuildLotComposition(request.Composition, request.Components),
                request.Price,
                request.PickupDeadline,
                request.ImagePath);

            if (request.Components is not null)
                lot.ReplaceComponents(CreateComponents(request.Components));

            await _foodLotRepository.UpdateAsync(lot, cancellationToken);
            await _foodLotRepository.SaveChangesAsync(cancellationToken);
            return lot;
        }

        public async Task DeleteLotAsync(Guid lotId, CancellationToken cancellationToken = default)
        {
            var lot = await GetOwnedLotAsync(lotId, cancellationToken);
            lot.Cancel();
            await _foodLotRepository.UpdateAsync(lot, cancellationToken);
            await _foodLotRepository.SaveChangesAsync(cancellationToken);
        }

        public Task<List<FoodLot>> GetAllLotsAsync(CancellationToken cancellationToken = default)
        {
            return _foodLotRepository.GetAllAsync(cancellationToken);
        }

        public async Task<List<FoodLot>> GetAvailableLotsAsync(CancellationToken cancellationToken = default)
        {
            await MarkExpiredLotsAsync(cancellationToken);

            var lots = await _foodLotRepository.GetAllAsync(cancellationToken);
            var now = DateTime.UtcNow;

            return lots
                .Where(x =>
                    x.Status == LotStatus.Active &&
                    x.AvailableQuantity > 0 &&
                    x.PickupDeadline > now &&
                    x.FoodPoint is { IsActive: true })
                .ToList();
        }

        public async Task<List<FoodLot>> GetCurrentPartnerLotsAsync(CancellationToken cancellationToken = default)
        {
            var partner = await GetCurrentPartnerAsync(cancellationToken);
            var foodPoints = await _foodPointRepository.GetByOwnerIdAsync(partner.Id, cancellationToken);
            if (foodPoints.Count == 0)
                return new List<FoodLot>();

            var lots = new List<FoodLot>();
            foreach (var foodPoint in foodPoints)
            {
                lots.AddRange(await _foodLotRepository.GetByFoodPointIdAsync(foodPoint.Id, cancellationToken));
            }

            return lots
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
        }

        public Task<List<FoodLot>> GetFoodPointLotsAsync(
            Guid foodPointId,
            CancellationToken cancellationToken = default)
        {
            return _foodLotRepository.GetByFoodPointIdAsync(foodPointId, cancellationToken);
        }

        public async Task<int> MarkExpiredLotsAsync(CancellationToken cancellationToken = default)
        {
            var candidates = await _foodLotRepository.GetCandidatesForExpirationAsync(cancellationToken);
            var updated = 0;

            foreach (var lot in candidates)
            {
                lot.MarkExpired();
                await _foodLotRepository.UpdateAsync(lot, cancellationToken);
                updated++;
            }

            if (updated > 0)
                await _foodLotRepository.SaveChangesAsync(cancellationToken);

            return updated;
        }

        public async Task<FoodLot?> GetCurrentPartnerLotAsync(Guid lotId, CancellationToken cancellationToken = default)
        {
            var lot = await _foodLotRepository.GetByIdAsync(lotId, cancellationToken);
            if (lot is null)
                return null;

            await EnsureLotBelongsToCurrentPartnerAsync(lot, cancellationToken);
            return lot;
        }

        public Task<FoodLot?> GetLotAsync(Guid lotId, CancellationToken cancellationToken = default)
        {
            return _foodLotRepository.GetByIdAsync(lotId, cancellationToken);
        }

        private async Task<FoodLot> GetOwnedLotAsync(Guid lotId, CancellationToken cancellationToken)
        {
            var lot = await _foodLotRepository.GetByIdAsync(lotId, cancellationToken)
                ?? throw new KeyNotFoundException($"Лот '{lotId}' не найден.");

            await EnsureLotBelongsToCurrentPartnerAsync(lot, cancellationToken);
            return lot;
        }

        private async Task EnsureLotBelongsToCurrentPartnerAsync(FoodLot lot, CancellationToken cancellationToken)
        {
            var partner = await GetCurrentPartnerAsync(cancellationToken);

            if (lot.FoodPoint?.OwnerId != partner.Id)
                throw new UnauthorizedAccessException("Нельзя управлять лотом чужой точки питания.");
        }

        private async Task<FoodPoint> GetOwnedFoodPointAsync(Guid foodPointId, CancellationToken cancellationToken)
        {
            var partner = await GetCurrentPartnerAsync(cancellationToken);
            var foodPoint = await _foodPointRepository.GetByIdAsync(foodPointId, cancellationToken)
                ?? throw new KeyNotFoundException($"Точка питания '{foodPointId}' не найдена.");

            if (foodPoint.OwnerId != partner.Id)
                throw new UnauthorizedAccessException("Нельзя создать лот для чужой точки питания.");

            return foodPoint;
        }

        private async Task<UserProfileDto> GetCurrentPartnerAsync(CancellationToken cancellationToken)
        {
            var user = await _authService.GetCurrentUserAsync(cancellationToken)
                ?? throw new UnauthorizedAccessException("Пользователь не авторизован.");

            if (user.Role != UserRole.FoodPointRepresentative)
                throw new UnauthorizedAccessException("Управление лотами доступно только партнеру.");

            return user;
        }

        private static List<LotComponent> CreateComponents(IReadOnlyList<LotComponentRequest> requests)
        {
            var components = new List<LotComponent>();
            var sortOrder = 0;

            foreach (var request in requests.OrderBy(x => x.SortOrder))
            {
                var hasText = !string.IsNullOrWhiteSpace(request.Name) ||
                    !string.IsNullOrWhiteSpace(request.Composition) ||
                    !string.IsNullOrWhiteSpace(request.ImagePath);

                if (!hasText)
                    continue;

                components.Add(new LotComponent(
                    request.Id,
                    request.Name,
                    request.Quantity,
                    request.Unit,
                    request.Composition,
                    request.ImagePath,
                    sortOrder));
                sortOrder++;
            }

            return components;
        }

        private static string BuildLotComposition(
            string composition,
            IReadOnlyList<LotComponentRequest>? componentRequests)
        {
            if (componentRequests is null)
                return string.IsNullOrWhiteSpace(composition) ? string.Empty : composition.Trim();

            return FoodLot.BuildLotComposition(CreateComponents(componentRequests));
        }
    }
}
