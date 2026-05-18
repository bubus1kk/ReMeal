namespace ReMeal.Application.DTOs.FoodPoints;

public sealed record UpdateFoodPointRequest(
    Guid Id,
    string Name,
    string Address,
    string Description,
    string Phone);
