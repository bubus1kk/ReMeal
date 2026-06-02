using Domain.Enums;

namespace Domain.Entities;

public class Booking
{
    private Booking()
    {
    }

    public Booking(
        Guid userId,
        Guid foodLotId,
        int quantity,
        decimal priceAtReservation,
        DateTime reservedAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Требуется пользователь.", nameof(userId));

        if (foodLotId == Guid.Empty)
            throw new ArgumentException("Требуется лот.", nameof(foodLotId));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Количество должно быть положительным.");

        if (priceAtReservation <= 0)
            throw new ArgumentOutOfRangeException(nameof(priceAtReservation), "Цена должна быть больше нуля.");

        Id = Guid.NewGuid();
        UserId = userId;
        FoodLotId = foodLotId;
        Quantity = quantity;
        PriceAtReservation = priceAtReservation;
        ReservedAt = reservedAt;
        StatusId = (int)BookingStatus.Active;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid FoodLotId { get; private set; }

    public int Quantity { get; private set; }

    public decimal PriceAtReservation { get; private set; }

    public int StatusId { get; private set; }

    public BookingStatus Status
    {
        get => (BookingStatus)StatusId;
        private set => StatusId = (int)value;
    }

    public DateTime ReservedAt { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    public DateTime? IssuedAt { get; private set; }

    public User? User { get; private set; }

    public FoodLot? FoodLot { get; private set; }

    public BookingStatusReference? StatusReference { get; private set; }

    public void Cancel(DateTime cancelledAt)
    {
        if (Status != BookingStatus.Active)
            throw new InvalidOperationException("Отменить можно только активное бронирование.");

        StatusId = (int)BookingStatus.Cancelled;
        CancelledAt = cancelledAt;
    }

    public void MarkIssued(DateTime issuedAt)
    {
        if (Status != BookingStatus.Active)
            throw new InvalidOperationException("Подтвердить выдачу можно только для активного бронирования.");

        StatusId = (int)BookingStatus.Issued;
        IssuedAt = issuedAt;
    }
}
