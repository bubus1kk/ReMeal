using Domain.Enums;

namespace Application.DTOs.Booking;

public class BookingDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string LotTitle { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public DateTime BookingDate { get; set; }

    public BookingStatus Status { get; set; }
}