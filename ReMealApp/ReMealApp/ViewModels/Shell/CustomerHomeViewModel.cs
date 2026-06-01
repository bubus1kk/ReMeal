using Application.DTOs.Booking;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Domain.Enums;
using ReMealApp.ViewModels;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ReMealApp.ViewModels.Shell;

public partial class CustomerHomeViewModel : ViewModelBase
{
    private static readonly CultureInfo RussianCulture =
        CultureInfo.GetCultureInfo("ru-RU");

    private readonly IAuthService _authService;
    private readonly ILotService _lotService;
    private readonly IBookingService _bookingService;
    private readonly Action<string> _navigateToSection;
    private readonly Func<Guid, Task> _openLotDetails;
    private List<CustomerHomeLotItemViewModel> _loadedLots = new();

    [ObservableProperty]
    private ObservableCollection<CustomerHomeLotItemViewModel> _availableLots = new();

    [ObservableProperty]
    private ObservableCollection<CustomerHomeBookingItemViewModel> _activeBookings = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _displayName = "Пользователь";

    [ObservableProperty]
    private string _login = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _avatarPath = string.Empty;

    [ObservableProperty]
    private UserRole _role;

    [ObservableProperty]
    private bool _isExpiringSoonSelected;

    [ObservableProperty]
    private bool _isCheapestFirstSelected;

    public CustomerHomeViewModel(
        IAuthService authService,
        ILotService lotService,
        IBookingService bookingService,
        Action<string> navigateToSection,
        Func<Guid, Task> openLotDetails)
    {
        _authService = authService;
        _lotService = lotService;
        _bookingService = bookingService;
        _navigateToSection = navigateToSection;
        _openLotDetails = openLotDetails;
    }

    public string GreetingText => $"Добро пожаловать, {FirstName}!";

    public string FirstName
    {
        get
        {
            var value = DisplayName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            return string.IsNullOrWhiteSpace(value) ? "Пользователь" : value;
        }
    }

    public string RoleText => Role switch
    {
        UserRole.StudentCustomer => "Покупатель",
        UserRole.FoodPointRepresentative => "Партнер",
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

    public bool HasAvailableLots => AvailableLots.Count > 0;

    public bool IsAvailableLotsEmpty => !IsBusy && !HasAvailableLots;

    public bool HasActiveBookings => ActiveBookings.Count > 0;

    public bool IsActiveBookingsEmpty => !IsBusy && !HasActiveBookings;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public string AvailableLotCountText => _loadedLots.Count.ToString(CultureInfo.InvariantCulture);

    public string ActiveBookingCountText => ActiveBookings.Count.ToString(CultureInfo.InvariantCulture);

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            var user = await _authService.GetCurrentUserAsync();
            if (user is null)
            {
                StatusMessage = "Пользователь не авторизован.";
                return;
            }

            DisplayName = ResolveDisplayName(user.FullName, user.Login, user.Email);
            Login = user.Login;
            Email = user.Email;
            AvatarPath = user.AvatarPath;
            Role = user.Role;

            var lots = await _lotService.GetAvailableLotsAsync();
            _loadedLots = lots
                .Select(CustomerHomeLotItemViewModel.FromLot)
                .ToList();

            var bookings = await _bookingService.GetCurrentUserBookingsAsync();
            ActiveBookings = new ObservableCollection<CustomerHomeBookingItemViewModel>(
                bookings
                    .Where(x => x.Status == BookingStatus.Active)
                    .OrderBy(x => x.PickupDeadline == default)
                    .ThenBy(x => x.PickupDeadline)
                    .ThenByDescending(x => x.ReservedAt)
                    .Select(CustomerHomeBookingItemViewModel.FromDto));

            ApplyFilters();

            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
        }
        finally
        {
            IsBusy = false;
            NotifyCountsChanged();
            NotifyEmptyStatesChanged();
        }
    }

    [RelayCommand]
    private void Search()
    {
        ApplyFilters();
    }

    [RelayCommand]
    private void ShowExpiringSoon()
    {
        IsExpiringSoonSelected = true;
        IsCheapestFirstSelected = false;
        ApplyFilters();
    }

    [RelayCommand]
    private void ShowCheapestFirst()
    {
        IsCheapestFirstSelected = true;
        IsExpiringSoonSelected = false;
        ApplyFilters();
    }

    [RelayCommand]
    private void ShowAvailableOnly()
    {
        IsExpiringSoonSelected = false;
        IsCheapestFirstSelected = false;
        ApplyFilters();
    }

    [RelayCommand]
    private async Task BookLotAsync(CustomerHomeLotItemViewModel? lot)
    {
        if (lot is null || IsBusy)
            return;

        var bookingCreated = false;

        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            await _bookingService.BookLotAsync(lot.Id, 1);
            bookingCreated = true;
        }
        catch (Exception ex)
        {
            StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
        }
        finally
        {
            IsBusy = false;
        }

        if (bookingCreated)
        {
            await LoadAsync();
            StatusMessage = "Бронирование создано.";
        }
    }

    [RelayCommand]
    private void OpenCatalog()
    {
        _navigateToSection(HomeViewModel.CatalogSection);
    }

    [RelayCommand]
    private void OpenMap()
    {
        _navigateToSection(HomeViewModel.FoodPointsSection);
    }

    [RelayCommand]
    private void OpenBookings()
    {
        _navigateToSection(HomeViewModel.BookingsSection);
    }

    [RelayCommand]
    private void OpenProfile()
    {
        _navigateToSection(HomeViewModel.ProfileSection);
    }

    [RelayCommand]
    private Task OpenLotDetailsAsync(CustomerHomeLotItemViewModel? lot)
    {
        return lot is null
            ? Task.CompletedTask
            : _openLotDetails(lot.Id);
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnAvailableLotsChanged(ObservableCollection<CustomerHomeLotItemViewModel> value)
    {
        NotifyEmptyStatesChanged();
    }

    partial void OnActiveBookingsChanged(ObservableCollection<CustomerHomeBookingItemViewModel> value)
    {
        NotifyEmptyStatesChanged();
        NotifyCountsChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        NotifyEmptyStatesChanged();
    }

    partial void OnStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusMessage));
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

    private void ApplyFilters()
    {
        IEnumerable<CustomerHomeLotItemViewModel> result = _loadedLots;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var query = SearchText.Trim();
            result = result.Where(x =>
                x.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                x.FoodPointName.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                x.Address.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        }

        if (IsExpiringSoonSelected)
            result = result.OrderBy(x => x.PickupDeadline);
        else if (IsCheapestFirstSelected)
            result = result.OrderBy(x => x.Price).ThenBy(x => x.PickupDeadline);
        else
            result = result.OrderBy(x => x.PickupDeadline);

        AvailableLots = new ObservableCollection<CustomerHomeLotItemViewModel>(
            result.Take(6));

        NotifyCountsChanged();
        NotifyEmptyStatesChanged();
    }

    private void NotifyEmptyStatesChanged()
    {
        OnPropertyChanged(nameof(HasAvailableLots));
        OnPropertyChanged(nameof(IsAvailableLotsEmpty));
        OnPropertyChanged(nameof(HasActiveBookings));
        OnPropertyChanged(nameof(IsActiveBookingsEmpty));
    }

    private void NotifyCountsChanged()
    {
        OnPropertyChanged(nameof(AvailableLotCountText));
        OnPropertyChanged(nameof(ActiveBookingCountText));
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

        return "Пользователь";
    }

    internal static string FormatPrice(decimal price)
    {
        return price > 0
            ? $"{price.ToString("N0", RussianCulture)} ₽"
            : "Цена не указана";
    }

    internal static string FormatPickupDeadline(DateTime pickupDeadline)
    {
        if (pickupDeadline == default)
            return "Время получения не указано";

        var local = pickupDeadline.ToLocalTime();
        var today = DateTime.Today;

        if (local.Date == today)
            return $"Забрать до {local:HH:mm}";

        return $"Забрать до {local:dd.MM HH:mm}";
    }
}

