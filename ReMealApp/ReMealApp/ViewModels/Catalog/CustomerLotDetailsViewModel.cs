using Application.Interfaces;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Domain.Enums;
using ReMealApp.ViewModels.Shell;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ReMealApp.ViewModels.Catalog;

public partial class CustomerLotDetailsViewModel : ViewModelBase
{
    private static readonly CultureInfo RussianCulture =
        CultureInfo.GetCultureInfo("ru-RU");

    private static readonly IBrush GreenBrush = SolidColorBrush.Parse("#67D454");
    private static readonly IBrush GreenSoftBrush = SolidColorBrush.Parse("#1A3C24");
    private static readonly IBrush WarningBrush = SolidColorBrush.Parse("#FFBA45");
    private static readonly IBrush WarningSoftBrush = SolidColorBrush.Parse("#3D2E16");
    private static readonly IBrush NeutralBrush = SolidColorBrush.Parse("#A6B2AD");
    private static readonly IBrush NeutralSoftBrush = SolidColorBrush.Parse("#202B2A");
    private static readonly IBrush DangerBrush = SolidColorBrush.Parse("#FF6B6B");
    private static readonly IBrush DangerSoftBrush = SolidColorBrush.Parse("#3A1E20");

    private readonly ILotService _lotService;
    private readonly IBookingService _bookingService;
    private readonly Func<Task> _backToCatalog;
    private readonly Guid _lotId;
    private FoodLot? _currentLot;

    [ObservableProperty]
    private string _title = "Набор";

    [ObservableProperty]
    private string _description = "Описание не добавлено.";

    [ObservableProperty]
    private string _composition = "Состав набора не указан.";

    [ObservableProperty]
    private string _foodPointName = "Точка не указана";

    [ObservableProperty]
    private string _foodPointAddress = "Адрес не указан";

    [ObservableProperty]
    private string _foodPointDescriptionText = "Описание точки питания не добавлено.";

    [ObservableProperty]
    private string _foodPointPhoneText = "Телефон не указан";

    [ObservableProperty]
    private string _foodPointWorkingHoursText = "Режим работы не указан";

    [ObservableProperty]
    private string _pickupInstructionText = "Способ получения не указан";

    [ObservableProperty]
    private string _mapPreviewText = "Карта недоступна";

    [ObservableProperty]
    private string _priceText = "Цена не указана";

    [ObservableProperty]
    private string _availableQuantityText = "Количество не указано";

    [ObservableProperty]
    private string _pickupDeadlineText = "Время получения не указано";

    [ObservableProperty]
    private string _selectedImagePath = string.Empty;

    [ObservableProperty]
    private string _statusText = "Недоступен";

    [ObservableProperty]
    private string _statusDetailsText = string.Empty;

    [ObservableProperty]
    private IBrush _statusBadgeBackground = NeutralSoftBrush;

    [ObservableProperty]
    private IBrush _statusBadgeForeground = NeutralBrush;

    [ObservableProperty]
    private string _compositionTextOnly = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isNotFound;

    public CustomerLotDetailsViewModel(
        Guid lotId,
        ILotService lotService,
        IBookingService bookingService,
        Func<Task> backToCatalog)
    {
        _lotId = lotId;
        _lotService = lotService;
        _bookingService = bookingService;
        _backToCatalog = backToCatalog;
    }

    public ObservableCollection<CustomerLotComponentItemViewModel> Components { get; } = new();

    public ObservableCollection<CustomerLotGalleryImageViewModel> GalleryImages { get; } = new();

    public string CatalogBreadcrumbText => "Каталог";

    public bool HasLot => !IsNotFound;

    public bool HasImage => !string.IsNullOrWhiteSpace(SelectedImagePath);

    public bool HasNoImage => !HasImage;

    public bool HasComponents => Components.Count > 0;

    public bool HasNoComponents => !HasComponents;

    public bool HasCompositionTextOnly => !string.IsNullOrWhiteSpace(CompositionTextOnly);

    public bool IsCompositionEmpty => HasNoComponents && !HasCompositionTextOnly;

    public bool HasGalleryThumbnails => GalleryImages.Count > 1;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool IsFavoriteSupported => false;

    public bool IsRouteAvailable => false;

    public bool CanBook => !IsBusy && !IsNotFound && _currentLot is not null && IsBookable(_currentLot);

    public string BookButtonText => IsBusy ? "Подождите..." : "Забронировать";

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            var lot = await _lotService.GetLotAsync(_lotId);
            if (lot is null)
            {
                _currentLot = null;
                IsNotFound = true;
                StatusMessage = "Набор не найден.";
                return;
            }

