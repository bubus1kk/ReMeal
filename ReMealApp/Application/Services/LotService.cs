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
            var foodPoint = await GetCurrentPartnerFoodPointAsync(cancellationToken);

            if (!foodPoint.IsActive)
                throw new InvalidOperationException("Нельзя создать лот для неактивной точки питания.");

            var lot = new FoodLot(
                foodPoint.Id,
                request.Title,
                request.Description,
                request.Composition,
                request.TotalQuantity,
                request.Price,
                request.PickupDeadline);

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
                request.Composition,
                request.Price,
                request.PickupDeadline);

            await _foodLotRepository.UpdateAsync(lot, cancellationToken);
            await _foodLotRepository.SaveChangesAsync(cancellationToken);
            return lot;
        }

        public async Task DeleteLotAsync(Guid lotId, CancellationToken cancellationToken = default)
        {
            var lot = await GetOwnedLotAsync(lotId, cancellationToken);
            await _foodLotRepository.DeleteAsync(lot, cancellationToken);
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
            var foodPoint = await GetCurrentPartnerFoodPointOrNullAsync(cancellationToken);
            if (foodPoint is null)
                return new List<FoodLot>();

            return await _foodLotRepository.GetByFoodPointIdAsync(foodPoint.Id, cancellationToken);
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

        private async Task<FoodPoint> GetCurrentPartnerFoodPointAsync(CancellationToken cancellationToken)
        {
            var foodPoint = await GetCurrentPartnerFoodPointOrNullAsync(cancellationToken);
            return foodPoint ?? throw new InvalidOperationException("Сначала создайте точку питания партнера.");
        }

        private async Task<FoodPoint?> GetCurrentPartnerFoodPointOrNullAsync(CancellationToken cancellationToken)
        {
            var partner = await GetCurrentPartnerAsync(cancellationToken);
            return await _foodPointRepository.GetByOwnerIdAsync(partner.Id, cancellationToken);
        }

        private async Task<UserProfileDto> GetCurrentPartnerAsync(CancellationToken cancellationToken)
        {
            var user = await _authService.GetCurrentUserAsync(cancellationToken)
                ?? throw new UnauthorizedAccessException("Пользователь не авторизован.");

            if (user.Role != UserRole.FoodPointRepresentative)
                throw new UnauthorizedAccessException("Управление лотами доступно только партнеру.");

            return user;
        }
    }
}
