using Domain.Enums;

namespace Application.DTOs.Admin
{
    public sealed class AdminBookingDto
    {
        public Guid Id { get; init; }

        public string CustomerName { get; init; } = string.Empty;

        public string LotTitle { get; init; } = string.Empty;

        public string FoodPointName { get; init; } = string.Empty;

        public int Quantity { get; init; }

        public decimal PriceAtReservation { get; init; }

        public decimal TotalPrice => PriceAtReservation * Quantity;

        public DateTime ReservedAt { get; init; }

        public BookingStatus Status { get; init; }

        public string StatusText => Status switch
        {
            BookingStatus.Active => "Активна",
            BookingStatus.Cancelled => "Отменена",
            BookingStatus.Issued => "Выдана",
            _ => Status.ToString()
        };
    }
}
