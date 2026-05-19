namespace ReMeal.Application.DTOs.Lots;

public sealed record UpdateLotRequest(
    Guid Id,
    string Title,
    string Description,
    string Composition,
    decimal Price,
    DateTime PickupDeadline);
