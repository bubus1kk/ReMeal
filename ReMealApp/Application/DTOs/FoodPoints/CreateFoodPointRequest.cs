namespace ReMeal.Application.DTOs.FoodPoints;

public sealed record CreateFoodPointRequest(
    string Name,
    string Address,
    string Description,
    string Phone,
    Guid OwnerId);
