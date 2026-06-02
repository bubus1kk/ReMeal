using Application.DTOs.Admin;
using Application.DTOs.Users;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using ReMealApp.ViewModels;
using System.Collections.ObjectModel;
using System.Globalization;
using static ReMealApp.ViewModels.Shell.AdminHomeViewModel;

namespace ReMealApp.ViewModels.Shell;

public partial class AdminHomeViewModel : ViewModelBase
{
    internal static readonly CultureInfo RussianCulture =
        CultureInfo.GetCultureInfo("ru-RU");

    private readonly IAuthService _authService;
    private readonly IAdminService _adminService;
    private readonly Action<string> _navigateToSection;

    [ObservableProperty]
    private string _displayName = "Администратор";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public AdminHomeViewModel(
        IAuthService authService,
        IAdminService adminService,
        Action<string> navigateToSection)
    {
        _authService = authService;
        _adminService = adminService;
        _navigateToSection = navigateToSection;

        NavigationCards =
        [
            new AdminHomeNavigationCardViewModel(
                "Пользователи",
                "Управление аккаунтами",
                "/Assets/Icons/profile.png",
                HomeViewModel.AdminSection),
            new AdminHomeNavigationCardViewModel(
                "Точки питания",
                "Управление точками",
                "/Assets/Icons/location.png",
                HomeViewModel.AdminSection),
            new AdminHomeNavigationCardViewModel(
                "Лоты",
                "Управление лотами",
                "/Assets/Icons/sets.png",
                HomeViewModel.AdminSection),
            new AdminHomeNavigationCardViewModel(
                "Бронирования",
                "Управление бронированиями",
                "/Assets/Icons/bookings.png",
                HomeViewModel.AdminSection),
            new AdminHomeNavigationCardViewModel(
                "Администрирование",
                "Настройки системы",
                "/Assets/Icons/settings.png",
                HomeViewModel.AdminSection)
        ];

        ModuleStates =
        [
            new AdminHomeModuleStateViewModel("Пользователи", "Доступно", true),
            new AdminHomeModuleStateViewModel("Точки питания", "Доступно", true),
            new AdminHomeModuleStateViewModel("Лоты", "Доступно", true),
            new AdminHomeModuleStateViewModel("Бронирования", "Доступно", true),
            new AdminHomeModuleStateViewModel("Карты", "Доступно", true),
            new AdminHomeModuleStateViewModel("Аналитика", "Доступно", true)
        ];

        ResetKpis();
    }

    public ObservableCollection<AdminHomeNavigationCardViewModel> NavigationCards { get; }

    public ObservableCollection<AdminHomeKpiCardViewModel> KpiCards { get; } = new();

    public ObservableCollection<AdminHomeUserItemViewModel> RecentUsers { get; } = new();

    public ObservableCollection<AdminHomeFoodPointItemViewModel> FoodPoints { get; } = new();

    public ObservableCollection<AdminHomeLotWatchItemViewModel> ProblemLots { get; } = new();

    public ObservableCollection<AdminHomeModuleStateViewModel> ModuleStates { get; }

    public string HeroSubtitle =>
        $"Контролируйте пользователей, точки питания, лоты и бронирования, {DisplayName}.";

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool HasRecentUsers => RecentUsers.Count > 0;

    public bool IsRecentUsersEmpty => !IsBusy && !HasRecentUsers;

    public bool HasFoodPoints => FoodPoints.Count > 0;

    public bool IsFoodPointsEmpty => !IsBusy && !HasFoodPoints;

    public bool HasProblemLots => ProblemLots.Count > 0;

    public bool IsProblemLotsEmpty => !IsBusy && !HasProblemLots;

    public string ModulesSummary =>
        ModuleStates.All(module => module.IsAvailable)
            ? "Все доступные модули работают корректно"
            : "Некоторые модули еще не подключены";

