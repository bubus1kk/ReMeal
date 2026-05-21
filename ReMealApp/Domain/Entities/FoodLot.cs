using Domain.Enums;

namespace Domain.Entities
{
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

        public string? ImagePath { get; private set; }

        public FoodPoint? FoodPoint { get; private set; }

        public List<LotComponent> Components { get; private set; } = new();

        public string ComponentsSummary => Components.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, Components
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => x.DisplayText));

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
            DateTime pickupDeadline,
            string? imagePath = null)
        {
            ValidateCreation(foodPointId, title, composition, totalQuantity, price, pickupDeadline);

            Id = Guid.NewGuid();
            FoodPointId = foodPointId;
            Title = title.Trim();
            Description = description.Trim();
            Composition = composition.Trim();
            TotalQuantity = totalQuantity;
            AvailableQuantity = totalQuantity;
            Price = price;
            PickupDeadline = pickupDeadline;
            ImagePath = NormalizeImagePath(imagePath);
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = CreatedAt;
            RefreshStatus();
        }

        public void Update(
            string title,
            string description,
            string composition,
            decimal price,
            DateTime pickupDeadline,
            string? imagePath = null)
        {
            if (Status is LotStatus.Cancelled or LotStatus.Expired)
                throw new InvalidOperationException("Не удается обновить отмененный или истекший лот.");

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Требуется название.", nameof(title));

            if (price <= 0)
                throw new ArgumentException("Цена должна быть больше нуля.", nameof(price));

            if (pickupDeadline <= DateTime.UtcNow)
                throw new ArgumentException("Срок получения должен быть в будущем.", nameof(pickupDeadline));

            Title = title.Trim();
            Description = description.Trim();
            Composition = composition.Trim();
            Price = price;
            PickupDeadline = pickupDeadline;
            ImagePath = NormalizeImagePath(imagePath);
            UpdatedAt = DateTime.UtcNow;
            RefreshStatus();
        }

        public void ReplaceComponents(IEnumerable<LotComponent> components)
        {
            var existingById = Components.ToDictionary(x => x.Id);
            var retainedIds = new HashSet<Guid>();
            var sortOrder = 0;

            foreach (var component in components
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name))
            {
                if (existingById.TryGetValue(component.Id, out var existing))
                {
                    existing.Update(
                        component.Name,
                        component.Quantity,
                        component.Unit,
                        component.Composition,
                        component.ImagePath,
                        sortOrder);
                    retainedIds.Add(existing.Id);
                }
                else
                {
                    component.AssignToLot(Id, sortOrder);
                    Components.Add(component);
                    retainedIds.Add(component.Id);
                }

                sortOrder++;
            }

            for (var index = Components.Count - 1; index >= 0; index--)
            {
                if (!retainedIds.Contains(Components[index].Id))
                    Components.RemoveAt(index);
            }

            UpdatedAt = DateTime.UtcNow;
            Composition = BuildLotComposition(Components);
        }

        public static string BuildLotComposition(IEnumerable<LotComponent> components)
        {
            return string.Join("; ", components
                .Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Quantity > 0)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => x.DisplayText));
        }

        public void MarkExpired()
        {
            Status = LotStatus.Expired;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            if (Status == LotStatus.Expired)
                throw new InvalidOperationException("Не удается отменить истекший лот.");

            if (Status == LotStatus.Cancelled)
                return;

            Status = LotStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
        }

        public void DecreaseQuantity(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Количество должно быть положительным.");

            if (AvailableQuantity < amount)
                throw new InvalidOperationException("Недостаточно доступного количества.");

            AvailableQuantity -= amount;
            UpdatedAt = DateTime.UtcNow;
            RefreshStatus();
        }

        public void IncreaseQuantity(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Количество должно быть положительным.");

            AvailableQuantity += amount;
            TotalQuantity += amount;
            UpdatedAt = DateTime.UtcNow;
            RefreshStatus();
        }

        internal static void ValidateCreation(
            Guid foodPointId,
            string title,
            string composition,
            int totalQuantity,
            decimal price,
            DateTime pickupDeadline)
        {
            if (foodPointId == Guid.Empty)
                throw new ArgumentException("Требуется точка питания.", nameof(foodPointId));

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Требуется название.", nameof(title));

            if (totalQuantity <= 0)
                throw new ArgumentException("Количество должно быть больше нуля.", nameof(totalQuantity));

            if (price <= 0)
                throw new ArgumentException("Цена должна быть больше нуля.", nameof(price));

            if (pickupDeadline <= DateTime.UtcNow)
                throw new ArgumentException("Срок получения должен быть в будущем.", nameof(pickupDeadline));
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

        private static string? NormalizeImagePath(string? imagePath)
        {
            return string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
        }
    }
}
