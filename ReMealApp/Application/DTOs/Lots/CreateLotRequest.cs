namespace ReMeal.Application.DTOs.Lots;

public sealed record CreateLotRequest(
    Guid FoodPointId,
    string Title,
    string Description,
    string Composition,
    int TotalQuantity,
    decimal Price,
    DateTime PickupDeadline);