            ApplyLot(lot);
        }
        catch (Exception ex)
        {
            StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task BackAsync()
    {
        return _backToCatalog();
    }

    [RelayCommand]
    private async Task BookAsync()
    {
        if (!CanBook)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            await _bookingService.BookLotAsync(_lotId, 1);
            StatusMessage = "Бронирование создано.";

            var updated = await _lotService.GetLotAsync(_lotId);
            if (updated is not null)
                ApplyLot(updated);
        }
        catch (Exception ex)
        {
            StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectImage(CustomerLotGalleryImageViewModel? image)
    {
        if (image is null || string.IsNullOrWhiteSpace(image.ImagePath))
            return;

        SelectedImagePath = image.ImagePath;
        foreach (var galleryImage in GalleryImages)
            galleryImage.IsSelected = galleryImage == image;
    }

    partial void OnSelectedImagePathChanged(string value)
    {
        OnPropertyChanged(nameof(HasImage));
        OnPropertyChanged(nameof(HasNoImage));
    }

    partial void OnStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    partial void OnCompositionTextOnlyChanged(string value)
    {
        OnPropertyChanged(nameof(HasCompositionTextOnly));
        OnPropertyChanged(nameof(IsCompositionEmpty));
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanBook));
        OnPropertyChanged(nameof(BookButtonText));
    }

    partial void OnIsNotFoundChanged(bool value)
    {
        OnPropertyChanged(nameof(HasLot));
        OnPropertyChanged(nameof(CanBook));
    }

    private void ApplyLot(FoodLot lot)
    {
        _currentLot = lot;
        IsNotFound = false;
        Title = string.IsNullOrWhiteSpace(lot.Title) ? "Название не указано" : lot.Title;
        Description = string.IsNullOrWhiteSpace(lot.Description)
            ? "Описание не добавлено."
            : lot.Description.Trim();
        Composition = string.IsNullOrWhiteSpace(lot.Composition)
            ? "Состав набора не указан."
            : lot.Composition.Trim();
        FoodPointName = string.IsNullOrWhiteSpace(lot.FoodPoint?.Name)
            ? "Точка не указана"
            : lot.FoodPoint!.Name;
        FoodPointAddress = string.IsNullOrWhiteSpace(lot.FoodPoint?.Address)
            ? "Адрес не указан"
            : lot.FoodPoint!.Address;
        FoodPointDescriptionText = string.IsNullOrWhiteSpace(lot.FoodPoint?.Description)
            ? "Описание точки питания не добавлено."
            : lot.FoodPoint!.Description.Trim();
        FoodPointPhoneText = string.IsNullOrWhiteSpace(lot.FoodPoint?.Phone)
            ? "Телефон не указан"
            : lot.FoodPoint!.Phone.Trim();
        FoodPointWorkingHoursText = "Режим работы не указан";
        PickupInstructionText = "Способ получения не указан";
        MapPreviewText = ResolveMapPreviewText(lot.FoodPoint);
        PriceText = lot.Price > 0
            ? $"{lot.Price.ToString("N0", RussianCulture)} ₽"
            : "Цена не указана";
        AvailableQuantityText = FormatAvailableQuantity(lot.AvailableQuantity, lot.TotalQuantity);
        PickupDeadlineText = FormatPickupDeadline(lot.PickupDeadline);
        SelectedImagePath = lot.ImagePath ?? string.Empty;
        ApplyStatus(lot);
        RebuildGallery(lot);

        Components.Clear();
        foreach (var component in lot.Components
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name))
        {
            Components.Add(new CustomerLotComponentItemViewModel(component));
        }

        CompositionTextOnly = Components.Count == 0 && !string.IsNullOrWhiteSpace(lot.Composition)
            ? lot.Composition.Trim()
            : string.Empty;

        OnPropertyChanged(nameof(HasComponents));
        OnPropertyChanged(nameof(HasNoComponents));
        OnPropertyChanged(nameof(HasCompositionTextOnly));
        OnPropertyChanged(nameof(IsCompositionEmpty));
        OnPropertyChanged(nameof(CanBook));
        OnPropertyChanged(nameof(BookButtonText));
    }

    private void RebuildGallery(FoodLot lot)
    {
        GalleryImages.Clear();

        if (!string.IsNullOrWhiteSpace(lot.ImagePath))
        {
            GalleryImages.Add(new CustomerLotGalleryImageViewModel(
                lot.ImagePath,
                "Главное фото",
                true));
        }

        OnPropertyChanged(nameof(HasGalleryThumbnails));
    }

    private void ApplyStatus(FoodLot lot)
    {
        var isBookable = IsBookable(lot);

        StatusText = isBookable
            ? "Доступен"
            : lot.Status switch
            {
                LotStatus.Active when lot.FoodPoint is { IsActive: false } => "Точка неактивна",
                LotStatus.Active when lot.AvailableQuantity <= 0 => "Распродан",
                LotStatus.Active when lot.PickupDeadline <= DateTime.UtcNow => "Просрочен",
                LotStatus.Active => "Активен",
                LotStatus.SoldOut => "Распродан",
                LotStatus.Expired => "Просрочен",
                LotStatus.Cancelled => "Снят с публикации",
                _ => lot.Status.ToString()
            };

        StatusDetailsText = ResolveStatusDetails(lot, isBookable);

        StatusBadgeBackground = lot.Status switch
        {
            LotStatus.Active when isBookable => GreenSoftBrush,
            LotStatus.Active => WarningSoftBrush,
            LotStatus.SoldOut => WarningSoftBrush,
            LotStatus.Cancelled => DangerSoftBrush,
            LotStatus.Expired => NeutralSoftBrush,
            _ => NeutralSoftBrush
        };

        StatusBadgeForeground = lot.Status switch
        {
            LotStatus.Active when isBookable => GreenBrush,
            LotStatus.Active => WarningBrush,
            LotStatus.SoldOut => WarningBrush,
            LotStatus.Cancelled => DangerBrush,
            LotStatus.Expired => NeutralBrush,
            _ => NeutralBrush
        };
    }

    private static string ResolveStatusDetails(FoodLot lot, bool isBookable)
    {
        if (isBookable)
            return "Можно забронировать 1 набор.";

        if (lot.FoodPoint is { IsActive: false })
            return "Точка питания сейчас неактивна.";

        if (lot.PickupDeadline == default)
            return "Время получения не указано.";

        if (lot.PickupDeadline <= DateTime.UtcNow)
            return "Время получения уже прошло.";

        if (lot.AvailableQuantity <= 0 || lot.Status == LotStatus.SoldOut)
            return "Набор уже распродан.";

        return lot.Status switch
        {
            LotStatus.Cancelled => "Лот снят с публикации.",
            LotStatus.Expired => "Лот просрочен.",
            _ => "Бронирование сейчас недоступно."
        };
    }

    private static bool IsBookable(FoodLot lot)
    {
        return lot.Status == LotStatus.Active &&
            lot.FoodPoint is { IsActive: true } &&
            lot.AvailableQuantity > 0 &&
            lot.PickupDeadline > DateTime.UtcNow;
    }

    private static string FormatAvailableQuantity(int availableQuantity, int totalQuantity)
    {
        var availableText = availableQuantity >= 0
            ? $"Осталось {availableQuantity.ToString(CultureInfo.InvariantCulture)} шт."
            : "Количество не указано";

        return totalQuantity > 0 && availableQuantity >= 0
            ? $"{availableText} из {totalQuantity.ToString(CultureInfo.InvariantCulture)}"
            : availableText;
    }

    private static string FormatPickupDeadline(DateTime pickupDeadline)
    {
        if (pickupDeadline == default)
            return "Время получения не указано";

        var local = pickupDeadline.ToLocalTime();
        return local.Date == DateTime.Today
            ? $"Сегодня до {local:HH:mm}"
            : $"{local.ToString("dd.MM.yyyy", RussianCulture)} до {local:HH:mm}";
    }

    private static string ResolveMapPreviewText(FoodPoint? foodPoint)
    {
        if (foodPoint?.Latitude is double latitude &&
            foodPoint.Longitude is double longitude)
        {
            return string.Create(
                RussianCulture,
                $"Координаты: {latitude:F5}, {longitude:F5}");
        }

        return "Координаты не указаны";
    }
}

