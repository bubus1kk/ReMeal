namespace ReMeal.Domain.Entities;

public class FoodPoint
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public Guid OwnerId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool IsActive { get; private set; }

    public ICollection<FoodLot> Lots { get; private set; } = new List<FoodLot>();

    private FoodPoint()
    {
    }

    public FoodPoint(
        string name,
        string address,
        string description,
        string phone,
        Guid ownerId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required.", nameof(address));

        if (ownerId == Guid.Empty)
            throw new ArgumentException("OwnerId is required.", nameof(ownerId));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Address = address.Trim();
        Description = description.Trim();
        Phone = phone.Trim();
        OwnerId = ownerId;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public void UpdateInformation(
        string name,
        string address,
        string description,
        string phone)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot update a deactivated food point.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required.", nameof(address));

        Name = name.Trim();
        Address = address.Trim();
        Description = description.Trim();
        Phone = phone.Trim();
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
