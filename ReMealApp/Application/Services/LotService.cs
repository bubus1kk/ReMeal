using ReMeal.Application.DTOs.Lots;
using ReMeal.Application.Interfaces;
using ReMeal.Domain.Entities;
using ReMeal.Domain.Enums;

namespace ReMeal.Application.Services;

public class LotService : ILotService
{
    private readonly IFoodPointRepository _foodPointRepository;
    private readonly IFoodLotRepository _foodLotRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LotService(
        IFoodPointRepository foodPointRepository,
        IFoodLotRepository foodLotRepository,
        IUnitOfWork unitOfWork)
    {
        _foodPointRepository = foodPointRepository;
        _foodLotRepository = foodLotRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FoodLot> CreateLotAsync(
        CreateLotRequest request,
        CancellationToken cancellationToken = default)
    {
        var foodPoint = await _foodPointRepository.GetByIdAsync(request.FoodPointId, cancellationToken)
            ?? throw new KeyNotFoundException($"Точка питания '{request.FoodPointId}' не найдена.");

        if (!foodPoint.IsActive)
            throw new InvalidOperationException("Невозможно создать много места для неактивной точки питания.");

        var lot = new FoodLot(
            request.FoodPointId,
            request.Title,
            request.Description,
            request.Composition,
            request.TotalQuantity,
            request.Price,
            request.PickupDeadline);

        await _foodLotRepository.AddAsync(lot, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return lot;
    }

    public async Task<FoodLot> UpdateLotAsync(
        UpdateLotRequest request,
        CancellationToken cancellationToken = default)
    {
        var lot = await _foodLotRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Лот '{request.Id}' не найден.");

        lot.Update(
            request.Title,
            request.Description,
            request.Composition,
            request.Price,
            request.PickupDeadline);

        await _foodLotRepository.UpdateAsync(lot, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return lot;
    }

    public async Task DeleteLotAsync(Guid lotId, CancellationToken cancellationToken = default)
    {
        var lot = await _foodLotRepository.GetByIdAsync(lotId, cancellationToken)
            ?? throw new KeyNotFoundException($"Лот '{lotId}' не найден.");

        await _foodLotRepository.DeleteAsync(lot, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<List<FoodLot>> GetAllLotsAsync(CancellationToken cancellationToken = default)
    {
        return _foodLotRepository.GetAllAsync(cancellationToken);
    }

    public async Task<List<FoodLot>> GetAvailableLotsAsync(CancellationToken cancellationToken = default)
    {
        var lots = await _foodLotRepository.GetAllAsync(cancellationToken);
        var now = DateTime.UtcNow;

        return lots
            .Where(x =>
                x.Status == LotStatus.Active &&
                x.AvailableQuantity > 0 &&
                x.PickupDeadline > now)
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
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return updated;
    }

    public Task<FoodLot?> GetLotAsync(Guid lotId, CancellationToken cancellationToken = default)
    {
        return _foodLotRepository.GetByIdAsync(lotId, cancellationToken);
    }
}
