namespace Domain.Entities
{
    public class FoodPoint
    {
        public Guid Id { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string Address { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public string Phone { get; private set; } = string.Empty;

        public double? Latitude { get; private set; }

        public double? Longitude { get; private set; }

        public Guid OwnerId { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public bool IsActive { get; private set; }

        public User? Owner { get; private set; }

        public ICollection<FoodLot> Lots { get; private set; } = new List<FoodLot>();

        private FoodPoint()
        {
        }

        public FoodPoint(
            string name,
            string address,
            string description,
            string phone,
            Guid ownerId,
            double? latitude = null,
            double? longitude = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Требуется указать название.", nameof(name));

            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Требуется указать адрес.", nameof(address));

            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Требуется указать телефон.", nameof(phone));

            if (ownerId == Guid.Empty)
                throw new ArgumentException("Требуется id владельца.", nameof(ownerId));

            ValidateCoordinates(latitude, longitude);

            Id = Guid.NewGuid();
            Name = name.Trim();
            Address = address.Trim();
            Description = description.Trim();
            Phone = phone.Trim();
            Latitude = latitude;
            Longitude = longitude;
            OwnerId = ownerId;
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }

        public void UpdateInformation(
            string name,
            string address,
            string description,
            string phone,
            double? latitude = null,
            double? longitude = null)
        {
            if (!IsActive)
                throw new InvalidOperationException("Не удается обновить деактивированную точку питания.");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Требуется указать название.", nameof(name));

            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Требуется указать адрес.", nameof(address));

            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Требуется указать телефон.", nameof(phone));

            ValidateCoordinates(latitude, longitude);

            Name = name.Trim();
            Address = address.Trim();
            Description = description.Trim();
            Phone = phone.Trim();
            Latitude = latitude;
            Longitude = longitude;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }

        private static void ValidateCoordinates(double? latitude, double? longitude)
        {
            if (latitude.HasValue != longitude.HasValue)
                throw new ArgumentException("Координаты точки питания должны быть указаны полностью.");

            if (latitude is < -90 or > 90)
                throw new ArgumentOutOfRangeException(nameof(latitude), "Широта должна быть от -90 до 90.");

            if (longitude is < -180 or > 180)
                throw new ArgumentOutOfRangeException(nameof(longitude), "Долгота должна быть от -180 до 180.");
        }
    }
}
