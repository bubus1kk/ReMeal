namespace ReMeal.Desktop;

public sealed class NavigationState
{
    public static readonly Guid DemoOwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid? SelectedFoodPointId { get; set; }

    public Guid? EditingLotId { get; set; }
}
