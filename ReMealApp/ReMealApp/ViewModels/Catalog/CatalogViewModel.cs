using Application.Interfaces;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Domain.Enums;
using ReMealApp.ViewModels;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ReMealApp.ViewModels.Catalog
{
    public partial class CatalogViewModel : ViewModelBase
    {
        private static readonly CultureInfo RussianCulture =
            CultureInfo.GetCultureInfo("ru-RU");

        private readonly ILotService _lotService;
        private readonly IBookingService _bookingService;
        private readonly Func<Guid, Task>? _openLotDetails;
        private readonly List<CatalogLotListItemViewModel> _loadedLots = new();
        private bool _isUpdatingFilters;
        private Guid? _pendingFoodPointFilterId;

        [ObservableProperty]
        private ObservableCollection<CatalogLotListItemViewModel> _lots = new();

        [ObservableProperty]
        private ObservableCollection<CatalogFoodPointFilterOption> _foodPointFilters = new();

        [ObservableProperty]
        private CatalogFoodPointFilterOption? _selectedFoodPointFilter;

        [ObservableProperty]
        private ObservableCollection<CatalogStatusFilterOption> _statusFilters = new();

        [ObservableProperty]
        private CatalogStatusFilterOption? _selectedStatusFilter;

        [ObservableProperty]
        private ObservableCollection<CatalogSortOption> _sortOptions = new();

        [ObservableProperty]
        private CatalogSortOption? _selectedSortOption;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _feedbackMessage = string.Empty;

        [ObservableProperty]
        private string _bookingLimitMessage = string.Empty;

        [ObservableProperty]
        private string _summaryMessage = "Доступных наборов: 0";

        [ObservableProperty]
        private bool _isFoodPointFilterActive;

        [ObservableProperty]
        private bool _isFoodPointFilterEnabled;

        [ObservableProperty]
        private bool _isBusy;

        public CatalogViewModel(
            ILotService lotService,
            IBookingService bookingService)
            : this(lotService, bookingService, null)
        {
        }

        public CatalogViewModel(
            ILotService lotService,
            IBookingService bookingService,
            Func<Guid, Task>? openLotDetails)
        {
            _lotService = lotService;
            _bookingService = bookingService;
            _openLotDetails = openLotDetails;

            StatusFilters = new ObservableCollection<CatalogStatusFilterOption>(
                CatalogStatusFilterOption.CreateDefault());
            SelectedStatusFilter = StatusFilters.FirstOrDefault();

            SortOptions = new ObservableCollection<CatalogSortOption>(
                CatalogSortOption.CreateDefault());
            SelectedSortOption = SortOptions.FirstOrDefault();

            RebuildFoodPointFilters(null);
        }

        public string CatalogTitle => "Доступные пищевые наборы";

        public string CatalogSubtitle =>
            "Выберите подходящий набор и забронируйте его до окончания срока получения";

        public bool HasLots => Lots.Count > 0;

        public bool IsCatalogEmpty => !IsBusy && !HasLots;

        public bool HasAnyLoadedLots => _loadedLots.Count > 0;

        public bool IsFilteredEmptyState => IsCatalogEmpty && HasAnyLoadedLots;

        public bool IsNoDataEmptyState => IsCatalogEmpty && !HasAnyLoadedLots;

        public bool HasFeedbackMessage => !string.IsNullOrWhiteSpace(FeedbackMessage);

        public bool IsBookingLimitDialogOpen => !string.IsNullOrWhiteSpace(BookingLimitMessage);

        public bool CanResetFilters => HasActiveFilters;

        public string EmptyTitle =>
            IsFilteredEmptyState
                ? "Ничего не найдено"
                : "Сейчас нет доступных наборов";

        public string EmptySubtitle =>
            IsFilteredEmptyState
                ? "Измените поисковый запрос или сбросьте фильтры."
                : "Попробуйте изменить фильтры или зайдите позже.";

        private bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(SearchText) ||
            SelectedFoodPointFilter?.Id is not null ||
            SelectedStatusFilter?.Status is not null ||
            SelectedSortOption?.Kind is not null and not CatalogSortKind.PickupDeadline;

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                FeedbackMessage = string.Empty;
                await ReloadDataCoreAsync();
            }
            catch (Exception ex)
            {
                FeedbackMessage = ExceptionMessageFormatter
                    .ToUserMessage(ex);
                StatusMessage = FeedbackMessage;
            }
            finally
            {
                IsBusy = false;
                RefreshStateProperties();
            }
        }

        public async Task LoadAllAsync()
        {
            _pendingFoodPointFilterId = null;
            await LoadAsync();
        }

        public async Task LoadForFoodPointAsync(Guid foodPointId)
        {
            _pendingFoodPointFilterId = foodPointId;
            await LoadAsync();
        }

        [RelayCommand]
        private void ClearFoodPointFilter()
        {
            _pendingFoodPointFilterId = null;
            SelectedFoodPointFilter = FoodPointFilters.FirstOrDefault(x => x.Id is null);
            ApplyFilters();
        }

        [RelayCommand]
        private async Task BookLotAsync(object? parameter)
        {
            var lotId = parameter switch
            {
                CatalogLotListItemViewModel lot => lot.Id,
                Guid value => value,
                _ => (Guid?)null
            };

            if (lotId is not Guid selectedLotId || IsBusy)
                return;

            try
            {
                IsBusy = true;
                FeedbackMessage = string.Empty;

                await _bookingService.BookLotAsync(
                    selectedLotId,
                    1);

                _pendingFoodPointFilterId = SelectedFoodPointFilter?.Id;
                await ReloadDataCoreAsync();
            }
            catch (Exception ex)
            {
                ShowBookingFailure(ex);
            }
            finally
            {
                IsBusy = false;
                RefreshStateProperties();
            }
        }

        [RelayCommand]
        private async Task OpenLotDetailsAsync(CatalogLotListItemViewModel? lot)
        {
            if (lot is null || IsBusy)
                return;

            if (_openLotDetails is null)
            {
                FeedbackMessage = "Экран деталей набора недоступен.";
                return;
            }

            await _openLotDetails(lot.Id);
        }

        [RelayCommand]
        private void ResetFilters()
        {
            _pendingFoodPointFilterId = null;
            SearchText = string.Empty;
            SelectedFoodPointFilter = FoodPointFilters.FirstOrDefault(x => x.Id is null);
            SelectedStatusFilter = StatusFilters.FirstOrDefault();
            SelectedSortOption = SortOptions.FirstOrDefault();
            ApplyFilters();
        }

        [RelayCommand]
        private void CloseBookingLimitDialog()
        {
            BookingLimitMessage = string.Empty;
        }

        partial void OnSearchTextChanged(string value) => ApplyFilters();

        partial void OnSelectedFoodPointFilterChanged(CatalogFoodPointFilterOption? value)
        {
            if (_isUpdatingFilters)
                return;

            _pendingFoodPointFilterId = value?.Id;
            ApplyFilters();
        }

        partial void OnSelectedStatusFilterChanged(CatalogStatusFilterOption? value) => ApplyFilters();

        partial void OnSelectedSortOptionChanged(CatalogSortOption? value) => ApplyFilters();

        partial void OnLotsChanged(ObservableCollection<CatalogLotListItemViewModel> value) =>
            RefreshStateProperties();

        partial void OnIsBusyChanged(bool value) => RefreshStateProperties();

        partial void OnFeedbackMessageChanged(string value) =>
            OnPropertyChanged(nameof(HasFeedbackMessage));

        partial void OnBookingLimitMessageChanged(string value) =>
            OnPropertyChanged(nameof(IsBookingLimitDialogOpen));

        private async Task ReloadDataCoreAsync()
        {
            var preferredFoodPointId = _pendingFoodPointFilterId ?? SelectedFoodPointFilter?.Id;
            var items = await _lotService.GetAvailableLotsAsync();

            _loadedLots.Clear();
            _loadedLots.AddRange(items.Select(CatalogLotListItemViewModel.FromLot));

            RebuildFoodPointFilters(preferredFoodPointId);
            ApplyFilters();
        }

        private void RebuildFoodPointFilters(Guid? selectedFoodPointId)
        {
            _isUpdatingFilters = true;

            try
            {
                var pointOptions = _loadedLots
                    .Where(x => x.FoodPointId != Guid.Empty)
                    .GroupBy(x => x.FoodPointId)
                    .Select(x => new CatalogFoodPointFilterOption(
                        x.Key,
                        x.Select(item => item.FoodPointName)
                            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? "Точка не указана"))
                    .OrderBy(x => x.Name)
                    .ToList();

                IsFoodPointFilterEnabled = pointOptions.Count > 0;

                if (pointOptions.Count == 0)
                {
                    FoodPointFilters = new ObservableCollection<CatalogFoodPointFilterOption>
                    {
                        new(null, "Нет точек")
                    };
                    SelectedFoodPointFilter = FoodPointFilters[0];
                    IsFoodPointFilterActive = false;
                    _pendingFoodPointFilterId = null;
                    return;
                }

                var options = new List<CatalogFoodPointFilterOption>
                {
                    new(null, "Все")
                };
                options.AddRange(pointOptions);

                FoodPointFilters = new ObservableCollection<CatalogFoodPointFilterOption>(options);
                SelectedFoodPointFilter = selectedFoodPointId is Guid id
                    ? FoodPointFilters.FirstOrDefault(x => x.Id == id) ?? FoodPointFilters[0]
                    : FoodPointFilters[0];

                IsFoodPointFilterActive = SelectedFoodPointFilter.Id is not null;
                _pendingFoodPointFilterId = SelectedFoodPointFilter.Id;
            }
            finally
            {
                _isUpdatingFilters = false;
            }
        }

        private void ApplyFilters()
        {
            if (_isUpdatingFilters)
                return;

            IEnumerable<CatalogLotListItemViewModel> result = _loadedLots;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var query = SearchText.Trim();
                result = result.Where(x =>
                    Contains(x.Title, query) ||
                    Contains(x.FoodPointName, query) ||
                    Contains(x.Address, query) ||
                    Contains(x.CompositionText, query));
            }

            if (SelectedFoodPointFilter?.Id is Guid foodPointId)
                result = result.Where(x => x.FoodPointId == foodPointId);

            if (SelectedStatusFilter?.Status is LotStatus status)
                result = result.Where(x => x.Status == status);

            result = SelectedSortOption?.Kind switch
            {
                CatalogSortKind.PriceAsc => result
                    .OrderBy(x => x.Price)
                    .ThenBy(x => x.PickupDeadline),
                CatalogSortKind.PriceDesc => result
                    .OrderByDescending(x => x.Price)
                    .ThenBy(x => x.PickupDeadline),
                CatalogSortKind.QuantityDesc => result
                    .OrderByDescending(x => x.AvailableQuantity)
                    .ThenBy(x => x.PickupDeadline),
                _ => result.OrderBy(x => x.PickupDeadline)
            };

            Lots = new ObservableCollection<CatalogLotListItemViewModel>(result);
            SummaryMessage = $"Доступных наборов: {Lots.Count.ToString(CultureInfo.InvariantCulture)}";
            StatusMessage = Lots.Count == 0
                ? "Сейчас нет доступных наборов."
                : SummaryMessage;
            IsFoodPointFilterActive = SelectedFoodPointFilter?.Id is not null;
            RefreshStateProperties();
        }

        private void ShowBookingFailure(Exception exception)
        {
            var message = ExceptionMessageFormatter.ToUserMessage(exception);
            if (BookingLimitMessageFormatter.TryFormat(message, out var bookingLimitMessage))
            {
                BookingLimitMessage = bookingLimitMessage;
                FeedbackMessage = string.Empty;
                StatusMessage = string.Empty;
                return;
            }

            FeedbackMessage = message;
            StatusMessage = message;
        }

        private void RefreshStateProperties()
        {
            OnPropertyChanged(nameof(HasLots));
            OnPropertyChanged(nameof(IsCatalogEmpty));
            OnPropertyChanged(nameof(HasAnyLoadedLots));
            OnPropertyChanged(nameof(IsFilteredEmptyState));
            OnPropertyChanged(nameof(IsNoDataEmptyState));
            OnPropertyChanged(nameof(CanResetFilters));
            OnPropertyChanged(nameof(EmptyTitle));
            OnPropertyChanged(nameof(EmptySubtitle));
        }

        private static bool Contains(string? source, string query)
        {
            return source?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true;
        }

        internal static string FormatPrice(decimal price)
        {
            return price > 0
                ? $"{price.ToString("N2", RussianCulture)} ₽"
                : "Цена не указана";
        }

        internal static string FormatPickupDate(DateTime pickupDeadline)
        {
            if (pickupDeadline == default)
                return "Срок получения не указан";

            return pickupDeadline.ToLocalTime().ToString("dd.MM.yyyy", RussianCulture);
        }

        internal static string FormatPickupTime(DateTime pickupDeadline)
        {
            return pickupDeadline == default
                ? string.Empty
                : pickupDeadline.ToLocalTime().ToString("HH:mm", RussianCulture);
        }
    }

    public sealed class CatalogLotListItemViewModel
    {
        private static readonly IBrush TextBrush = SolidColorBrush.Parse("#F2F8F1");
        private static readonly IBrush MutedBrush = SolidColorBrush.Parse("#9EA9A4");
        private static readonly IBrush GreenBrush = SolidColorBrush.Parse("#78D957");
        private static readonly IBrush GreenSoftBrush = SolidColorBrush.Parse("#173821");
        private static readonly IBrush WarningBrush = SolidColorBrush.Parse("#FFB238");
        private static readonly IBrush WarningSoftBrush = SolidColorBrush.Parse("#3A2D16");
        private static readonly IBrush NeutralBrush = SolidColorBrush.Parse("#A4AFAB");
        private static readonly IBrush NeutralSoftBrush = SolidColorBrush.Parse("#202B2A");

        private CatalogLotListItemViewModel()
        {
        }

        public Guid Id { get; init; }

        public Guid FoodPointId { get; init; }

        public string Title { get; init; } = string.Empty;

        public string FoodPointName { get; init; } = string.Empty;

        public FoodPoint? FoodPoint { get; init; }

        public string Address { get; init; } = string.Empty;

        public string CompositionText { get; init; } = string.Empty;

        public int ComponentsCount { get; init; }

        public int AvailableQuantity { get; init; }

        public decimal Price { get; init; }

        public DateTime PickupDeadline { get; init; }

        public LotStatus Status { get; init; }

        public string ImagePath { get; init; } = string.Empty;

        public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

        public bool HasComponentCount => ComponentsCount > 0;

        public bool HasPickupTime => PickupDeadline != default;

        public string ComponentsCountText => FormatPositionCount(ComponentsCount);

        public string AvailableQuantityText =>
            $"{AvailableQuantity.ToString(CultureInfo.InvariantCulture)} шт";

        public string PriceText => CatalogViewModel.FormatPrice(Price);

        public string PickupDateText => CatalogViewModel.FormatPickupDate(PickupDeadline);

        public string PickupTimeText => CatalogViewModel.FormatPickupTime(PickupDeadline);

        public bool IsAvailableForSale =>
            Status == LotStatus.Active &&
            AvailableQuantity > 0 &&
            PickupDeadline > DateTime.UtcNow;

        public string StatusText => IsAvailableForSale
            ? "Доступен"
            : Status switch
            {
                LotStatus.Active => "Активен",
                LotStatus.SoldOut => "Распродан",
                LotStatus.Expired => "Просрочен",
                LotStatus.Cancelled => "Снят с публикации",
                _ => Status.ToString()
            };

        public IBrush QuantityBrush => AvailableQuantity > 0 ? GreenBrush : MutedBrush;

        public IBrush StatusBadgeBackground => Status switch
        {
            LotStatus.Active when IsAvailableForSale => GreenSoftBrush,
            LotStatus.Active => GreenSoftBrush,
            LotStatus.SoldOut => WarningSoftBrush,
            _ => NeutralSoftBrush
        };

        public IBrush StatusBadgeForeground => Status switch
        {
            LotStatus.Active when IsAvailableForSale => GreenBrush,
            LotStatus.Active => GreenBrush,
            LotStatus.SoldOut => WarningBrush,
            _ => NeutralBrush
        };

        public IBrush StatusDotBrush => StatusBadgeForeground;

        public IBrush TitleBrush => TextBrush;

        public static CatalogLotListItemViewModel FromLot(FoodLot lot)
        {
            var componentCount = lot.Components.Count;
            var composition = ResolveComposition(lot);

            return new CatalogLotListItemViewModel
            {
                Id = lot.Id,
                FoodPointId = lot.FoodPointId,
                Title = string.IsNullOrWhiteSpace(lot.Title)
                    ? "Название не указано"
                    : lot.Title.Trim(),
                FoodPointName = string.IsNullOrWhiteSpace(lot.FoodPoint?.Name)
                    ? "Точка не указана"
                    : lot.FoodPoint!.Name.Trim(),
                FoodPoint = lot.FoodPoint,
                Address = string.IsNullOrWhiteSpace(lot.FoodPoint?.Address)
                    ? "Адрес не указан"
                    : lot.FoodPoint!.Address.Trim(),
                CompositionText = string.IsNullOrWhiteSpace(composition)
                    ? "Состав не указан"
                    : composition,
                ComponentsCount = componentCount,
                AvailableQuantity = lot.AvailableQuantity,
                Price = lot.Price,
                PickupDeadline = lot.PickupDeadline,
                Status = lot.Status,
                ImagePath = lot.ImagePath ?? string.Empty
            };
        }

        private static string ResolveComposition(FoodLot lot)
        {
            if (!string.IsNullOrWhiteSpace(lot.Composition))
                return lot.Composition.Trim();

            if (lot.Components.Count == 0)
                return string.Empty;

            return string.Join(", ", lot.Components
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Take(4)
                .Select(x => x.DisplayText));
        }

        private static string FormatPositionCount(int count)
        {
            if (count <= 0)
                return string.Empty;

            var lastTwo = count % 100;
            var last = count % 10;
            var noun = lastTwo is >= 11 and <= 14
                ? "позиций"
                : last switch
                {
                    1 => "позиция",
                    2 or 3 or 4 => "позиции",
                    _ => "позиций"
                };

            return $"{count.ToString(CultureInfo.InvariantCulture)} {noun}";
        }
    }

    public sealed class CatalogFoodPointFilterOption
    {
        public CatalogFoodPointFilterOption(Guid? id, string name)
        {
            Id = id;
            Name = name;
        }

        public Guid? Id { get; }

        public string Name { get; }
    }

    public sealed class CatalogStatusFilterOption
    {
        public CatalogStatusFilterOption(LotStatus? status, string name)
        {
            Status = status;
            Name = name;
        }

        public LotStatus? Status { get; }

        public string Name { get; }

        public static IReadOnlyList<CatalogStatusFilterOption> CreateDefault()
        {
            return new[]
            {
                new CatalogStatusFilterOption(null, "Все статусы"),
                new CatalogStatusFilterOption(LotStatus.Active, "Активен"),
                new CatalogStatusFilterOption(LotStatus.SoldOut, "Распродан"),
                new CatalogStatusFilterOption(LotStatus.Expired, "Просрочен"),
                new CatalogStatusFilterOption(LotStatus.Cancelled, "Снят с публикации")
            };
        }
    }

    public sealed class CatalogSortOption
    {
        public CatalogSortOption(CatalogSortKind kind, string name)
        {
            Kind = kind;
            Name = name;
        }

        public CatalogSortKind Kind { get; }

        public string Name { get; }

        public static IReadOnlyList<CatalogSortOption> CreateDefault()
        {
            return new[]
            {
                new CatalogSortOption(CatalogSortKind.PickupDeadline, "Срок получения"),
                new CatalogSortOption(CatalogSortKind.PriceAsc, "Сначала дешевле"),
                new CatalogSortOption(CatalogSortKind.PriceDesc, "Сначала дороже"),
                new CatalogSortOption(CatalogSortKind.QuantityDesc, "Больше доступно")
            };
        }
    }

    public enum CatalogSortKind
    {
        PickupDeadline,
        PriceAsc,
        PriceDesc,
        QuantityDesc
    }
}
