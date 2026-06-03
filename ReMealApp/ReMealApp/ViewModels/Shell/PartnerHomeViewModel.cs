using Application.DTOs.Booking;
using Application.Interfaces;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Domain.Enums;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ReMealApp.ViewModels.Shell;

public partial class PartnerHomeViewModel : ViewModelBase
{
    private static readonly CultureInfo RussianCulture =
        CultureInfo.GetCultureInfo("ru-RU");

    private readonly IAuthService _authService;
    private readonly IFoodPointService _foodPointService;
    private readonly ILotService _lotService;
    private readonly IBookingService _bookingService;
    private readonly Action<string> _navigateToSection;
    private readonly Func<Task> _openCreateLot;
    private readonly Func<Task> _openCreateFoodPoint;
    private readonly Func<Guid, Task> _openLotDetails;
    private readonly Func<Guid, Task> _openFoodPointDetails;

    [ObservableProperty]
    private ObservableCollection<PartnerHomeKpiCardViewModel> _kpiCards = new();

    [ObservableProperty]
    private ObservableCollection<PartnerHomeBookingItemViewModel> _pendingIssueBookings = new();

    [ObservableProperty]
    private ObservableCollection<PartnerHomeExpiringLotItemViewModel> _expiringLots = new();

    [ObservableProperty]
    private ObservableCollection<PartnerHomeFoodPointItemViewModel> _foodPoints = new();

    [ObservableProperty]
    private string _displayName = "Партнер";

    [ObservableProperty]
    private string _login = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _avatarPath = string.Empty;

    [ObservableProperty]
    private UserRole _role = UserRole.FoodPointRepresentative;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public PartnerHomeViewModel(
        IAuthService authService,
        IFoodPointService foodPointService,
        ILotService lotService,
        IBookingService bookingService,
        Action<string> navigateToSection,
        Func<Task> openCreateLot,
        Func<Task> openCreateFoodPoint,
        Func<Guid, Task> openLotDetails,
        Func<Guid, Task> openFoodPointDetails)
    {
        _authService = authService;
        _foodPointService = foodPointService;
        _lotService = lotService;
        _bookingService = bookingService;
        _navigateToSection = navigateToSection;
        _openCreateLot = openCreateLot;
        _openCreateFoodPoint = openCreateFoodPoint;
        _openLotDetails = openLotDetails;
        _openFoodPointDetails = openFoodPointDetails;

        ResetKpis();
    }

    public string GreetingText => $"Здравствуйте, {FirstName}!";

    public string FirstName
    {
        get
        {
            var value = DisplayName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            return string.IsNullOrWhiteSpace(value) ? "Партнер" : value;
        }
    }

    public string RoleText => Role switch
    {
        UserRole.FoodPointRepresentative => "Партнер",
        UserRole.StudentCustomer => "Покупатель",
        UserRole.Administrator => "Администратор",
        _ => Role.ToString()
    };

    public string AvatarInitials
    {
        get
        {
            var parts = DisplayName.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
                return "?";

            return string.Concat(parts.Take(2).Select(x => char.ToUpperInvariant(x[0])));
        }
    }