public sealed class CustomerLotComponentItemViewModel
{
    public CustomerLotComponentItemViewModel(LotComponent component)
    {
        Name = string.IsNullOrWhiteSpace(component.Name)
            ? "Компонент"
            : component.Name.Trim();
        QuantityText = string.IsNullOrWhiteSpace(component.Unit)
            ? component.Quantity.ToString(CultureInfo.InvariantCulture)
            : $"{component.Quantity.ToString(CultureInfo.InvariantCulture)} {component.Unit.Trim()}";
        CompositionText = string.IsNullOrWhiteSpace(component.Composition)
            ? string.Empty
            : component.Composition.Trim();
        ImagePath = component.ImagePath ?? string.Empty;
    }

    public string Name { get; }

    public string QuantityText { get; }

    public string CompositionText { get; }

    public bool HasComposition => !string.IsNullOrWhiteSpace(CompositionText);

    public string ImagePath { get; }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);
}

public sealed partial class CustomerLotGalleryImageViewModel : ObservableObject
{
    private static readonly IBrush ActiveBorder = SolidColorBrush.Parse("#65D857");
    private static readonly IBrush DefaultBorder = SolidColorBrush.Parse("#2C4138");

    public CustomerLotGalleryImageViewModel(
        string imagePath,
        string title,
        bool isSelected)
    {
        ImagePath = imagePath;
        Title = title;
        _isSelected = isSelected;
    }

    public string ImagePath { get; }

    public string Title { get; }

    [ObservableProperty]
    private bool _isSelected;

    public IBrush BorderBrush => IsSelected ? ActiveBorder : DefaultBorder;

    public double Opacity => IsSelected ? 1 : 0.78;

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(BorderBrush));
        OnPropertyChanged(nameof(Opacity));
    }
}
