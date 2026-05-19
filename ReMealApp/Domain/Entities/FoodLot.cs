using ReMeal.Domain.Enums;

namespace ReMeal.Domain.Entities;

public class FoodLot
{
    public Guid Id { get; private set; }

    public Guid FoodPointId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string Composition { get; private set; } = string.Empty;

    public int TotalQuantity { get; private set; }

    public int AvailableQuantity { get; private set; }

    public decimal Price { get; private set; }

    public DateTime PickupDeadline { get; private set; }

    public LotStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public FoodPoint? FoodPoint { get; private set; }

    private FoodLot()
    {
    }

    public FoodLot(
        Guid foodPointId,
        string title,
        string description,
        string composition,
        int totalQuantity,
        decimal price,
        DateTime pickupDeadline)
    {
        ValidateCreation(foodPointId, totalQuantity, price, pickupDeadline);

        Id = Guid.NewGuid();
        FoodPointId = foodPointId;
        Title = title.Trim();
        Description = description.Trim();
        Composition = composition.Trim();
        TotalQuantity = totalQuantity;
        AvailableQuantity = totalQuantity;
        Price = price;
        PickupDeadline = pickupDeadline;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        RefreshStatus();
    }

    public void Update(
        string title,
        string description,
        string composition,
        decimal price,
        DateTime pickupDeadline)
    {
        if (Status is LotStatus.Cancelled or LotStatus.Expired)
            throw new InvalidOperationException("Не удается обновить отмененную партию или истек срок годности.");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Требуется название.", nameof(title));

        if (price < 0)
            throw new ArgumentException("Цена не может быть отрицательной.", nameof(price));

        if (pickupDeadline <= DateTime.UtcNow)
            throw new ArgumentException("Крайний срок доставки должен быть в будущем.", nameof(pickupDeadline));

        Title = title.Trim();
        Description = description.Trim();
        Composition = composition.Trim();
        Price = price;
        PickupDeadline = pickupDeadline;
        UpdatedAt = DateTime.UtcNow;
        RefreshStatus();
    }

    public void MarkExpired()
    {
        Status = LotStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecreaseQuantity(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Сумма должна быть положительной.");

        if (AvailableQuantity < amount)
            throw new InvalidOperationException("Недостаточно доступного количества.");

        AvailableQuantity -= amount;
        UpdatedAt = DateTime.UtcNow;
        RefreshStatus();
    }

    public void IncreaseQuantity(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Сумма должна быть положительной.");

        AvailableQuantity += amount;
        TotalQuantity += amount;
        UpdatedAt = DateTime.UtcNow;
        RefreshStatus();
    }

    internal static void ValidateCreation(
        Guid foodPointId,
        int totalQuantity,
        decimal price,
        DateTime pickupDeadline)
    {
        if (foodPointId == Guid.Empty)
            throw new ArgumentException("Требуется пункт питания.", nameof(foodPointId));

        if (totalQuantity <= 0)
            throw new ArgumentException("Количество должно быть больше нуля.", nameof(totalQuantity));

        if (price < 0)
            throw new ArgumentException("Цена не может быть отрицательной.", nameof(price));

        if (pickupDeadline <= DateTime.UtcNow)
            throw new ArgumentException("Крайний срок доставки должен быть в будущем.", nameof(pickupDeadline));
    }

    internal void RefreshStatus()
    {
        if (Status == LotStatus.Cancelled)
            return;

        if (PickupDeadline <= DateTime.UtcNow)
        {
            Status = LotStatus.Expired;
            return;
        }

        if (AvailableQuantity <= 0)
        {
            Status = LotStatus.SoldOut;
            return;
        }

        Status = LotStatus.Active;
    }
}