    public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarPath);

    public bool HasNoAvatar => !HasAvatar;

    public bool HasPendingIssueBookings => PendingIssueBookings.Count > 0;

    public bool IsPendingIssueBookingsEmpty => !IsBusy && !HasPendingIssueBookings;

    public bool HasExpiringLots => ExpiringLots.Count > 0;

    public bool IsExpiringLotsEmpty => !IsBusy && !HasExpiringLots;

    public bool HasFoodPoints => FoodPoints.Count > 0;

    public bool IsFoodPointsEmpty => !IsBusy && !HasFoodPoints;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

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
                ResetKpis();
                StatusMessage = "Пользователь не авторизован.";
                return;
            }

            DisplayName = ResolveDisplayName(
                currentUser.FullName,
                currentUser.Login,
                currentUser.Email);
            Login = currentUser.Login;
            Email = currentUser.Email;
            AvatarPath = currentUser.AvatarPath;
            Role = currentUser.Role;

            if (currentUser.Role != UserRole.FoodPointRepresentative)
            {
                ClearDashboardData();
                ResetKpis();
                StatusMessage = "Главная партнера доступна только представителю точки питания.";
                return;
            }

            await _lotService.MarkExpiredLotsAsync();

            var foodPoints = await _foodPointService.GetCurrentPartnerFoodPointsAsync();
            var lots = await _lotService.GetCurrentPartnerLotsAsync();
            var bookings = await _bookingService.GetCurrentPartnerBookingsAsync();

            BuildKpis(lots, bookings);
            BuildPendingIssueBookings(bookings);
            BuildExpiringLots(lots);
            BuildFoodPoints(foodPoints, lots);
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
    private Task CreateLotAsync()
    {
        return _openCreateLot();
    }

    [RelayCommand]
    private Task CreateFoodPointAsync()
    {
        return _openCreateFoodPoint();
    }

    [RelayCommand]
    private void OpenPartnerLots()
    {
        _navigateToSection(HomeViewModel.PartnerLotsSection);
    }

    [RelayCommand]
    private void OpenBookings()
    {
        _navigateToSection(HomeViewModel.PartnerBookingsSection);
    }

    [RelayCommand]
    private void OpenAnalytics()
    {
        _navigateToSection(HomeViewModel.AnalyticsSection);
    }

    [RelayCommand]
    private Task OpenLotAsync(PartnerHomeExpiringLotItemViewModel? lot)
    {
        return lot is null ? Task.CompletedTask : _openLotDetails(lot.Id);
    }

    [RelayCommand]
    private Task ManageFoodPointAsync(PartnerHomeFoodPointItemViewModel? foodPoint)
    {
        return foodPoint is null ? Task.CompletedTask : _openFoodPointDetails(foodPoint.Id);
    }

    [RelayCommand]
    private async Task ConfirmIssueAsync(PartnerHomeBookingItemViewModel? booking)
    {
        if (booking is null || IsBusy || !booking.CanConfirmIssue)
            return;

        var confirmed = false;

        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            await _bookingService.ConfirmBookingAsync(booking.Id);
            confirmed = true;
        }
        catch (Exception ex)
        {
            StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
        }
        finally
        {
            IsBusy = false;
        }

        if (confirmed)
        {
            await LoadAsync();
            StatusMessage = "Выдача подтверждена.";
        }
    }

    partial void OnDisplayNameChanged(string value)
    {
        OnPropertyChanged(nameof(FirstName));
        OnPropertyChanged(nameof(GreetingText));
        OnPropertyChanged(nameof(AvatarInitials));
    }

    partial void OnAvatarPathChanged(string value)
    {
        OnPropertyChanged(nameof(HasAvatar));
        OnPropertyChanged(nameof(HasNoAvatar));
    }

    partial void OnRoleChanged(UserRole value)
    {
        OnPropertyChanged(nameof(RoleText));
    }

    partial void OnPendingIssueBookingsChanged(ObservableCollection<PartnerHomeBookingItemViewModel> value)
    {
        OnPropertyChanged(nameof(HasPendingIssueBookings));
        OnPropertyChanged(nameof(IsPendingIssueBookingsEmpty));
    }

    partial void OnExpiringLotsChanged(ObservableCollection<PartnerHomeExpiringLotItemViewModel> value)
    {
        OnPropertyChanged(nameof(HasExpiringLots));
        OnPropertyChanged(nameof(IsExpiringLotsEmpty));
    }

    partial void OnFoodPointsChanged(ObservableCollection<PartnerHomeFoodPointItemViewModel> value)
    {
        OnPropertyChanged(nameof(HasFoodPoints));
        OnPropertyChanged(nameof(IsFoodPointsEmpty));
    }

    partial void OnIsBusyChanged(bool value)
    {
        NotifyDashboardStateChanged();
    }

    partial void OnStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    private void BuildKpis(
        IReadOnlyCollection<FoodLot> lots,
        IReadOnlyCollection<BookingDto> bookings)
    {
        var activeLots = lots
            .Where(x => x.Status == LotStatus.Active)
            .ToList();

        var today = DateTime.Today;
        var pendingTodayCount = bookings.Count(x =>
            x.Status == BookingStatus.Active &&
            ToLocal(x.PickupDeadline).Date == today);

        var expiringTodayCount = activeLots.Count(x =>
            ToLocal(x.PickupDeadline).Date == today);

        KpiCards = new ObservableCollection<PartnerHomeKpiCardViewModel>
        {
            new(
                "Активных лотов",
                activeLots.Count.ToString(CultureInfo.InvariantCulture),
                "По вашим точкам",
                "/Assets/Icons/Lots/Tinted/box-icon-green.png",
                SolidColorBrush.Parse("#1D3825")),
            new(
                "Доступных наборов",
                activeLots.Sum(x => x.AvailableQuantity).ToString(CultureInfo.InvariantCulture),
                "Остаток активных лотов",
                "/Assets/Icons/Booking/portions-green.png",
                SolidColorBrush.Parse("#1D3825")),
            new(
                "Активных бронирований",
                bookings.Count(x => x.Status == BookingStatus.Active).ToString(CultureInfo.InvariantCulture),
                "Ожидают действий",
                "/Assets/Icons/bookings.png",
                SolidColorBrush.Parse("#1D3825")),
            new(
                "Ожидают выдачи сегодня",
                pendingTodayCount.ToString(CultureInfo.InvariantCulture),
                "По активным броням",
                "/Assets/Icons/time-green.png",
                SolidColorBrush.Parse("#1D3825")),
            new(
                "Истекают сегодня",
                expiringTodayCount.ToString(CultureInfo.InvariantCulture),
                "Активные лоты",
                "/Assets/Icons/Lots/Tinted/time-orange.png",
                SolidColorBrush.Parse("#392713"))
        };
    }

    private void ResetKpis()
    {
        KpiCards = new ObservableCollection<PartnerHomeKpiCardViewModel>
        {
            CreateUnavailableKpi("Активных лотов", "/Assets/Icons/sets.png"),
            CreateUnavailableKpi("Доступных наборов", "/Assets/Icons/Booking/portions-green.png"),
            CreateUnavailableKpi("Активных бронирований", "/Assets/Icons/bookings.png"),
            CreateUnavailableKpi("Ожидают выдачи сегодня", "/Assets/Icons/time-green.png"),
            CreateUnavailableKpi("Истекают сегодня", "/Assets/Icons/Lots/Tinted/time-orange.png")
        };
    }

    private static PartnerHomeKpiCardViewModel CreateUnavailableKpi(
        string title,
        string iconPath)
    {
        return new PartnerHomeKpiCardViewModel(
            title,
            "Нет данных",
            "Статистика недоступна",
            iconPath,
            SolidColorBrush.Parse("#252D2B"));
    }

    private void BuildPendingIssueBookings(IEnumerable<BookingDto> bookings)
    {
        PendingIssueBookings = new ObservableCollection<PartnerHomeBookingItemViewModel>(
            bookings
                .Where(x => x.Status == BookingStatus.Active)
                .OrderBy(x => x.PickupDeadline == default)
                .ThenBy(x => x.PickupDeadline)
                .ThenByDescending(x => x.ReservedAt)
                .Take(5)
                .Select(PartnerHomeBookingItemViewModel.FromDto));
    }

    private void BuildExpiringLots(IEnumerable<FoodLot> lots)
    {
        var now = DateTime.Now;
        var soonLimit = now.AddDays(1);

        ExpiringLots = new ObservableCollection<PartnerHomeExpiringLotItemViewModel>(
            lots
                .Where(x =>
                    x.Status == LotStatus.Active &&
                    x.AvailableQuantity > 0 &&
                    ToLocal(x.PickupDeadline) >= now &&
                    ToLocal(x.PickupDeadline) <= soonLimit)
                .OrderBy(x => x.PickupDeadline)
                .Take(4)
                .Select(PartnerHomeExpiringLotItemViewModel.FromLot));
    }

    private void BuildFoodPoints(
        IEnumerable<FoodPoint> foodPoints,
        IReadOnlyCollection<FoodLot> lots)
    {
        var activeLotCountsByFoodPoint = lots
            .Where(x => x.Status == LotStatus.Active)
            .GroupBy(x => x.FoodPointId)
            .ToDictionary(
                x => x.Key,
                x => x.Count());

        FoodPoints = new ObservableCollection<PartnerHomeFoodPointItemViewModel>(
            foodPoints
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Name)
                .Take(3)
                .Select(x => PartnerHomeFoodPointItemViewModel.FromFoodPoint(
                    x,
                    activeLotCountsByFoodPoint.GetValueOrDefault(x.Id))));
    }

    private void ClearDashboardData()
    {
        PendingIssueBookings.Clear();
        ExpiringLots.Clear();
        FoodPoints.Clear();
    }

    private void NotifyDashboardStateChanged()
    {
        OnPropertyChanged(nameof(HasPendingIssueBookings));
        OnPropertyChanged(nameof(IsPendingIssueBookingsEmpty));
        OnPropertyChanged(nameof(HasExpiringLots));
        OnPropertyChanged(nameof(IsExpiringLotsEmpty));
        OnPropertyChanged(nameof(HasFoodPoints));
        OnPropertyChanged(nameof(IsFoodPointsEmpty));
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    private static string ResolveDisplayName(
        string fullName,
        string login,
        string email)
    {
        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName.Trim();

        if (!string.IsNullOrWhiteSpace(login))
            return login.Trim();

        if (!string.IsNullOrWhiteSpace(email))
            return email.Trim();

        return "Партнер";
    }

    private static DateTime ToLocal(DateTime dateTime)
    {
        if (dateTime == default)
            return default;

        return dateTime.ToLocalTime();
    }

    internal static string DisplayOrFallback(
        string? value,
        string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    internal static string FormatDateTime(DateTime dateTime)
    {
        if (dateTime == default)
            return "Не указано";

        var local = ToLocal(dateTime);
        var today = DateTime.Today;

        if (local.Date == today)
            return $"Сегодня {local:HH:mm}";

        if (local.Date == today.AddDays(1))
            return $"Завтра {local:HH:mm}";

        return local.ToString("dd.MM.yyyy HH:mm", RussianCulture);
    }

    internal static string FormatTime(DateTime dateTime)
    {
        if (dateTime == default)
            return "Не указано";

        return ToLocal(dateTime).ToString("HH:mm", RussianCulture);
    }

    internal static string FormatQuantity(int quantity)
    {
        return quantity > 0
            ? quantity.ToString(CultureInfo.InvariantCulture)
            : "Не указано";
    }
}

public sealed class PartnerHomeKpiCardViewModel
{
    public PartnerHomeKpiCardViewModel(
        string title,
        string value,
        string subtitle,
        string iconPath,
        IBrush iconBackground)
    {
        Title = title;
        Value = value;
        Subtitle = subtitle;
        IconPath = iconPath;
        IconBackground = iconBackground;
    }

    public string Title { get; }

    public string Value { get; }

    public string Subtitle { get; }

    public string IconPath { get; }

    public IBrush IconBackground { get; }
}

public sealed class PartnerHomeBookingItemViewModel
{
    private PartnerHomeBookingItemViewModel()
    {
    }

    public Guid Id { get; init; }

    public string BuyerName { get; init; } = string.Empty;

    public string LotTitle { get; init; } = string.Empty;

    public string FoodPointName { get; init; } = string.Empty;

    public string QuantityText { get; init; } = string.Empty;

    public string ReservedAtText { get; init; } = string.Empty;

    public string PickupDeadlineText { get; init; } = string.Empty;

    public string StatusText { get; init; } = string.Empty;

    public BookingStatus Status { get; init; }

    public bool CanConfirmIssue => Status == BookingStatus.Active;

    public string BuyerInitials
    {
        get
        {
            var parts = BuyerName.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0 || BuyerName == "Покупатель")
                return "?";

            return string.Concat(parts.Take(2).Select(x => char.ToUpperInvariant(x[0])));
        }
    }

    public static PartnerHomeBookingItemViewModel FromDto(BookingDto booking)
    {
        return new PartnerHomeBookingItemViewModel
        {
            Id = booking.Id,
            BuyerName = ResolveBuyerName(booking),
            LotTitle = booking.DisplayLotTitle,
            FoodPointName = booking.DisplayFoodPointName,
            QuantityText = PartnerHomeViewModel.FormatQuantity(booking.Quantity),
            ReservedAtText = PartnerHomeViewModel.FormatDateTime(booking.ReservedAt),
            PickupDeadlineText = PartnerHomeViewModel.FormatDateTime(booking.PickupDeadline),
            StatusText = booking.Status == BookingStatus.Active ? "Ожидает выдачи" : booking.StatusText,
            Status = booking.Status
        };
    }

    private static string ResolveBuyerName(BookingDto booking)
    {
        if (!string.IsNullOrWhiteSpace(booking.UserName))
            return booking.UserName.Trim();

        if (!string.IsNullOrWhiteSpace(booking.UserLogin))
            return booking.UserLogin.Trim();

        if (!string.IsNullOrWhiteSpace(booking.UserEmail))
            return booking.UserEmail.Trim();

        return "Покупатель";
    }
}

