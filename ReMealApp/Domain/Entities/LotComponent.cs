namespace Domain.Entities
{
    public class LotComponent
    {
        public Guid Id { get; private set; }

        public Guid LotId { get; private set; }

        public FoodLot Lot { get; private set; } = null!;

        public string Name { get; private set; } = string.Empty;

        public int Quantity { get; private set; }

        public string Unit { get; private set; } = DefaultUnit;

        public string? Composition { get; private set; }

        public string? ImagePath { get; private set; }

        public int SortOrder { get; private set; }

        public string DisplayText => string.IsNullOrWhiteSpace(Unit)
            ? $"{Name} — {Quantity}"
            : $"{Name} — {Quantity} {Unit}";

        public static IReadOnlyList<string> AllowedUnits { get; } = new[] { "шт", "л", "мл", "гр", "кг" };

        public const string DefaultUnit = "шт";

        private LotComponent()
        {
        }

        public LotComponent(
            Guid? id,
            string name,
            int quantity,
            string unit,
            string? composition,
            string? imagePath,
            int sortOrder)
        {
            Id = id is { } value && value != Guid.Empty
                ? value
                : Guid.NewGuid();
            SetValues(name, quantity, unit, composition, imagePath, sortOrder);
        }

        public LotComponent(
            string name,
            int quantity,
            string unit,
            string? composition,
            string? imagePath,
            int sortOrder)
            : this(null, name, quantity, unit, composition, imagePath, sortOrder)
        {
        }

        public void AssignToLot(Guid lotId, int sortOrder)
        {
            if (lotId == Guid.Empty)
                throw new ArgumentException("Требуется лот.", nameof(lotId));

            LotId = lotId;
            SortOrder = sortOrder;
        }

        public void Update(
            string name,
            int quantity,
            string unit,
            string? composition,
            string? imagePath,
            int sortOrder)
        {
            SetValues(name, quantity, unit, composition, imagePath, sortOrder);
        }

        private void SetValues(
            string name,
            int quantity,
            string unit,
            string? composition,
            string? imagePath,
            int sortOrder)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Требуется название компонента.", nameof(name));

            if (quantity <= 0)
                throw new ArgumentException("Количество компонента должно быть больше нуля.", nameof(quantity));

            if (string.IsNullOrWhiteSpace(unit))
                throw new ArgumentException("Требуется единица измерения компонента.", nameof(unit));

            var normalizedUnit = unit.Trim();
            if (!AllowedUnits.Contains(normalizedUnit))
                throw new ArgumentException("Недопустимая единица измерения компонента.", nameof(unit));

            Name = name.Trim();
            Quantity = quantity;
            Unit = normalizedUnit;
            Composition = string.IsNullOrWhiteSpace(composition) ? null : composition.Trim();
            ImagePath = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
            SortOrder = sortOrder;
        }
    }
}
