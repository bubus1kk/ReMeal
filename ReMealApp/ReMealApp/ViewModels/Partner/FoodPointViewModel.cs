using Application.DTOs.FoodPoints;
using Application.Interfaces;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using ReMealApp.ViewModels.Shell;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ReMealApp.ViewModels.Partner
{
    public partial class FoodPointViewModel : ViewModelBase
    {
        private readonly IFoodPointService _foodPointService;
        private readonly ILotService _lotService;
        private readonly IGeocodingService _geocodingService;
        private readonly HomeViewModel _shell;
        private readonly List<FoodPointListItemViewModel> _allFoodPoints = new();
        private bool _isUpdatingCoordinates;

        [ObservableProperty]
        private ObservableCollection<FoodPointListItemViewModel> _foodPoints = new();

        [ObservableProperty]
        private ObservableCollection<FoodPointStatusFilterOption> _statusFilters = new();

        [ObservableProperty]
        private FoodPointStatusFilterOption? _selectedStatusFilter;

        [ObservableProperty]
        private FoodPointDetailsViewModel? _currentDetails;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private bool _showActiveOnly;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _address = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private double? _latitude;

        [ObservableProperty]
        private double? _longitude;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _hasAnyFoodPoints;

        [ObservableProperty]
        private bool _hasVisibleFoodPoints;

        [ObservableProperty]
        private bool _isCreateMode;

        [ObservableProperty]
        private bool _isEditing;

        public FoodPointViewModel(
            IFoodPointService foodPointService,
            ILotService lotService,
            IGeocodingService geocodingService,
            HomeViewModel shell)
        {
            _foodPointService = foodPointService;
            _lotService = lotService;
            _geocodingService = geocodingService;
            _shell = shell;

            StatusFilters = new ObservableCollection<FoodPointStatusFilterOption>
            {
                new(FoodPointStatusFilterKind.All, "Все"),
                new(FoodPointStatusFilterKind.Active, "Активные"),
                new(FoodPointStatusFilterKind.Inactive, "Деактивированные")
            };
            SelectedStatusFilter = StatusFilters.FirstOrDefault();
        }

        public bool IsListMode => CurrentDetails is null && !IsCreateMode;

        public bool IsDetailScreen => CurrentDetails is not null || IsCreateMode;

        public bool HasCurrentFoodPoint => CurrentDetails is not null && !IsCreateMode;

        public bool HasFoodPoints => HasAnyFoodPoints;

        public bool IsEmptyState => !IsBusy && !HasVisibleFoodPoints;

        public bool IsFilteredEmptyState => HasAnyFoodPoints && IsEmptyState;

        public bool CanResetFilters => HasActiveFilters;

        public bool CanEditCurrent => HasCurrentFoodPoint && CurrentDetails?.IsActive == true && !IsBusy && !IsEditing;

        public bool CanCreateLotForCurrent => HasCurrentFoodPoint && CurrentDetails?.IsActive == true && !IsBusy;

        public bool CanDeactivateCurrent => HasCurrentFoodPoint && CurrentDetails?.IsActive == true && !IsBusy;

        public bool CanDeleteCurrent => HasCurrentFoodPoint && !IsBusy;

        public bool CanSave => !IsBusy && (IsCreateMode || IsEditing);

        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

        public bool HasSelectedCoordinates => Latitude.HasValue && Longitude.HasValue;

        public string CoordinatesStatusText => HasSelectedCoordinates ? "Координаты выбраны" : "Координаты не выбраны";

        public string CoordinatesTechnicalText => HasSelectedCoordinates
            ? $"Latitude: {Latitude!.Value:F6}; Longitude: {Longitude!.Value:F6}"
            : "Нажмите «Найти на карте», чтобы определить координаты по адресу.";

        public bool IsInactiveDetail => HasCurrentFoodPoint && CurrentDetails?.IsActive == false;

        public string EmptyTitle => HasAnyFoodPoints ? "Ничего не найдено" : "У вас пока нет точек питания";

        public string EmptySubtitle => HasAnyFoodPoints
            ? "Измените поисковый запрос или сбросьте фильтры."
            : "Создайте первую точку, чтобы публиковать лоты и управлять бронированиями.";

        public string DetailTitle => IsCreateMode
            ? "Новая точка питания"
            : CurrentDetails?.NameText ?? "Точка питания";

        public string DetailSubtitle => IsCreateMode
            ? "Заполните реальные данные новой точки питания."
            : CurrentDetails?.AddressText ?? "Адрес не указан";

        public string DetailStatusText => CurrentDetails?.StatusText ?? string.Empty;

        public IBrush DetailStatusBadgeBackground => CurrentDetails?.StatusBadgeBackground ?? FoodPointDisplayFormatting.NeutralSoftBrush;

        public IBrush DetailStatusBadgeForeground => CurrentDetails?.StatusBadgeForeground ?? FoodPointDisplayFormatting.NeutralBrush;

        public IBrush DetailStatusDotBrush => CurrentDetails?.StatusDotBrush ?? FoodPointDisplayFormatting.NeutralBrush;

        public string FormCardTitle => IsCreateMode ? "Данные новой точки" : "Данные точки";

        public string SaveButtonText => IsCreateMode ? "Создать точку" : "Сохранить изменения";

        public string CancelButtonText => IsCreateMode ? "Отмена" : "Отмена редактирования";

        public string ReadNameText => CurrentDetails?.NameText ?? "Не указано";

        public string ReadAddressText => CurrentDetails?.AddressText ?? "Адрес не указан";

        public string ReadDescriptionText => CurrentDetails?.DescriptionText ?? "Описание не добавлено";

        public string ReadPhoneText => CurrentDetails?.PhoneText ?? "Телефон не указан";

        public string ReadCreatedAtText => CurrentDetails?.CreatedAtText ?? "Дата создания недоступна";

        public IReadOnlyList<FoodPointLotItemViewModel> CurrentLots => CurrentDetails?.Lots ?? Array.Empty<FoodPointLotItemViewModel>();

        public bool CurrentDetailsHasLots => CurrentDetails?.HasLots == true;

        private bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(SearchQuery) ||
            ShowActiveOnly ||
            SelectedStatusFilter?.Kind is not null and not FoodPointStatusFilterKind.All;

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            var currentDetailId = CurrentDetails?.Id;
            var shouldRestoreDetails = HasCurrentFoodPoint;

            try
            {
                IsBusy = true;
                await ReloadFoodPointsAsync();

                if (shouldRestoreDetails && currentDetailId is Guid id)
                {
                    var item = _allFoodPoints.FirstOrDefault(x => x.Id == id);
                    if (item is not null)
                        await ShowDetailsCoreAsync(item.FoodPoint);
                    else
                        BackToListCore();
                }

                StatusMessage = HasAnyFoodPoints
                    ? "Список точек обновлен."
                    : "У вас пока нет точек питания.";
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
        private async Task OpenDetailsAsync(FoodPointListItemViewModel? item)
        {
            if (item is null || IsBusy)
                return;

            try
            {
                IsBusy = true;
                await ShowDetailsCoreAsync(item.FoodPoint);
                StatusMessage = string.Empty;
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
        private void BackToList()
        {
            BackToListCore();
        }

        [RelayCommand]
        private void NewFoodPoint()
        {
            CurrentDetails = null;
            IsCreateMode = true;
            IsEditing = true;
            ClearFormFields();
            StatusMessage = string.Empty;
        }

        [RelayCommand]
        private void EditCurrent()
        {
            if (CurrentDetails is null)
                return;

            if (!CurrentDetails.IsActive)
            {
                StatusMessage = "Деактивированную точку нельзя редактировать.";
                return;
            }

            ApplyFoodPointToForm(CurrentDetails.FoodPoint);
            IsEditing = true;
            StatusMessage = string.Empty;
        }

        [RelayCommand]
        private void CancelEditing()
        {
            if (IsCreateMode)
            {
                BackToListCore();
                return;
            }

            if (CurrentDetails is not null)
                ApplyFoodPointToForm(CurrentDetails.FoodPoint);

            IsEditing = false;
            StatusMessage = string.Empty;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (IsBusy || (!IsCreateMode && CurrentDetails is null))
                return;

            try
            {
                IsBusy = true;

                FoodPoint saved;
                if (IsCreateMode)
                {
                    saved = await _foodPointService.CreateFoodPointAsync(new CreateFoodPointRequest(
                        Name,
                        Address,
                        Description,
                        Phone,
                        Latitude,
                        Longitude));

                    StatusMessage = "Точка питания создана.";
                }
                else
                {
                    saved = await _foodPointService.UpdateFoodPointAsync(new UpdateFoodPointRequest(
                        CurrentDetails!.Id,
                        Name,
                        Address,
                        Description,
                        Phone,
                        Latitude,
                        Longitude));

                    StatusMessage = "Точка питания обновлена.";
                }

                await ReloadFoodPointsAsync();
                var reloaded = _allFoodPoints.FirstOrDefault(x => x.Id == saved.Id)?.FoodPoint ?? saved;
                IsCreateMode = false;
                IsEditing = false;
                await ShowDetailsCoreAsync(reloaded);
                await _shell.RefreshPartnerAsync();
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
        private async Task FindAddressOnMapAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(Address))
            {
                StatusMessage = "Введите адрес точки питания.";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Ищем адрес на карте...";

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                var result = await _geocodingService.GeocodeAddressAsync(Address, timeout.Token);
                if (result is null)
                {
                    StatusMessage = "Адрес не найден.";
                    return;
                }

                SetSelectedCoordinates(
                    result.Coordinates.Latitude,
                    result.Coordinates.Longitude);
                StatusMessage = "Координаты найдены по адресу.";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Не удалось найти адрес: запрос занял слишком много времени.";
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

        public void SetSelectedCoordinates(double latitude, double longitude)
        {
            if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            {
                StatusMessage = "Не удалось выбрать координаты на карте.";
                return;
            }

            UpdateCoordinates(latitude, longitude);
            StatusMessage = "Координаты выбраны.";
        }

        [RelayCommand]
        private async Task DeactivateAsync()
        {
            if (CurrentDetails is null || IsBusy)
            {
                StatusMessage = "Сначала выберите точку питания.";
                return;
            }

            if (!CurrentDetails.IsActive)
            {
                StatusMessage = "Точка уже деактивирована.";
                return;
            }

            try
            {
                IsBusy = true;
                var foodPointId = CurrentDetails.Id;
                await _foodPointService.DeactivateFoodPointAsync(foodPointId);
                await ReloadFoodPointsAsync();

                var reloaded = _allFoodPoints.FirstOrDefault(x => x.Id == foodPointId)?.FoodPoint;
                if (reloaded is not null)
                    await ShowDetailsCoreAsync(reloaded);

                IsEditing = false;
                StatusMessage = "Точка питания деактивирована.";
                await _shell.RefreshPartnerAsync();
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
        private async Task DeleteAsync()
        {
            if (CurrentDetails is null || IsBusy)
            {
                StatusMessage = "Сначала выберите точку питания.";
                return;
            }

            try
            {
                IsBusy = true;
                await _foodPointService.DeleteFoodPointAsync(CurrentDetails.Id);
                BackToListCore();
                await ReloadFoodPointsAsync();
                StatusMessage = "Точка питания и связанные с ней лоты удалены.";
                await _shell.RefreshPartnerAsync();
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
        private void CreateLotForCurrent()
        {
            if (CurrentDetails is not { IsActive: true })
            {
                StatusMessage = "Сначала выберите активную точку питания.";
                return;
            }

            _shell.OpenCreateLot(CurrentDetails.Id);
        }

        [RelayCommand]
        private void CreateLotForItem(FoodPointListItemViewModel? item)
        {
            if (item is not { IsActive: true })
            {
                StatusMessage = "Сначала выберите активную точку питания.";
                return;
            }

            _shell.OpenCreateLot(item.Id);
        }

        [RelayCommand]
        private void OpenLot(FoodPointLotItemViewModel? item)
        {
            if (item is null)
                return;

            _shell.OpenLotEditor(item.Id);
        }

        [RelayCommand]
        private void ResetFilters()
        {
            SearchQuery = string.Empty;
            ShowActiveOnly = false;
            SelectedStatusFilter = StatusFilters.FirstOrDefault();
            ApplyFilters();
        }

        private async Task ReloadFoodPointsAsync()
        {
            var foodPoints = await _foodPointService.GetCurrentPartnerFoodPointsAsync();

            _allFoodPoints.Clear();
            _allFoodPoints.AddRange(foodPoints.Select(x => new FoodPointListItemViewModel(x)));

            HasAnyFoodPoints = _allFoodPoints.Count > 0;
            ApplyFilters();
        }

        private async Task ShowDetailsCoreAsync(FoodPoint foodPoint)
        {
            await _lotService.MarkExpiredLotsAsync();
            var lots = await _lotService.GetFoodPointLotsAsync(foodPoint.Id);
            CurrentDetails = new FoodPointDetailsViewModel(foodPoint, lots);
            IsCreateMode = false;
            IsEditing = false;
            ApplyFoodPointToForm(foodPoint);
        }

        private void BackToListCore()
        {
            CurrentDetails = null;
            IsCreateMode = false;
            IsEditing = false;
            ClearFormFields();
        }

        private void ApplyFilters()
        {
            var filtered = _allFoodPoints.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var query = SearchQuery.Trim();
                filtered = filtered.Where(x =>
                    Contains(x.NameText, query) ||
                    Contains(x.AddressText, query));
            }

            filtered = SelectedStatusFilter?.Kind switch
            {
                FoodPointStatusFilterKind.Active => filtered.Where(x => x.IsActive),
                FoodPointStatusFilterKind.Inactive => filtered.Where(x => !x.IsActive),
                _ => filtered
            };

            if (ShowActiveOnly)
                filtered = filtered.Where(x => x.IsActive);

            FoodPoints = new ObservableCollection<FoodPointListItemViewModel>(filtered);
            HasVisibleFoodPoints = FoodPoints.Count > 0;
            RefreshScreenState();
        }

        private static bool Contains(string? source, string query)
        {
            return source?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true;
        }

        private void ApplyFoodPointToForm(FoodPoint foodPoint)
        {
            Name = foodPoint.Name;
            Address = foodPoint.Address;
            Description = foodPoint.Description;
            Phone = foodPoint.Phone;
            UpdateCoordinates(foodPoint.Latitude, foodPoint.Longitude);
        }

        private void ClearFormFields()
        {
            Name = string.Empty;
            Address = string.Empty;
            Description = string.Empty;
            Phone = string.Empty;
            UpdateCoordinates(null, null);
        }

        private void RefreshScreenState()
        {
            OnPropertyChanged(nameof(IsListMode));
            OnPropertyChanged(nameof(IsDetailScreen));
            OnPropertyChanged(nameof(HasCurrentFoodPoint));
            OnPropertyChanged(nameof(HasFoodPoints));
            OnPropertyChanged(nameof(IsEmptyState));
            OnPropertyChanged(nameof(IsFilteredEmptyState));
            OnPropertyChanged(nameof(CanResetFilters));
            OnPropertyChanged(nameof(CanEditCurrent));
            OnPropertyChanged(nameof(CanCreateLotForCurrent));
            OnPropertyChanged(nameof(CanDeactivateCurrent));
            OnPropertyChanged(nameof(CanDeleteCurrent));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(HasSelectedCoordinates));
            OnPropertyChanged(nameof(CoordinatesStatusText));
            OnPropertyChanged(nameof(CoordinatesTechnicalText));
            OnPropertyChanged(nameof(IsInactiveDetail));
            OnPropertyChanged(nameof(EmptyTitle));
            OnPropertyChanged(nameof(EmptySubtitle));
            OnPropertyChanged(nameof(DetailTitle));
            OnPropertyChanged(nameof(DetailSubtitle));
            OnPropertyChanged(nameof(DetailStatusText));
            OnPropertyChanged(nameof(DetailStatusBadgeBackground));
            OnPropertyChanged(nameof(DetailStatusBadgeForeground));
            OnPropertyChanged(nameof(DetailStatusDotBrush));
            OnPropertyChanged(nameof(FormCardTitle));
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(CancelButtonText));
            OnPropertyChanged(nameof(ReadNameText));
            OnPropertyChanged(nameof(ReadAddressText));
            OnPropertyChanged(nameof(ReadDescriptionText));
            OnPropertyChanged(nameof(ReadPhoneText));
            OnPropertyChanged(nameof(ReadCreatedAtText));
            OnPropertyChanged(nameof(CurrentLots));
            OnPropertyChanged(nameof(CurrentDetailsHasLots));
        }

        partial void OnSearchQueryChanged(string value) => ApplyFilters();

        partial void OnShowActiveOnlyChanged(bool value) => ApplyFilters();

        partial void OnSelectedStatusFilterChanged(FoodPointStatusFilterOption? value) => ApplyFilters();

        partial void OnCurrentDetailsChanged(FoodPointDetailsViewModel? value) => RefreshScreenState();

        partial void OnIsCreateModeChanged(bool value) => RefreshScreenState();

        partial void OnIsEditingChanged(bool value) => RefreshScreenState();

        partial void OnIsBusyChanged(bool value) => RefreshScreenState();

        partial void OnHasAnyFoodPointsChanged(bool value) => RefreshScreenState();

        partial void OnHasVisibleFoodPointsChanged(bool value) => RefreshScreenState();

        partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));

        partial void OnLatitudeChanged(double? value)
        {
            if (!_isUpdatingCoordinates)
                RefreshCoordinateState();
        }

        partial void OnLongitudeChanged(double? value)
        {
            if (!_isUpdatingCoordinates)
                RefreshCoordinateState();
        }

        private void UpdateCoordinates(double? latitude, double? longitude)
        {
            if (Latitude == latitude && Longitude == longitude)
                return;

            _isUpdatingCoordinates = true;
            try
            {
                Latitude = latitude;
                Longitude = longitude;
            }
            finally
            {
                _isUpdatingCoordinates = false;
            }

            RefreshCoordinateState();
        }

        private void RefreshCoordinateState()
        {
            OnPropertyChanged(nameof(HasSelectedCoordinates));
            OnPropertyChanged(nameof(CoordinatesStatusText));
            OnPropertyChanged(nameof(CoordinatesTechnicalText));
        }
    }

    public sealed class FoodPointListItemViewModel
    {
        public FoodPointListItemViewModel(FoodPoint foodPoint)
        {
            FoodPoint = foodPoint;
        }

        public FoodPoint FoodPoint { get; }

        public Guid Id => FoodPoint.Id;

        public string NameText => FoodPointDisplayFormatting.Fallback(FoodPoint.Name, "Без названия");

        public string AddressText => FoodPointDisplayFormatting.Fallback(FoodPoint.Address, "Адрес не указан");

        public bool IsActive => FoodPoint.IsActive;

        public bool CanCreateLot => IsActive;

        public string StatusText => FoodPointDisplayFormatting.GetStatusText(IsActive);

        public IBrush StatusBadgeBackground => FoodPointDisplayFormatting.GetStatusBackground(IsActive);

        public IBrush StatusBadgeForeground => FoodPointDisplayFormatting.GetStatusForeground(IsActive);

        public IBrush StatusDotBrush => FoodPointDisplayFormatting.GetStatusAccent(IsActive);

        public double InactiveOpacity => IsActive ? 1 : 0.72;
    }

    public sealed class FoodPointDetailsViewModel : ViewModelBase
    {
        public FoodPointDetailsViewModel(FoodPoint foodPoint, IReadOnlyList<FoodLot> lots)
        {
            FoodPoint = foodPoint;
            Lots = lots
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new FoodPointLotItemViewModel(x))
                .ToList();
        }

        public FoodPoint FoodPoint { get; }

        public Guid Id => FoodPoint.Id;

        public string NameText => FoodPointDisplayFormatting.Fallback(FoodPoint.Name, "Без названия");

        public string AddressText => FoodPointDisplayFormatting.Fallback(FoodPoint.Address, "Адрес не указан");

        public string DescriptionText => FoodPointDisplayFormatting.Fallback(FoodPoint.Description, "Описание не добавлено");

        public string PhoneText => FoodPointDisplayFormatting.Fallback(FoodPoint.Phone, "Телефон не указан");

        public bool IsActive => FoodPoint.IsActive;

        public string StatusText => FoodPointDisplayFormatting.GetStatusText(IsActive);

        public IBrush StatusBadgeBackground => FoodPointDisplayFormatting.GetStatusBackground(IsActive);

        public IBrush StatusBadgeForeground => FoodPointDisplayFormatting.GetStatusForeground(IsActive);

        public IBrush StatusDotBrush => FoodPointDisplayFormatting.GetStatusAccent(IsActive);

        public string CreatedAtText => FoodPointDisplayFormatting.FormatFullDateTime(FoodPoint.CreatedAt);

        public IReadOnlyList<FoodPointLotItemViewModel> Lots { get; }

        public bool HasLots => Lots.Count > 0;
    }

    public sealed class FoodPointLotItemViewModel
    {
        public FoodPointLotItemViewModel(FoodLot lot)
        {
            Lot = lot;
        }

        public FoodLot Lot { get; }

        public Guid Id => Lot.Id;

        public string Title => FoodPointDisplayFormatting.Fallback(Lot.Title, "Без названия");

        public string StatusText => LotDisplayFormatting.GetVisualStatusText(Lot);

        public IBrush StatusBadgeBackground => LotDisplayFormatting.GetStatusBackground(Lot);

        public IBrush StatusBadgeForeground => LotDisplayFormatting.GetStatusForeground(Lot);

        public IBrush StatusDotBrush => LotDisplayFormatting.GetStatusAccent(Lot);

        public string AvailableText => $"{Lot.AvailableQuantity} шт.";

        public string PriceText => Lot.AvailableQuantity <= 0 ? "-" : LotDisplayFormatting.FormatPrice(Lot.Price);
    }

    public sealed class FoodPointStatusFilterOption
    {
        public FoodPointStatusFilterOption(FoodPointStatusFilterKind kind, string name)
        {
            Kind = kind;
            Name = name;
        }

        public FoodPointStatusFilterKind Kind { get; }

        public string Name { get; }
    }

    public enum FoodPointStatusFilterKind
    {
        All,
        Active,
        Inactive
    }

    internal static class FoodPointDisplayFormatting
    {
        private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

        public static readonly IBrush GreenBrush = SolidColorBrush.Parse("#78D957");
        public static readonly IBrush GreenSoftBrush = SolidColorBrush.Parse("#1D3825");
        public static readonly IBrush NeutralBrush = SolidColorBrush.Parse("#9AA5A0");
        public static readonly IBrush NeutralSoftBrush = SolidColorBrush.Parse("#252D2B");

        public static string Fallback(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        public static string GetStatusText(bool isActive)
        {
            return isActive ? "Активна" : "Деактивирована";
        }

        public static IBrush GetStatusAccent(bool isActive)
        {
            return isActive ? GreenBrush : NeutralBrush;
        }

        public static IBrush GetStatusBackground(bool isActive)
        {
            return isActive ? GreenSoftBrush : NeutralSoftBrush;
        }

        public static IBrush GetStatusForeground(bool isActive)
        {
            return isActive ? GreenBrush : NeutralBrush;
        }

        public static string FormatFullDateTime(DateTime dateTimeUtc)
        {
            if (dateTimeUtc == default)
                return "Дата создания недоступна";

            return dateTimeUtc.ToLocalTime().ToString("dd.MM.yyyy, HH:mm", RussianCulture);
        }
    }
}