public sealed class PartnerHomeExpiringLotItemViewModel
{
    private PartnerHomeExpiringLotItemViewModel()
    {
    }

    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string FoodPointName { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public string RemainingText { get; init; } = string.Empty;

    public string PickupDeadlineText { get; init; } = string.Empty;

    public string StatusText { get; init; } = string.Empty;

    public string ImagePath { get; init; } = string.Empty;

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

    public static PartnerHomeExpiringLotItemViewModel FromLot(FoodLot lot)
    {
        return new PartnerHomeExpiringLotItemViewModel
        {
            Id = lot.Id,
            Title = PartnerHomeViewModel.DisplayOrFallback(lot.Title, "Название не указано"),
            FoodPointName = PartnerHomeViewModel.DisplayOrFallback(lot.FoodPoint?.Name, "Точка не указана"),
            Address = PartnerHomeViewModel.DisplayOrFallback(lot.FoodPoint?.Address, "Адрес не указан"),
            RemainingText = $"Осталось: {lot.AvailableQuantity.ToString(CultureInfo.InvariantCulture)}",
            PickupDeadlineText = $"До {PartnerHomeViewModel.FormatDateTime(lot.PickupDeadline)}",
            StatusText = "Истекает",
            ImagePath = lot.ImagePath ?? string.Empty
        };
    }
}

public sealed class PartnerHomeFoodPointItemViewModel
{
    private PartnerHomeFoodPointItemViewModel()
    {
    }

    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public bool IsInactive => !IsActive;

    public string StatusText { get; init; } = string.Empty;

    public string ActiveLotsCountText { get; init; } = string.Empty;

    public string ImagePath { get; init; } = string.Empty;

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

    public static PartnerHomeFoodPointItemViewModel FromFoodPoint(
        FoodPoint foodPoint,
        int activeLotCount)
    {
        return new PartnerHomeFoodPointItemViewModel
        {
            Id = foodPoint.Id,
            Name = PartnerHomeViewModel.DisplayOrFallback(foodPoint.Name, "Без названия"),
            Address = PartnerHomeViewModel.DisplayOrFallback(foodPoint.Address, "Адрес не указан"),
            IsActive = foodPoint.IsActive,
            StatusText = foodPoint.IsActive ? "Активна" : "Деактивирована",
            ActiveLotsCountText = activeLotCount.ToString(CultureInfo.InvariantCulture)
        };
    }
}
