using ReMeal.Application.DTOs.FoodPoints;
using ReMeal.Application.Interfaces;
using ReMeal.Domain.Entities;

namespace ReMeal.Application.Services;

public class FoodPointService : IFoodPointService
{
    private readonly IFoodPointRepository _foodPointRepository;
    private readonly IUnitOfWork _unitOfWork;

    public FoodPointService(IFoodPointRepository foodPointRepository, IUnitOfWork unitOfWork)
    {
        _foodPointRepository = foodPointRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FoodPoint> CreateFoodPointAsync(
        CreateFoodPointRequest request,
        CancellationToken cancellationToken = default)
    {
        var foodPoint = new FoodPoint(
            request.Name,
            request.Address,
            request.Description,
            request.Phone,
            request.OwnerId);

        await _foodPointRepository.AddAsync(foodPoint, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return foodPoint;
    }

    public async Task<FoodPoint> UpdateFoodPointAsync(
        UpdateFoodPointRequest request,
        CancellationToken cancellationToken = default)
    {
        var foodPoint = await _foodPointRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Food point '{request.Id}' was not found.");

        foodPoint.UpdateInformation(request.Name, request.Address, request.Description, request.Phone);
        await _foodPointRepository.UpdateAsync(foodPoint, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return foodPoint;
    }

    public async Task DeactivateFoodPointAsync(
        Guid foodPointId,
        CancellationToken cancellationToken = default)
    {
        var foodPoint = await _foodPointRepository.GetByIdAsync(foodPointId, cancellationToken)
            ?? throw new KeyNotFoundException($"Food point '{foodPointId}' was not found.");

        foodPoint.Deactivate();
        await _foodPointRepository.UpdateAsync(foodPoint, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteFoodPointAsync(
        Guid foodPointId,
        CancellationToken cancellationToken = default)
    {
        var foodPoint = await _foodPointRepository.GetByIdAsync(foodPointId, cancellationToken)
            ?? throw new KeyNotFoundException($"Food point '{foodPointId}' was not found.");

        await _foodPointRepository.DeleteAsync(foodPoint, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
}
