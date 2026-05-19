using Application.DTOs.FoodPoints;
using Application.DTOs.Users;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class FoodPointService : IFoodPointService
    {
        private readonly IFoodPointRepository _foodPointRepository;
        private readonly IAuthService _authService;

        public FoodPointService(IFoodPointRepository foodPointRepository, IAuthService authService)
        {
            _foodPointRepository = foodPointRepository;
            _authService = authService;
        }

        public async Task<FoodPoint> CreateFoodPointAsync(
            CreateFoodPointRequest request,
            CancellationToken cancellationToken = default)
        {
            var partner = await GetCurrentPartnerAsync(cancellationToken);
            var existingFoodPoint = await _foodPointRepository.GetByOwnerIdAsync(partner.Id, cancellationToken);

            if (existingFoodPoint is not null)
                throw new InvalidOperationException("У текущего партнера уже есть точка питания.");

            var foodPoint = new FoodPoint(
                request.Name,
                request.Address,
                request.Description,
                request.Phone,
                partner.Id);

            await _foodPointRepository.AddAsync(foodPoint, cancellationToken);
            await _foodPointRepository.SaveChangesAsync(cancellationToken);
            return foodPoint;
        }

        public async Task<FoodPoint> UpdateFoodPointAsync(
            UpdateFoodPointRequest request,
            CancellationToken cancellationToken = default)
        {
            var foodPoint = await GetOwnedFoodPointAsync(request.Id, cancellationToken);
            foodPoint.UpdateInformation(request.Name, request.Address, request.Description, request.Phone);
            await _foodPointRepository.UpdateAsync(foodPoint, cancellationToken);
            await _foodPointRepository.SaveChangesAsync(cancellationToken);
            return foodPoint;
        }

        public async Task DeactivateFoodPointAsync(
            Guid foodPointId,
            CancellationToken cancellationToken = default)
        {
            var foodPoint = await GetOwnedFoodPointAsync(foodPointId, cancellationToken);
            foodPoint.Deactivate();
            await _foodPointRepository.UpdateAsync(foodPoint, cancellationToken);
            await _foodPointRepository.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteFoodPointAsync(
            Guid foodPointId,
            CancellationToken cancellationToken = default)
        {
            var foodPoint = await GetOwnedFoodPointAsync(foodPointId, cancellationToken);
            await _foodPointRepository.DeleteAsync(foodPoint, cancellationToken);
            await _foodPointRepository.SaveChangesAsync(cancellationToken);
        }

        public async Task<FoodPoint?> GetCurrentPartnerFoodPointAsync(CancellationToken cancellationToken = default)
        {
            var partner = await GetCurrentPartnerAsync(cancellationToken);
            return await _foodPointRepository.GetByOwnerIdAsync(partner.Id, cancellationToken);
        }

        public Task<List<FoodPoint>> GetAllFoodPointsAsync(CancellationToken cancellationToken = default)
        {
            return _foodPointRepository.GetAllAsync(cancellationToken);
        }

        public Task<FoodPoint?> GetFoodPointAsync(
            Guid foodPointId,
            CancellationToken cancellationToken = default)
        {
            return _foodPointRepository.GetByIdAsync(foodPointId, cancellationToken);
        }

        private async Task<FoodPoint> GetOwnedFoodPointAsync(Guid foodPointId, CancellationToken cancellationToken)
        {
            var partner = await GetCurrentPartnerAsync(cancellationToken);
            var foodPoint = await _foodPointRepository.GetByIdAsync(foodPointId, cancellationToken)
                ?? throw new KeyNotFoundException($"Точка питания '{foodPointId}' не найдена.");

            if (foodPoint.OwnerId != partner.Id)
                throw new UnauthorizedAccessException("Нельзя управлять чужой точкой питания.");

            return foodPoint;
        }

        private async Task<UserProfileDto> GetCurrentPartnerAsync(CancellationToken cancellationToken)
        {
            var user = await _authService.GetCurrentUserAsync(cancellationToken)
                ?? throw new UnauthorizedAccessException("Пользователь не авторизован.");

            if (user.Role != UserRole.FoodPointRepresentative)
                throw new UnauthorizedAccessException("Управление точкой питания доступно только партнеру.");

            return user;
        }
    }
}
