namespace Domain.Entities;

public sealed class BookingStatusReference
{
    private BookingStatusReference()
    {
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public ICollection<Booking> Bookings { get; private set; } = new List<Booking>();
}