    public string LastCheckText => "Последняя проверка недоступна";

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser is null)
            {
                ClearDashboardData();
                StatusMessage = "Пользователь не авторизован.";
                return;
            }

            DisplayName = ResolveDisplayName(currentUser);

            var users = await _adminService.GetUsersAsync();
            var foodPoints = await _adminService.GetFoodPointsAsync();
            var lots = await _adminService.GetLotsAsync();
            var bookings = await _adminService.GetBookingsAsync();

            BuildKpis(users, foodPoints, lots, bookings);
            BuildRecentUsers(users);
            BuildFoodPoints(foodPoints);
            BuildProblemLots(lots);
        }
        catch (Exception ex)
        {
            ClearDashboardData();
            ResetKpis();
            StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
        }
        finally
        {
            IsBusy = false;
            NotifyDashboardStateChanged();
        }
    }

    [RelayCommand]
    private void OpenNavigationCard(AdminHomeNavigationCardViewModel? card)
    {
        if (card is null)
            return;

        _navigateToSection(card.SectionKey);
    }

    [RelayCommand]
    private void OpenAdmin()
    {
        _navigateToSection(HomeViewModel.AdminSection);
    }

    partial void OnDisplayNameChanged(string value)
    {
        OnPropertyChanged(nameof(HeroSubtitle));
    }

    partial void OnStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    partial void OnIsBusyChanged(bool value)
    {
        NotifyDashboardStateChanged();
    }

    private void BuildKpis(
        IReadOnlyCollection<AdminUserDto> users,
        IReadOnlyCollection<AdminFoodPointDto> foodPoints,
        IReadOnlyCollection<AdminLotDto> lots,
        IReadOnlyCollection<AdminBookingDto> bookings)
    {
        KpiCards.Clear();

        var activeUsersCount = users.Count(user => user.IsActive);
        var activePartnersCount = users.Count(user =>
            user.Role == UserRole.FoodPointRepresentative && user.IsActive);
        var activeFoodPointsCount = foodPoints.Count(foodPoint => foodPoint.IsActive);
        var activeLotsCount = lots.Count(lot => lot.Status == LotStatus.Active);
        var activeBookingsCount = bookings.Count(booking => booking.Status == BookingStatus.Active);

        KpiCards.Add(new AdminHomeKpiCardViewModel(
            "Всего пользователей",
            users.Count.ToString(CultureInfo.InvariantCulture),
            $"Активных: {activeUsersCount.ToString(CultureInfo.InvariantCulture)}",
            "/Assets/Icons/profile.png"));
        KpiCards.Add(new AdminHomeKpiCardViewModel(
            "Активных партнеров",
            activePartnersCount.ToString(CultureInfo.InvariantCulture),
            "Роль представителя точки",
            "/Assets/Icons/partners.png"));
        KpiCards.Add(new AdminHomeKpiCardViewModel(
            "Активных точек питания",
            activeFoodPointsCount.ToString(CultureInfo.InvariantCulture),
            "Статус точки активен",
            "/Assets/Icons/location.png"));
        KpiCards.Add(new AdminHomeKpiCardViewModel(
            "Активных лотов",
            activeLotsCount.ToString(CultureInfo.InvariantCulture),
            "Статус лота активен",
            "/Assets/Icons/sets.png"));
        KpiCards.Add(new AdminHomeKpiCardViewModel(
            "Активных бронирований",
            activeBookingsCount.ToString(CultureInfo.InvariantCulture),
            "Статус брони активен",
            "/Assets/Icons/bookings.png"));
    }

    private void ResetKpis()
    {
        KpiCards.Clear();
        KpiCards.Add(new AdminHomeKpiCardViewModel(
            "Всего пользователей",
            "Нет данных",
            "Статистика недоступна",
            "/Assets/Icons/profile.png"));
        KpiCards.Add(new AdminHomeKpiCardViewModel(
            "Активных партнеров",
            "Нет данных",
            "Статистика недоступна",
            "/Assets/Icons/partners.png"));
        KpiCards.Add(new AdminHomeKpiCardViewModel(
            "Активных точек питания",
            "Нет данных",
            "Статистика недоступна",
            "/Assets/Icons/location.png"));
        KpiCards.Add(new AdminHomeKpiCardViewModel(
            "Активных лотов",
            "Нет данных",
            "Статистика недоступна",
            "/Assets/Icons/sets.png"));
        KpiCards.Add(new AdminHomeKpiCardViewModel(
            "Активных бронирований",
            "Нет данных",
            "Статистика недоступна",
            "/Assets/Icons/bookings.png"));
    }

    private void BuildRecentUsers(IEnumerable<AdminUserDto> users)
    {
        RecentUsers.Clear();

        foreach (var user in users.Take(5).Select(AdminHomeUserItemViewModel.FromDto))
            RecentUsers.Add(user);
    }

    private void BuildFoodPoints(IEnumerable<AdminFoodPointDto> foodPoints)
    {
        FoodPoints.Clear();

        foreach (var foodPoint in foodPoints.Take(5).Select(AdminHomeFoodPointItemViewModel.FromDto))
            FoodPoints.Add(foodPoint);
    }

    private void BuildProblemLots(IEnumerable<AdminLotDto> lots)
    {
        ProblemLots.Clear();

        var now = DateTime.Now;

        var problemLots = lots
            .Select(lot => AdminHomeLotWatchItemViewModel.TryCreate(lot, now))
            .Where(lot => lot is not null)
            .Cast<AdminHomeLotWatchItemViewModel>()
            .OrderBy(lot => lot.Priority)
            .ThenBy(lot => lot.PickupDeadline)
            .Take(5);

        foreach (var lot in problemLots)
            ProblemLots.Add(lot);
    }

    private void ClearDashboardData()
    {
        RecentUsers.Clear();
        FoodPoints.Clear();
        ProblemLots.Clear();
    }

    private void NotifyDashboardStateChanged()
    {
        OnPropertyChanged(nameof(HasRecentUsers));
        OnPropertyChanged(nameof(IsRecentUsersEmpty));
        OnPropertyChanged(nameof(HasFoodPoints));
        OnPropertyChanged(nameof(IsFoodPointsEmpty));
        OnPropertyChanged(nameof(HasProblemLots));
        OnPropertyChanged(nameof(IsProblemLotsEmpty));
        OnPropertyChanged(nameof(ModulesSummary));
    }

    private static string ResolveDisplayName(UserProfileDto user)
    {
        if (!string.IsNullOrWhiteSpace(user.FullName))
            return user.FullName.Trim();

        if (!string.IsNullOrWhiteSpace(user.Login))
            return user.Login.Trim();

        if (!string.IsNullOrWhiteSpace(user.Email))
            return user.Email.Trim();

        return "Администратор";
    }

    internal static string DisplayOrFallback(
        string value,
        string fallback = "Не указано")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    internal static string FormatDateTime(DateTime dateTime)
    {
        if (dateTime == default)
            return "Дата недоступна";

        return ToLocal(dateTime).ToString("dd.MM.yyyy HH:mm", RussianCulture);
    }

    internal static DateTime ToLocal(DateTime dateTime)
    {
        return dateTime.Kind == DateTimeKind.Utc
            ? dateTime.ToLocalTime()
            : dateTime;
    }

    internal static string FormatQuantity(AdminLotDto lot)
    {
        if (lot.TotalQuantity <= 0)
            return lot.AvailableQuantity.ToString(CultureInfo.InvariantCulture);

        return $"{lot.AvailableQuantity.ToString(CultureInfo.InvariantCulture)} / {lot.TotalQuantity.ToString(CultureInfo.InvariantCulture)}";
    }
}

    public sealed class AdminHomeNavigationCardViewModel
    {
        public AdminHomeNavigationCardViewModel(
            string title,
            string subtitle,
            string iconPath,
            string sectionKey)
        {
            Title = title;
            Subtitle = subtitle;
            IconPath = iconPath;
            SectionKey = sectionKey;
        }

        public string Title { get; }

        public string Subtitle { get; }

        public string IconPath { get; }

        public string SectionKey { get; }
    }

    public sealed class AdminHomeKpiCardViewModel
    {
        public AdminHomeKpiCardViewModel(
            string title,
            string value,
            string subtitle,
            string iconPath)
        {
            Title = title;
            Value = value;
            Subtitle = subtitle;
            IconPath = iconPath;
        }

        public string Title { get; }

        public string Value { get; }

        public string Subtitle { get; }

        public string IconPath { get; }
    }

    public sealed class AdminHomeUserItemViewModel
    {
        public string Login { get; init; } = string.Empty;

        public string FullName { get; init; } = string.Empty;

        public string RoleText { get; init; } = string.Empty;

        public string RegistrationDateText { get; init; } = "Дата недоступна";

        public string StatusText { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public bool IsInactive => !IsActive;

        public static AdminHomeUserItemViewModel FromDto(AdminUserDto dto)
        {
            return new AdminHomeUserItemViewModel
            {
                Login = DisplayOrFallback(dto.Login),
                FullName = DisplayOrFallback(dto.FullName),
                RoleText = DisplayOrFallback(dto.RoleText),
                RegistrationDateText = "Дата недоступна",
                StatusText = dto.ActivityStatusText,
                IsActive = dto.IsActive
            };
        }
    }

    public sealed class AdminHomeFoodPointItemViewModel
    {
        public string Name { get; init; } = string.Empty;

        public string OwnerText { get; init; } = string.Empty;

        public string Address { get; init; } = string.Empty;

        public string StatusText { get; init; } = string.Empty;

        public string LotCountText { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public bool IsInactive => !IsActive;

        public static AdminHomeFoodPointItemViewModel FromDto(AdminFoodPointDto dto)
        {
            var ownerName = DisplayOrFallback(dto.OwnerName, "Владелец не указан");
            var ownerText = string.IsNullOrWhiteSpace(dto.OwnerLogin)
                ? ownerName
                : $"{ownerName} ({dto.OwnerLogin.Trim()})";

            return new AdminHomeFoodPointItemViewModel
            {
                Name = DisplayOrFallback(dto.Name),
                OwnerText = ownerText,
                Address = DisplayOrFallback(dto.Address, "Адрес не указан"),
                StatusText = dto.StatusText,
                LotCountText = dto.LotCount.ToString(CultureInfo.InvariantCulture),
                IsActive = dto.IsActive
            };
        }
    }

    public sealed class AdminHomeLotWatchItemViewModel
    {
        public string Title { get; init; } = string.Empty;

        public string StateText { get; init; } = string.Empty;

        public string Reason { get; init; } = string.Empty;

        public string BalanceOrDateText { get; init; } = string.Empty;

        public DateTime PickupDeadline { get; init; }

        public int Priority { get; init; }

        public bool IsDanger { get; init; }

        public bool IsWarning { get; init; }

        public bool IsNeutral { get; init; }

        public bool IsSuccess => !IsDanger && !IsWarning && !IsNeutral;

        public static AdminHomeLotWatchItemViewModel? TryCreate(
            AdminLotDto dto,
            DateTime now)
        {
            var localDeadline = ToLocal(dto.PickupDeadline);
            var today = now.Date;
            var tomorrow = today.AddDays(1);

            if (dto.Status == LotStatus.Cancelled)
            {
                return new AdminHomeLotWatchItemViewModel
                {
                    Title = DisplayOrFallback(dto.Title),
                    StateText = dto.StatusText,
                    Reason = "Лот отменен",
                    BalanceOrDateText = FormatDateTime(dto.PickupDeadline),
                    PickupDeadline = localDeadline,
                    Priority = 30,
                    IsNeutral = true
                };
            }

            if (dto.Status == LotStatus.Expired)
            {
                return new AdminHomeLotWatchItemViewModel
                {
                    Title = DisplayOrFallback(dto.Title),
                    StateText = dto.StatusText,
                    Reason = "Срок получения прошел",
                    BalanceOrDateText = FormatDateTime(dto.PickupDeadline),
                    PickupDeadline = localDeadline,
                    Priority = 20,
                    IsDanger = true
                };
            }

            if (dto.Status == LotStatus.SoldOut || dto.AvailableQuantity <= 0)
            {
                return new AdminHomeLotWatchItemViewModel
                {
                    Title = DisplayOrFallback(dto.Title),
                    StateText = dto.Status == LotStatus.SoldOut ? dto.StatusText : "Нет наличия",
                    Reason = "Доступное количество равно 0",
                    BalanceOrDateText = FormatQuantity(dto),
                    PickupDeadline = localDeadline,
                    Priority = 10,
                    IsDanger = true
                };
            }

            if (dto.Status == LotStatus.Active && localDeadline.Date == today)
            {
                return new AdminHomeLotWatchItemViewModel
                {
                    Title = DisplayOrFallback(dto.Title),
                    StateText = "Истекает сегодня",
                    Reason = $"Истекает сегодня в {localDeadline.ToString("HH:mm", RussianCulture)}",
                    BalanceOrDateText = $"Осталось: {dto.AvailableQuantity.ToString(CultureInfo.InvariantCulture)}",
                    PickupDeadline = localDeadline,
                    Priority = 40,
                    IsWarning = true
                };
            }

            if (dto.Status == LotStatus.Active && localDeadline.Date == tomorrow)
            {
                return new AdminHomeLotWatchItemViewModel
                {
                    Title = DisplayOrFallback(dto.Title),
                    StateText = "Истекает завтра",
                    Reason = $"Истекает завтра в {localDeadline.ToString("HH:mm", RussianCulture)}",
                    BalanceOrDateText = $"Осталось: {dto.AvailableQuantity.ToString(CultureInfo.InvariantCulture)}",
                    PickupDeadline = localDeadline,
                    Priority = 50,
                    IsWarning = true
                };
            }

            return null;
        }
    }

    public sealed class AdminHomeModuleStateViewModel
    {
        public AdminHomeModuleStateViewModel(
            string name,
            string statusText,
            bool isAvailable)
        {
            Name = name;
            StatusText = statusText;
            IsAvailable = isAvailable;
        }

        public string Name { get; }

        public string StatusText { get; }

        public bool IsAvailable { get; }

        public bool IsUnavailable => !IsAvailable;
    }
