namespace ReMeal.Desktop;

/// <summary>
/// Shared UI state for module navigation until auth and routing are integrated.
/// </summary>
public sealed class NavigationState
{
    public static readonly Guid DemoOwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid? SelectedFoodPointId { get; set; }

    public Guid? EditingLotId { get; set; }
}
