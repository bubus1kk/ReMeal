using Domain.Enums;

namespace Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid FoodLotId { get; set; }

    public int Quantity { get; set; }

    public DateTime BookingDate { get; set; }

    public BookingStatus Status { get; set; }

    public User? User { get; set; }

    public FoodLot? FoodLot { get; set; }
}
