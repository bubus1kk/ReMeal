using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;

namespace ReMealApp.ViewModels.Partner
{
    public sealed class LotDetailsViewModel : ViewModelBase
    {
        public LotDetailsViewModel(
            FoodLot? lot,
            IRelayCommand backCommand,
            IRelayCommand editCommand,
            IAsyncRelayCommand cancelCommand)
        {
            BackCommand = backCommand;
            EditCommand = editCommand;
            CancelCommand = cancelCommand;

            if (lot is null)
            {
                IsNotFound = true;
                Title = "Лот не найден";
                DescriptionText = "Описание не добавлено.";
                CompositionText = "Состав набора не указан.";
                return;
            }

            Lot = lot;
            Id = lot.Id;
            Title = lot.Title;
            StatusText = LotDisplayFormatting.GetVisualStatusText(lot);
            StatusBadgeBackground = LotDisplayFormatting.GetStatusBackground(lot);
            StatusBadgeForeground = LotDisplayFormatting.GetStatusForeground(lot);
            StatusDotBrush = LotDisplayFormatting.GetStatusAccent(lot);
            DescriptionText = string.IsNullOrWhiteSpace(lot.Description)
                ? "Описание не добавлено."
                : lot.Description.Trim();
            CompositionText = string.IsNullOrWhiteSpace(lot.Composition)
                ? "Состав набора не указан."
                : lot.Composition.Trim();
            FoodPointName = lot.FoodPoint?.Name ?? "Не указано";
            FoodPointAddress = lot.FoodPoint?.Address ?? "Не указано";
            PickupText = $"{LotDisplayFormatting.FormatPickupDate(lot.PickupDeadline)}, {LotDisplayFormatting.FormatPickupTime(lot.PickupDeadline)}";
            AvailableText = lot.AvailableQuantity.ToString();
            TotalText = lot.TotalQuantity.ToString();
            PriceText = LotDisplayFormatting.FormatPrice(lot.Price);
            MainImagePath = lot.ImagePath;
            HasMainImage = !string.IsNullOrWhiteSpace(lot.ImagePath);
            CanEditLot = lot.Status is not Domain.Enums.LotStatus.Cancelled and not Domain.Enums.LotStatus.Expired;
            CanCancelLot = lot.Status is not Domain.Enums.LotStatus.Cancelled and not Domain.Enums.LotStatus.Expired;

            Components = lot.Components
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new LotComponentDetailsViewModel(x))
                .ToList();

            Photos = BuildPhotos(lot);
            Parameters = BuildParameters(lot);
        }

        public FoodLot? Lot { get; }

        public Guid Id { get; }

        public bool IsNotFound { get; }

        public bool HasLot => !IsNotFound;

        public string Title { get; } = string.Empty;

        public string StatusText { get; } = string.Empty;

        public IBrush StatusBadgeBackground { get; } = LotDisplayFormatting.NeutralSoftBrush;

        public IBrush StatusBadgeForeground { get; } = LotDisplayFormatting.NeutralBrush;

        public IBrush StatusDotBrush { get; } = LotDisplayFormatting.NeutralBrush;

        public string DescriptionText { get; } = string.Empty;

        public string CompositionText { get; } = string.Empty;

        public string FoodPointName { get; } = "Не указано";

        public string FoodPointAddress { get; } = "Не указано";

        public string PickupText { get; } = "Не указано";

        public string AvailableText { get; } = "0";

        public string TotalText { get; } = "0";

        public string PriceText { get; } = "0 ₽";

        public string? MainImagePath { get; }

        public bool HasMainImage { get; }

        public bool CanEditLot { get; }

        public bool CanCancelLot { get; }

        public IReadOnlyList<LotComponentDetailsViewModel> Components { get; } = Array.Empty<LotComponentDetailsViewModel>();

        public bool HasComponents => Components.Count > 0;

        public IReadOnlyList<LotPhotoViewModel> Photos { get; } = Array.Empty<LotPhotoViewModel>();

        public bool HasPhotos => Photos.Count > 0;

        public IReadOnlyList<LotParameterViewModel> Parameters { get; } = Array.Empty<LotParameterViewModel>();

        public IRelayCommand BackCommand { get; }

        public IRelayCommand EditCommand { get; }

        public IAsyncRelayCommand CancelCommand { get; }

        private static IReadOnlyList<LotPhotoViewModel> BuildPhotos(FoodLot lot)
        {
            var photos = new List<LotPhotoViewModel>();

            if (!string.IsNullOrWhiteSpace(lot.ImagePath))
                photos.Add(new LotPhotoViewModel(lot.ImagePath, lot.Title));

            photos.AddRange(lot.Components
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x.ImagePath))
                .Select(x => new LotPhotoViewModel(x.ImagePath!, x.Name)));

            return photos;
        }

        private static IReadOnlyList<LotParameterViewModel> BuildParameters(FoodLot lot)
        {
            return new List<LotParameterViewModel>
            {
                new("Статус", LotDisplayFormatting.GetStatusText(lot.Status), "/Assets/Icons/Lots/Tinted/available-green.png"),
                new("Точка питания", lot.FoodPoint?.Name ?? "Не указано", "/Assets/Icons/Lots/Tinted/location-green.png"),
                new("Дата публикации", LotDisplayFormatting.FormatFullDateTime(lot.CreatedAt), "/Assets/Icons/Lots/Tinted/calendar-muted.png"),
                new("Получить до", LotDisplayFormatting.FormatFullDateTime(lot.PickupDeadline), "/Assets/Icons/Lots/Tinted/calendar-muted.png"),
                new("Доступно", lot.AvailableQuantity.ToString(), "/Assets/Icons/Lots/Tinted/available-green.png"),
                new("Всего наборов", lot.TotalQuantity.ToString(), "/Assets/Icons/Lots/Tinted/composition-green.png"),
                new("Цена", LotDisplayFormatting.FormatPrice(lot.Price), "/Assets/Icons/Lots/Tinted/price-muted.png")
            };
        }
    }

    public sealed class LotComponentDetailsViewModel
    {
        public LotComponentDetailsViewModel(LotComponent component)
        {
            Name = component.Name;
            QuantityText = string.IsNullOrWhiteSpace(component.Unit)
                ? component.Quantity.ToString()
                : $"{component.Quantity} {component.Unit}";
            CompositionText = string.IsNullOrWhiteSpace(component.Composition)
                ? string.Empty
                : component.Composition.Trim();
            ImagePath = component.ImagePath;
            HasImage = !string.IsNullOrWhiteSpace(component.ImagePath);
        }

        public string Name { get; }

        public string QuantityText { get; }

        public string CompositionText { get; }

        public bool HasComposition => !string.IsNullOrWhiteSpace(CompositionText);

        public string? ImagePath { get; }

        public bool HasImage { get; }
    }

    public sealed class LotPhotoViewModel
    {
        public LotPhotoViewModel(string imagePath, string caption)
        {
            ImagePath = imagePath;
            Caption = caption;
        }

        public string ImagePath { get; }

        public string Caption { get; }
    }

    public sealed class LotParameterViewModel
    {
        public LotParameterViewModel(string name, string value, string iconPath)
        {
            Name = name;
            Value = string.IsNullOrWhiteSpace(value) ? "Не указано" : value;
            IconPath = iconPath;
        }

        public string Name { get; }

        public string Value { get; }

        public string IconPath { get; }
    }
}