public sealed class CustomerHomeLotItemViewModel
{
    private CustomerHomeLotItemViewModel()
    {
    }

    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string FoodPointName { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public int AvailableQuantity { get; init; }

    public DateTime PickupDeadline { get; init; }

    public LotStatus Status { get; init; }

    public string ImagePath { get; init; } = string.Empty;

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

    public string PriceText => CustomerHomeViewModel.FormatPrice(Price);

    public string AvailableQuantityText =>
        AvailableQuantity > 0
            ? $"Доступно: {AvailableQuantity}"
            : "Нет доступных наборов";

    public string PickupDeadlineText =>
        CustomerHomeViewModel.FormatPickupDeadline(PickupDeadline);

    public string StatusText => Status switch
    {
        LotStatus.Active => "Доступен",
        LotStatus.SoldOut => "Распродан",
        LotStatus.Expired => "Истек",
        LotStatus.Cancelled => "Отменен",
        _ => Status.ToString()
    };

    public static CustomerHomeLotItemViewModel FromLot(FoodLot lot)
    {
        return new CustomerHomeLotItemViewModel
        {
            Id = lot.Id,
            Title = string.IsNullOrWhiteSpace(lot.Title) ? "Название не указано" : lot.Title,
            FoodPointName = string.IsNullOrWhiteSpace(lot.FoodPoint?.Name)
                ? "Точка не указана"
                : lot.FoodPoint!.Name,
            Address = string.IsNullOrWhiteSpace(lot.FoodPoint?.Address)
                ? "Адрес не указан"
                : lot.FoodPoint!.Address,
            Price = lot.Price,
            AvailableQuantity = lot.AvailableQuantity,
            PickupDeadline = lot.PickupDeadline,
            Status = lot.Status,
            ImagePath = lot.ImagePath ?? string.Empty
        };
    }
}

public sealed class CustomerHomeBookingItemViewModel
{
    private CustomerHomeBookingItemViewModel()
    {
    }

    public Guid Id { get; init; }

    public string LotTitle { get; init; } = string.Empty;

    public string FoodPointName { get; init; } = string.Empty;

    public string FoodPointAddress { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public BookingStatus Status { get; init; }

    public DateTime PickupDeadline { get; init; }

    public string ImagePath { get; init; } = string.Empty;

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

    public string QuantityText =>
        Quantity > 0
            ? $"{Quantity} набор"
            : "Количество не указано";

    public string PickupDeadlineText =>
        CustomerHomeViewModel.FormatPickupDeadline(PickupDeadline);

    public string StatusText => Status switch
    {
        BookingStatus.Active => "Активна",
        BookingStatus.Cancelled => "Отменена",
        BookingStatus.Issued => "Выдана",
        _ => Status.ToString()
    };

    public static CustomerHomeBookingItemViewModel FromDto(BookingDto booking)
    {
        return new CustomerHomeBookingItemViewModel
        {
            Id = booking.Id,
            LotTitle = booking.DisplayLotTitle,
            FoodPointName = booking.DisplayFoodPointName,
            FoodPointAddress = booking.DisplayFoodPointAddress,
            Quantity = booking.Quantity,
            Status = booking.Status,
            PickupDeadline = booking.PickupDeadline,
            ImagePath = booking.FoodLotImagePath
        };
    }
}
