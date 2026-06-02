namespace Domain.Entities;

public sealed class LotStatusReference
{
    private LotStatusReference()
    {
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public ICollection<FoodLot> Lots { get; private set; } = new List<FoodLot>();
}
