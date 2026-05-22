using Domain.Enums;

namespace Application.DTOs.Booking;

public class BookingDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string LotTitle { get; set; } = string.Empty;

    public string FoodPointName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal PriceAtReservation { get; set; }

    public decimal TotalPrice => PriceAtReservation * Quantity;

    public DateTime ReservedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? IssuedAt { get; set; }

    public BookingStatus Status { get; set; }

    public bool IsIssued { get; set; }

    public bool IsPending { get; set; }

    public bool IsCancelled { get; set; }

    public bool CanCancel => Status == BookingStatus.Active;

    public bool CanConfirmIssue => Status == BookingStatus.Active;

    public string StatusText => Status switch
    {
        BookingStatus.Active => "Активна",
        BookingStatus.Cancelled => "Отменена",
        BookingStatus.Issued => "Выдана",
        _ => Status.ToString()
    };
}