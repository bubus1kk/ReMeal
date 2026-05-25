using Application.DTOs.Admin;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ReMealApp.ViewModels.Admin
{
    public partial class AdminPanelViewModel : ViewModelBase
    {
        private const int UsersPageSize = 4;
        private const int FoodPointsPageSize = 3;
        private const int LotsPageSize = 4;
        private const int BookingsPageSize = 3;

        private readonly IAdminService _adminService;
        private readonly List<AdminUserDto> _allUsers = new();
        private readonly List<AdminFoodPointDto> _allFoodPoints = new();
        private readonly List<AdminLotDto> _allLots = new();
        private readonly List<AdminBookingDto> _allBookings = new();
        private int _filteredUsersCount;
        private int _filteredFoodPointsCount;
        private int _filteredLotsCount;
        private int _filteredBookingsCount;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _hasAccess;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _userSearchQuery = string.Empty;

        [ObservableProperty]
        private string _foodPointSearchQuery = string.Empty;

        [ObservableProperty]
        private string _lotSearchQuery = string.Empty;

        [ObservableProperty]
        private string _bookingSearchQuery = string.Empty;

        [ObservableProperty]
        private AdminRoleFilterOption? _selectedRoleFilter;

        [ObservableProperty]
        private AdminActivityFilterOption? _selectedUserActivityFilter;

        [ObservableProperty]
        private AdminActivityFilterOption? _selectedFoodPointActivityFilter;

        [ObservableProperty]
        private AdminLotStatusFilterOption? _selectedLotStatusFilter;

        [ObservableProperty]
        private AdminBookingStatusFilterOption? _selectedBookingStatusFilter;

        [ObservableProperty]
        private AdminBookingPeriodFilterOption? _selectedBookingPeriodFilter;

        [ObservableProperty]
        private string _usersKpiValue = "Нет данных";

        [ObservableProperty]
        private string _usersKpiSubtitle = "Статистика недоступна";

        [ObservableProperty]
        private string _foodPointsKpiValue = "Нет данных";

        [ObservableProperty]
        private string _foodPointsKpiSubtitle = "Статистика недоступна";

        [ObservableProperty]
        private string _lotsKpiValue = "Нет данных";

        [ObservableProperty]
        private string _lotsKpiSubtitle = "Статистика недоступна";

        [ObservableProperty]
        private string _bookingsKpiValue = "Нет данных";

        [ObservableProperty]
        private string _bookingsKpiSubtitle = "Статистика недоступна";

        [ObservableProperty]
        private int _usersPage = 1;

        [ObservableProperty]
        private int _foodPointsPage = 1;

        [ObservableProperty]
        private int _lotsPage = 1;

        [ObservableProperty]
        private int _bookingsPage = 1;

        public AdminPanelViewModel(IAdminService adminService)
        {
            _adminService = adminService;

            RoleFilters =
            [
                new AdminRoleFilterOption(null, "Все роли"),
                new AdminRoleFilterOption(UserRole.StudentCustomer, "Покупатель"),
                new AdminRoleFilterOption(UserRole.FoodPointRepresentative, "Представитель точки"),
                new AdminRoleFilterOption(UserRole.Administrator, "Администратор")
            ];

            ActivityFilters =
            [
                new AdminActivityFilterOption(null, "Все статусы"),
                new AdminActivityFilterOption(true, "Активные"),
                new AdminActivityFilterOption(false, "Деактивированные")
            ];

            LotStatusFilters =
            [
                new AdminLotStatusFilterOption(null, "Все статусы"),
                new AdminLotStatusFilterOption(LotStatus.Active, "Активен"),
                new AdminLotStatusFilterOption(LotStatus.SoldOut, "Распродан"),
                new AdminLotStatusFilterOption(LotStatus.Expired, "Истек"),
                new AdminLotStatusFilterOption(LotStatus.Cancelled, "Отменен")
            ];

            BookingStatusFilters =
            [
                new AdminBookingStatusFilterOption(null, "Все статусы"),
                new AdminBookingStatusFilterOption(BookingStatus.Active, "Активна"),
                new AdminBookingStatusFilterOption(BookingStatus.Cancelled, "Отменена"),
                new AdminBookingStatusFilterOption(BookingStatus.Issued, "Выдана")
            ];

            BookingPeriodFilters =
            [
                new AdminBookingPeriodFilterOption(AdminBookingPeriodKind.All, "Все время"),
                new AdminBookingPeriodFilterOption(AdminBookingPeriodKind.Last30Days, "Последние 30 дней"),
                new AdminBookingPeriodFilterOption(AdminBookingPeriodKind.Last7Days, "Последние 7 дней"),
                new AdminBookingPeriodFilterOption(AdminBookingPeriodKind.Today, "Сегодня")
            ];

            _selectedRoleFilter = RoleFilters[0];
            _selectedUserActivityFilter = ActivityFilters[0];
            _selectedFoodPointActivityFilter = ActivityFilters[0];
            _selectedLotStatusFilter = LotStatusFilters[0];
            _selectedBookingStatusFilter = BookingStatusFilters[0];
            _selectedBookingPeriodFilter = BookingPeriodFilters[0];
        }

        public ObservableCollection<AdminUserRowViewModel> Users { get; } = new();

        public ObservableCollection<AdminFoodPointRowViewModel> FoodPoints { get; } = new();

        public ObservableCollection<AdminLotRowViewModel> Lots { get; } = new();

        public ObservableCollection<AdminBookingRowViewModel> Bookings { get; } = new();

        public ObservableCollection<AdminRoleFilterOption> RoleFilters { get; }

        public ObservableCollection<AdminActivityFilterOption> ActivityFilters { get; }

        public ObservableCollection<AdminLotStatusFilterOption> LotStatusFilters { get; }

        public ObservableCollection<AdminBookingStatusFilterOption> BookingStatusFilters { get; }

        public ObservableCollection<AdminBookingPeriodFilterOption> BookingPeriodFilters { get; }

        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

        public bool HasUsers => Users.Count > 0;

        public bool HasFoodPoints => FoodPoints.Count > 0;

        public bool HasLots => Lots.Count > 0;

        public bool HasBookings => Bookings.Count > 0;

        public bool IsUsersEmpty => HasAccess && !HasUsers;

        public bool IsFoodPointsEmpty => HasAccess && !HasFoodPoints;

        public bool IsLotsEmpty => HasAccess && !HasLots;

        public bool IsBookingsEmpty => HasAccess && !HasBookings;

        public bool CanResetUserFilters => IsUserFilteringApplied;

        public bool CanResetFoodPointFilters => IsFoodPointFilteringApplied;

        public bool CanResetLotFilters => IsLotFilteringApplied;

        public bool CanResetBookingFilters => IsBookingFilteringApplied;

        public int UsersTotalPages => GetTotalPages(_filteredUsersCount, UsersPageSize);

        public int FoodPointsTotalPages => GetTotalPages(_filteredFoodPointsCount, FoodPointsPageSize);

        public int LotsTotalPages => GetTotalPages(_filteredLotsCount, LotsPageSize);

        public int BookingsTotalPages => GetTotalPages(_filteredBookingsCount, BookingsPageSize);

        public bool CanGoToPreviousUsersPage => UsersPage > 1;

        public bool CanGoToNextUsersPage => UsersPage < UsersTotalPages;

        public bool CanGoToPreviousFoodPointsPage => FoodPointsPage > 1;

        public bool CanGoToNextFoodPointsPage => FoodPointsPage < FoodPointsTotalPages;

        public bool CanGoToPreviousLotsPage => LotsPage > 1;

        public bool CanGoToNextLotsPage => LotsPage < LotsTotalPages;

        public bool CanGoToPreviousBookingsPage => BookingsPage > 1;

        public bool CanGoToNextBookingsPage => BookingsPage < BookingsTotalPages;

        public bool HasUsersPagination => HasUsers && UsersTotalPages > 1;

        public bool HasFoodPointsPagination => HasFoodPoints && FoodPointsTotalPages > 1;

        public bool HasLotsPagination => HasLots && LotsTotalPages > 1;

        public bool HasBookingsPagination => HasBookings && BookingsTotalPages > 1;

        public string UsersEmptyTitle => _allUsers.Count == 0 ? "Пользователи не найдены" : "Ничего не найдено";

        public string FoodPointsEmptyTitle => _allFoodPoints.Count == 0 ? "Точки питания не найдены" : "Ничего не найдено";

        public string LotsEmptyTitle => _allLots.Count == 0 ? "Лоты не найдены" : "Ничего не найдено";

        public string BookingsEmptyTitle => _allBookings.Count == 0 ? "Бронирования не найдены" : "Ничего не найдено";

        public string UsersEmptySubtitle => _allUsers.Count == 0
            ? "Нет данных для отображения"
            : "Измените поисковый запрос или сбросьте фильтры.";

        public string FoodPointsEmptySubtitle => _allFoodPoints.Count == 0
            ? "Нет данных для отображения"
            : "Измените поисковый запрос или сбросьте фильтры.";

        public string LotsEmptySubtitle => _allLots.Count == 0
            ? "Нет данных для отображения"
            : "Измените поисковый запрос или сбросьте фильтры.";

        public string BookingsEmptySubtitle => _allBookings.Count == 0
            ? "Нет данных для отображения"
            : "Измените поисковый запрос или сбросьте фильтры.";

        public string UsersCountText => HasUsers
            ? BuildPageText(UsersPage, UsersPageSize, Users.Count, _filteredUsersCount)
            : string.Empty;

        public string FoodPointsCountText => HasFoodPoints
            ? BuildPageText(FoodPointsPage, FoodPointsPageSize, FoodPoints.Count, _filteredFoodPointsCount)
            : string.Empty;

        public string LotsCountText => HasLots
            ? BuildPageText(LotsPage, LotsPageSize, Lots.Count, _filteredLotsCount)
            : string.Empty;

        public string BookingsCountText => HasBookings
            ? BuildPageText(BookingsPage, BookingsPageSize, Bookings.Count, _filteredBookingsCount)
            : string.Empty;

        private bool IsUserFilteringApplied =>
            !string.IsNullOrWhiteSpace(UserSearchQuery)
            || SelectedRoleFilter?.Role is not null
            || SelectedUserActivityFilter?.IsActive is not null;

        private bool IsFoodPointFilteringApplied =>
            !string.IsNullOrWhiteSpace(FoodPointSearchQuery)
            || SelectedFoodPointActivityFilter?.IsActive is not null;

        private bool IsLotFilteringApplied =>
            !string.IsNullOrWhiteSpace(LotSearchQuery)
            || SelectedLotStatusFilter?.Status is not null;

        private bool IsBookingFilteringApplied =>
            !string.IsNullOrWhiteSpace(BookingSearchQuery)
            || SelectedBookingStatusFilter?.Status is not null
            || SelectedBookingPeriodFilter?.Kind != AdminBookingPeriodKind.All;

        public async Task InitializeAsync()
        {
            await LoadAsync();
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                var users = await _adminService.GetUsersAsync();
                var foodPoints = await _adminService.GetFoodPointsAsync();
                var lots = await _adminService.GetLotsAsync();
                var bookings = await _adminService.GetBookingsAsync();

                _allUsers.Clear();
                _allUsers.AddRange(users);
                _allFoodPoints.Clear();
                _allFoodPoints.AddRange(foodPoints);
                _allLots.Clear();
                _allLots.AddRange(lots);
                _allBookings.Clear();
                _allBookings.AddRange(bookings);

                HasAccess = true;
                UpdateKpis();
                ApplyUserFilter(resetPage: true);
                ApplyFoodPoints(resetPage: true);
                ApplyLots(resetPage: true);
                ApplyBookings(resetPage: true);

                StatusMessage = "Данные администрирования обновлены.";
            }
            catch (Exception ex)
            {
                HasAccess = false;
                ClearVisibleData();
                ResetKpis();
                StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnStatusMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasStatusMessage));
        }

        partial void OnHasAccessChanged(bool value)
        {
            NotifyUsersStateChanged();
            NotifyFoodPointsStateChanged();
            NotifyLotsStateChanged();
            NotifyBookingsStateChanged();
        }

        partial void OnUserSearchQueryChanged(string value)
        {
            ApplyUserFilter(resetPage: true);
        }

        partial void OnFoodPointSearchQueryChanged(string value)
        {
            ApplyFoodPoints(resetPage: true);
        }

        partial void OnLotSearchQueryChanged(string value)
        {
            ApplyLots(resetPage: true);
        }

        partial void OnBookingSearchQueryChanged(string value)
        {
            ApplyBookings(resetPage: true);
        }

        partial void OnSelectedRoleFilterChanged(AdminRoleFilterOption? value)
        {
            ApplyUserFilter(resetPage: true);
        }

        partial void OnSelectedUserActivityFilterChanged(AdminActivityFilterOption? value)
        {
            ApplyUserFilter(resetPage: true);
        }

        partial void OnSelectedFoodPointActivityFilterChanged(AdminActivityFilterOption? value)
        {
            ApplyFoodPoints(resetPage: true);
        }

        partial void OnSelectedLotStatusFilterChanged(AdminLotStatusFilterOption? value)
        {
            ApplyLots(resetPage: true);
        }

        partial void OnSelectedBookingStatusFilterChanged(AdminBookingStatusFilterOption? value)
        {
            ApplyBookings(resetPage: true);
        }

        partial void OnSelectedBookingPeriodFilterChanged(AdminBookingPeriodFilterOption? value)
        {
            ApplyBookings(resetPage: true);
        }

        [RelayCommand]
        private void ResetUserFilters()
        {
            UserSearchQuery = string.Empty;
            SelectedRoleFilter = RoleFilters[0];
            SelectedUserActivityFilter = ActivityFilters[0];
            ApplyUserFilter(resetPage: true);
        }

        [RelayCommand]
        private void ResetFoodPointFilters()
        {
            FoodPointSearchQuery = string.Empty;
            SelectedFoodPointActivityFilter = ActivityFilters[0];
            ApplyFoodPoints(resetPage: true);
        }

        [RelayCommand]
        private void ResetLotFilters()
        {
            LotSearchQuery = string.Empty;
            SelectedLotStatusFilter = LotStatusFilters[0];
            ApplyLots(resetPage: true);
        }

        [RelayCommand]
        private void ResetBookingFilters()
        {
            BookingSearchQuery = string.Empty;
            SelectedBookingStatusFilter = BookingStatusFilters[0];
            SelectedBookingPeriodFilter = BookingPeriodFilters[0];
            ApplyBookings(resetPage: true);
        }

        [RelayCommand]
        private void PreviousUsersPage()
        {
            if (!CanGoToPreviousUsersPage)
                return;

            UsersPage--;
            ApplyUserFilter();
        }

        [RelayCommand]
        private void NextUsersPage()
        {
            if (!CanGoToNextUsersPage)
                return;

            UsersPage++;
            ApplyUserFilter();
        }

        [RelayCommand]
        private void PreviousFoodPointsPage()
        {
            if (!CanGoToPreviousFoodPointsPage)
                return;

            FoodPointsPage--;
            ApplyFoodPoints();
        }

        [RelayCommand]
        private void NextFoodPointsPage()
        {
            if (!CanGoToNextFoodPointsPage)
                return;

            FoodPointsPage++;
            ApplyFoodPoints();
        }

        [RelayCommand]
        private void PreviousLotsPage()
        {
            if (!CanGoToPreviousLotsPage)
                return;

            LotsPage--;
            ApplyLots();
        }

        [RelayCommand]
        private void NextLotsPage()
        {
            if (!CanGoToNextLotsPage)
                return;

            LotsPage++;
            ApplyLots();
        }

        [RelayCommand]
        private void PreviousBookingsPage()
        {
            if (!CanGoToPreviousBookingsPage)
                return;

            BookingsPage--;
            ApplyBookings();
        }

        [RelayCommand]
        private void NextBookingsPage()
        {
            if (!CanGoToNextBookingsPage)
                return;

            BookingsPage++;
            ApplyBookings();
        }

        private void ApplyUserFilter(bool resetPage = false)
        {
            var query = UserSearchQuery.Trim();
            var role = SelectedRoleFilter?.Role;
            var isActive = SelectedUserActivityFilter?.IsActive;

            var visibleUsers = _allUsers
                .Where(user => role is null || user.Role == role)
                .Where(user => isActive is null || user.IsActive == isActive)
                .Where(user => string.IsNullOrWhiteSpace(query)
                    || Contains(user.Login, query)
                    || Contains(user.FullName, query)
                    || Contains(user.Email, query))
                .Select(AdminUserRowViewModel.FromDto)
                .ToList();

            _filteredUsersCount = visibleUsers.Count;
            UsersPage = GetValidPage(UsersPage, _filteredUsersCount, UsersPageSize, resetPage);

            Users.Clear();
            foreach (var user in GetPageItems(visibleUsers, UsersPage, UsersPageSize))
                Users.Add(user);

            NotifyUsersStateChanged();
        }

        [RelayCommand]
        private async Task ActivateFoodPointAsync(Guid foodPointId)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await _adminService.ActivateFoodPointAsync(foodPointId);
                await RefreshFoodPointsAsync();
                StatusMessage = "Точка питания активирована.";
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
        private async Task DeactivateFoodPointAsync(Guid foodPointId)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await _adminService.DeactivateFoodPointAsync(foodPointId);
                await RefreshFoodPointsAsync();
                StatusMessage = "Точка питания деактивирована.";
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

        private async Task RefreshFoodPointsAsync()
        {
            var foodPoints = await _adminService.GetFoodPointsAsync();
            _allFoodPoints.Clear();
            _allFoodPoints.AddRange(foodPoints);
            ApplyFoodPoints();
            UpdateKpis();
        }

        private void ApplyFoodPoints(bool resetPage = false)
        {
            var query = FoodPointSearchQuery.Trim();
            var isActive = SelectedFoodPointActivityFilter?.IsActive;

            var visibleFoodPoints = _allFoodPoints
                .Where(foodPoint => isActive is null || foodPoint.IsActive == isActive)
                .Where(foodPoint => string.IsNullOrWhiteSpace(query)
                    || Contains(foodPoint.Name, query)
                    || Contains(foodPoint.Address, query))
                .Select(AdminFoodPointRowViewModel.FromDto)
                .ToList();

            _filteredFoodPointsCount = visibleFoodPoints.Count;
            FoodPointsPage = GetValidPage(FoodPointsPage, _filteredFoodPointsCount, FoodPointsPageSize, resetPage);

            FoodPoints.Clear();
            foreach (var foodPoint in GetPageItems(visibleFoodPoints, FoodPointsPage, FoodPointsPageSize))
                FoodPoints.Add(foodPoint);

            NotifyFoodPointsStateChanged();
        }

        [RelayCommand]
        private async Task ActivateUserAsync(Guid userId)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await _adminService.ActivateUserAsync(userId);
                await RefreshUsersAsync();
                StatusMessage = "Пользователь активирован.";
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
        private async Task DeactivateUserAsync(Guid userId)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await _adminService.DeactivateUserAsync(userId);
                await RefreshUsersAsync();
                StatusMessage = "Пользователь деактивирован.";
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

        private async Task RefreshUsersAsync()
        {
            var users = await _adminService.GetUsersAsync();
            _allUsers.Clear();
            _allUsers.AddRange(users);
            ApplyUserFilter();
            UpdateKpis();
        }

        [RelayCommand]
        private async Task CancelLotAsync(Guid lotId)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await _adminService.CancelLotAsync(lotId);
                await RefreshLotsAsync();
                StatusMessage = "Лот отменен.";
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

        private async Task RefreshLotsAsync()
        {
            var lots = await _adminService.GetLotsAsync();
            _allLots.Clear();
            _allLots.AddRange(lots);
            ApplyLots();
            UpdateKpis();
        }

        private void ApplyLots(bool resetPage = false)
        {
            var query = LotSearchQuery.Trim();
            var status = SelectedLotStatusFilter?.Status;

            var visibleLots = _allLots
                .Where(lot => status is null || lot.Status == status)
                .Where(lot => string.IsNullOrWhiteSpace(query)
                    || Contains(lot.Title, query)
                    || Contains(lot.OwnerName, query)
                    || Contains(lot.FoodPointName, query))
                .Select(AdminLotRowViewModel.FromDto)
                .ToList();

            _filteredLotsCount = visibleLots.Count;
            LotsPage = GetValidPage(LotsPage, _filteredLotsCount, LotsPageSize, resetPage);

            Lots.Clear();
            foreach (var lot in GetPageItems(visibleLots, LotsPage, LotsPageSize))
                Lots.Add(lot);

            NotifyLotsStateChanged();
        }

        private void ApplyBookings(bool resetPage = false)
        {
            var query = BookingSearchQuery.Trim();
            var status = SelectedBookingStatusFilter?.Status;
            var period = SelectedBookingPeriodFilter?.Kind ?? AdminBookingPeriodKind.All;
            var now = DateTime.UtcNow;

            var visibleBookings = _allBookings
                .Where(booking => status is null || booking.Status == status)
                .Where(booking => MatchesBookingPeriod(booking.ReservedAt, period, now))
                .Where(booking => string.IsNullOrWhiteSpace(query)
                    || Contains(booking.CustomerName, query)
                    || Contains(booking.LotTitle, query)
                    || Contains(booking.FoodPointName, query))
                .Select(AdminBookingRowViewModel.FromDto)
                .ToList();

            _filteredBookingsCount = visibleBookings.Count;
            BookingsPage = GetValidPage(BookingsPage, _filteredBookingsCount, BookingsPageSize, resetPage);

            Bookings.Clear();
            foreach (var booking in GetPageItems(visibleBookings, BookingsPage, BookingsPageSize))
                Bookings.Add(booking);

            NotifyBookingsStateChanged();
        }

        private void UpdateKpis()
        {
            UsersKpiValue = _allUsers.Count.ToString(CultureInfo.InvariantCulture);
            UsersKpiSubtitle = $"Активных: {_allUsers.Count(user => user.IsActive)}";

            FoodPointsKpiValue = _allFoodPoints.Count.ToString(CultureInfo.InvariantCulture);
            FoodPointsKpiSubtitle = $"Активных: {_allFoodPoints.Count(foodPoint => foodPoint.IsActive)}";

            LotsKpiValue = _allLots.Count.ToString(CultureInfo.InvariantCulture);
            LotsKpiSubtitle = $"Активных: {_allLots.Count(lot => lot.Status == LotStatus.Active)}";

            BookingsKpiValue = _allBookings.Count.ToString(CultureInfo.InvariantCulture);
            BookingsKpiSubtitle = "Всего в базе";
        }

        private void ResetKpis()
        {
            UsersKpiValue = "Нет данных";
            UsersKpiSubtitle = "Статистика недоступна";
            FoodPointsKpiValue = "Нет данных";
            FoodPointsKpiSubtitle = "Статистика недоступна";
            LotsKpiValue = "Нет данных";
            LotsKpiSubtitle = "Статистика недоступна";
            BookingsKpiValue = "Нет данных";
            BookingsKpiSubtitle = "Статистика недоступна";
        }

        private void ClearVisibleData()
        {
            _allUsers.Clear();
            _allFoodPoints.Clear();
            _allLots.Clear();
            _allBookings.Clear();
            Users.Clear();
            FoodPoints.Clear();
            Lots.Clear();
            Bookings.Clear();
            NotifyUsersStateChanged();
            NotifyFoodPointsStateChanged();
            NotifyLotsStateChanged();
            NotifyBookingsStateChanged();
        }

        private void NotifyUsersStateChanged()
        {
            OnPropertyChanged(nameof(HasUsers));
            OnPropertyChanged(nameof(IsUsersEmpty));
            OnPropertyChanged(nameof(CanResetUserFilters));
            OnPropertyChanged(nameof(UsersTotalPages));
            OnPropertyChanged(nameof(CanGoToPreviousUsersPage));
            OnPropertyChanged(nameof(CanGoToNextUsersPage));
            OnPropertyChanged(nameof(HasUsersPagination));
            OnPropertyChanged(nameof(UsersEmptyTitle));
            OnPropertyChanged(nameof(UsersEmptySubtitle));
            OnPropertyChanged(nameof(UsersCountText));
        }

        private void NotifyFoodPointsStateChanged()
        {
            OnPropertyChanged(nameof(HasFoodPoints));
            OnPropertyChanged(nameof(IsFoodPointsEmpty));
            OnPropertyChanged(nameof(CanResetFoodPointFilters));
            OnPropertyChanged(nameof(FoodPointsTotalPages));
            OnPropertyChanged(nameof(CanGoToPreviousFoodPointsPage));
            OnPropertyChanged(nameof(CanGoToNextFoodPointsPage));
            OnPropertyChanged(nameof(HasFoodPointsPagination));
            OnPropertyChanged(nameof(FoodPointsEmptyTitle));
            OnPropertyChanged(nameof(FoodPointsEmptySubtitle));
            OnPropertyChanged(nameof(FoodPointsCountText));
        }

        private void NotifyLotsStateChanged()
        {
            OnPropertyChanged(nameof(HasLots));
            OnPropertyChanged(nameof(IsLotsEmpty));
            OnPropertyChanged(nameof(CanResetLotFilters));
            OnPropertyChanged(nameof(LotsTotalPages));
            OnPropertyChanged(nameof(CanGoToPreviousLotsPage));
            OnPropertyChanged(nameof(CanGoToNextLotsPage));
            OnPropertyChanged(nameof(HasLotsPagination));
            OnPropertyChanged(nameof(LotsEmptyTitle));
            OnPropertyChanged(nameof(LotsEmptySubtitle));
            OnPropertyChanged(nameof(LotsCountText));
        }

        private void NotifyBookingsStateChanged()
        {
            OnPropertyChanged(nameof(HasBookings));
            OnPropertyChanged(nameof(IsBookingsEmpty));
            OnPropertyChanged(nameof(CanResetBookingFilters));
            OnPropertyChanged(nameof(BookingsTotalPages));
            OnPropertyChanged(nameof(CanGoToPreviousBookingsPage));
            OnPropertyChanged(nameof(CanGoToNextBookingsPage));
            OnPropertyChanged(nameof(HasBookingsPagination));
            OnPropertyChanged(nameof(BookingsEmptyTitle));
            OnPropertyChanged(nameof(BookingsEmptySubtitle));
            OnPropertyChanged(nameof(BookingsCountText));
        }

        private static IReadOnlyList<T> GetPageItems<T>(
            IReadOnlyList<T> items,
            int page,
            int pageSize)
        {
            return items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        private static int GetValidPage(
            int currentPage,
            int itemCount,
            int pageSize,
            bool resetPage)
        {
            if (itemCount <= 0 || resetPage)
                return 1;

            return Math.Clamp(currentPage, 1, GetTotalPages(itemCount, pageSize));
        }

        private static int GetTotalPages(int itemCount, int pageSize)
        {
            return Math.Max(1, (int)Math.Ceiling(itemCount / (double)pageSize));
        }

        private static string BuildPageText(
            int page,
            int pageSize,
            int visibleCount,
            int totalCount)
        {
            if (visibleCount <= 0 || totalCount <= 0)
                return string.Empty;

            var first = ((page - 1) * pageSize) + 1;
            var last = Math.Min(first + visibleCount - 1, totalCount);
            return $"Показано {first}-{last} из {totalCount}";
        }

        private static bool Contains(string value, string query)
        {
            return value.Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesBookingPeriod(
            DateTime reservedAt,
            AdminBookingPeriodKind period,
            DateTime now)
        {
            return period switch
            {
                AdminBookingPeriodKind.Last30Days => reservedAt >= now.AddDays(-30),
                AdminBookingPeriodKind.Last7Days => reservedAt >= now.AddDays(-7),
                AdminBookingPeriodKind.Today => reservedAt.Date == now.Date,
                _ => true
            };
        }
    }

    public sealed class AdminUserRowViewModel
    {
        public Guid Id { get; init; }

        public string Login { get; init; } = string.Empty;

        public string FullName { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string Phone { get; init; } = string.Empty;

        public string RoleText { get; init; } = string.Empty;

        public string ActivityStatusText { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public bool IsInactive => !IsActive;

        public bool CanActivate => !IsActive;

        public bool CanDeactivate => IsActive;

        public static AdminUserRowViewModel FromDto(AdminUserDto dto)
        {
            return new AdminUserRowViewModel
            {
                Id = dto.Id,
                Login = DisplayOrFallback(dto.Login),
                FullName = DisplayOrFallback(dto.FullName),
                Email = DisplayOrFallback(dto.Email),
                Phone = DisplayOrFallback(dto.Phone),
                RoleText = DisplayOrFallback(dto.RoleText),
                ActivityStatusText = dto.ActivityStatusText,
                IsActive = dto.IsActive
            };
        }

        private static string DisplayOrFallback(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Не указано" : value.Trim();
        }
    }

    public sealed class AdminFoodPointRowViewModel
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Address { get; init; } = string.Empty;

        public string Phone { get; init; } = string.Empty;

        public string OwnerText { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public bool IsInactive => !IsActive;

        public string StatusText { get; init; } = string.Empty;

        public bool CanActivate => !IsActive;

        public bool CanDeactivate => IsActive;

        public static AdminFoodPointRowViewModel FromDto(AdminFoodPointDto dto)
        {
            var ownerName = DisplayOrFallback(dto.OwnerName, "Владелец не указан");
            var ownerText = string.IsNullOrWhiteSpace(dto.OwnerLogin)
                ? ownerName
                : $"{ownerName} ({dto.OwnerLogin.Trim()})";

            return new AdminFoodPointRowViewModel
            {
                Id = dto.Id,
                Name = DisplayOrFallback(dto.Name),
                Address = DisplayOrFallback(dto.Address, "Адрес не указан"),
                Phone = DisplayOrFallback(dto.Phone, "Не указан"),
                OwnerText = ownerText,
                IsActive = dto.IsActive,
                StatusText = dto.StatusText
            };
        }

        private static string DisplayOrFallback(string value, string fallback = "Не указано")
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }

    public sealed class AdminLotRowViewModel
    {
        private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

        public Guid Id { get; init; }

        public string Title { get; init; } = string.Empty;

        public string FoodPointName { get; init; } = string.Empty;

        public string OwnerName { get; init; } = string.Empty;

        public decimal Price { get; init; }

        public int TotalQuantity { get; init; }

        public int AvailableQuantity { get; init; }

        public DateTime PickupDeadline { get; init; }

        public LotStatus Status { get; init; }

        public string StatusText { get; init; } = string.Empty;

        public bool CanCancel { get; init; }

        public bool IsActiveStatus => Status == LotStatus.Active;

        public bool IsSoldOutStatus => Status == LotStatus.SoldOut;

        public bool IsExpiredStatus => Status == LotStatus.Expired;

        public bool IsCancelledStatus => Status == LotStatus.Cancelled;

        public string QuantityText => $"{AvailableQuantity} / {TotalQuantity}";

        public string PriceText => $"{Price.ToString("N2", RuCulture)} ₽";

        public string PickupDeadlineText => PickupDeadline.ToString("dd.MM.yyyy HH:mm", RuCulture);

        public static AdminLotRowViewModel FromDto(AdminLotDto dto)
        {
            return new AdminLotRowViewModel
            {
                Id = dto.Id,
                Title = DisplayOrFallback(dto.Title),
                FoodPointName = DisplayOrFallback(dto.FoodPointName, "Не указана"),
                OwnerName = DisplayOrFallback(dto.OwnerName),
                Price = dto.Price,
                TotalQuantity = dto.TotalQuantity,
                AvailableQuantity = dto.AvailableQuantity,
                PickupDeadline = dto.PickupDeadline,
                Status = dto.Status,
                StatusText = dto.StatusText,
                CanCancel = dto.CanCancel
            };
        }

        private static string DisplayOrFallback(string value, string fallback = "Не указано")
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }

    public sealed class AdminBookingRowViewModel
    {
        private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

        public Guid Id { get; init; }

        public string CustomerName { get; init; } = string.Empty;

        public string LotTitle { get; init; } = string.Empty;

        public string FoodPointName { get; init; } = string.Empty;

        public int Quantity { get; init; }

        public decimal PriceAtReservation { get; init; }

        public decimal TotalPrice { get; init; }

        public DateTime ReservedAt { get; init; }

        public BookingStatus Status { get; init; }

        public string StatusText { get; init; } = string.Empty;

        public bool IsActiveStatus => Status == BookingStatus.Active;

        public bool IsCancelledStatus => Status == BookingStatus.Cancelled;

        public bool IsIssuedStatus => Status == BookingStatus.Issued;

        public string ReservedAtText => ReservedAt.ToString("dd.MM.yyyy HH:mm", RuCulture);

        public string TotalPriceText => $"{TotalPrice.ToString("N2", RuCulture)} ₽";

        public static AdminBookingRowViewModel FromDto(AdminBookingDto dto)
        {
            return new AdminBookingRowViewModel
            {
                Id = dto.Id,
                CustomerName = DisplayOrFallback(dto.CustomerName),
                LotTitle = DisplayOrFallback(dto.LotTitle),
                FoodPointName = DisplayOrFallback(dto.FoodPointName, "Не указана"),
                Quantity = dto.Quantity,
                PriceAtReservation = dto.PriceAtReservation,
                TotalPrice = dto.TotalPrice,
                ReservedAt = dto.ReservedAt,
                Status = dto.Status,
                StatusText = dto.StatusText
            };
        }

        private static string DisplayOrFallback(string value, string fallback = "Не указано")
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }

    public sealed class AdminRoleFilterOption
    {
        public AdminRoleFilterOption(UserRole? role, string displayName)
        {
            Role = role;
            DisplayName = displayName;
        }

        public UserRole? Role { get; }

        public string DisplayName { get; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public sealed class AdminActivityFilterOption
    {
        public AdminActivityFilterOption(bool? isActive, string displayName)
        {
            IsActive = isActive;
            DisplayName = displayName;
        }

        public bool? IsActive { get; }

        public string DisplayName { get; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public sealed class AdminLotStatusFilterOption
    {
        public AdminLotStatusFilterOption(LotStatus? status, string displayName)
        {
            Status = status;
            DisplayName = displayName;
        }

        public LotStatus? Status { get; }

        public string DisplayName { get; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public sealed class AdminBookingStatusFilterOption
    {
        public AdminBookingStatusFilterOption(BookingStatus? status, string displayName)
        {
            Status = status;
            DisplayName = displayName;
        }

        public BookingStatus? Status { get; }

        public string DisplayName { get; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public enum AdminBookingPeriodKind
    {
        All = 0,
        Last30Days = 1,
        Last7Days = 2,
        Today = 3
    }

    public sealed class AdminBookingPeriodFilterOption
    {
        public AdminBookingPeriodFilterOption(AdminBookingPeriodKind kind, string displayName)
        {
            Kind = kind;
            DisplayName = displayName;
        }

        public AdminBookingPeriodKind Kind { get; }

        public string DisplayName { get; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
