using Application.DTOs.Booking;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using ReMealApp.ViewModels;
using System.Collections.ObjectModel;

namespace ReMealApp.ViewModels.Booking;

public partial class MyBookingsViewModel : ViewModelBase
{
    private readonly IBookingService _bookingService;
    private readonly Func<Guid, Task>? _openLotDetails;
    private readonly Func<Task>? _openCatalog;
    private List<BookingDto> _allBookings = new();

    [ObservableProperty]
    private ObservableCollection<BookingDto> _filteredBookings = new();

    [ObservableProperty]
    private ObservableCollection<BookingStatusFilterOption> _statusFilters = new();

    [ObservableProperty]
    private BookingStatusFilterOption? _selectedStatusFilter;

    [ObservableProperty]
    private ObservableCollection<BookingPeriodFilterOption> _periodFilters = new();

    [ObservableProperty]
    private BookingPeriodFilterOption? _selectedPeriodFilter;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _feedbackMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private int _activeBookingsCount;

    [ObservableProperty]
    private int _issuedBookingsCount;

    [ObservableProperty]
    private int _cancelledBookingsCount;

    public bool HasBookings => _allBookings.Count > 0;

    public ObservableCollection<BookingDto> Bookings => FilteredBookings;

    public bool HasFilteredBookings => FilteredBookings.Count > 0;

    public bool IsEmptyState => IsEmptyBookingsState;

    public bool IsEmptyBookingsState => !IsBusy && !HasBookings;

    public bool IsEmptyFilterState =>
        !IsBusy &&
        HasBookings &&
        !HasFilteredBookings;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool HasFeedbackMessage => !string.IsNullOrWhiteSpace(FeedbackMessage);

    public bool CanResetFilters => HasActiveFilters;

    public bool CanOpenDetails => _openLotDetails is not null;

    public bool CanOpenCatalog => _openCatalog is not null;

    public string BookingsCountText =>
        $"Всего бронирований: {_allBookings.Count}";

    public string EmptyTitle =>
        IsEmptyFilterState
            ? "Ничего не найдено"
            : "У вас пока нет бронирований";

    public string EmptySubtitle =>
        IsEmptyFilterState
            ? "Измените поисковый запрос или сбросьте фильтры."
            : "Перейдите в каталог, чтобы забронировать первый набор.";

    public string EmptyActionText =>
        IsEmptyFilterState
            ? "Сбросить фильтры"
            : "Перейти в каталог";

    public IRelayCommand EmptyActionCommand =>
        IsEmptyFilterState
            ? ResetFiltersCommand
            : ShowCatalogCommand;

    private bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchText) ||
        SelectedStatusFilter?.Status is not null ||
        SelectedPeriodFilter?.Kind is not null and not BookingPeriodKind.All;

    public MyBookingsViewModel(IBookingService bookingService)
        : this(bookingService, null, null)
    {
    }

    public MyBookingsViewModel(
        IBookingService bookingService,
        Func<Guid, Task>? openLotDetails,
        Func<Task>? openCatalog)
    {
        _bookingService = bookingService;
        _openLotDetails = openLotDetails;
        _openCatalog = openCatalog;

        StatusFilters = new ObservableCollection<BookingStatusFilterOption>(
            BookingStatusFilterOption.CreateDefault());
        SelectedStatusFilter = StatusFilters.FirstOrDefault();

        PeriodFilters = new ObservableCollection<BookingPeriodFilterOption>(
            BookingPeriodFilterOption.CreateDefault());
        SelectedPeriodFilter = PeriodFilters.FirstOrDefault();

        UpdateSelectedStatusFilter();
    }

    public async Task LoadAsync()
    {
        await LoadBookingsAsync();
    }

    [RelayCommand]
    private async Task LoadBookingsAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            var result = await _bookingService.GetCurrentUserBookingsAsync();
            _allBookings = result.ToList();
            RecalculateKpis();
            ApplyFilters();
            StatusMessage = result.Count == 0
                ? "У вас пока нет бронирований."
                : $"Бронирований: {result.Count}";
            FeedbackMessage = string.Empty;
        }
        catch (Exception ex)
        {
            SetMessage(ExceptionMessageFormatter.ToUserMessage(ex), showFeedback: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnFilteredBookingsChanged(ObservableCollection<BookingDto> value)
    {
        NotifyBookingStateChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        NotifyBookingStateChanged();
    }

    partial void OnStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    partial void OnFeedbackMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasFeedbackMessage));
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedPeriodFilterChanged(BookingPeriodFilterOption? value)
    {
        ApplyFilters();
    }

    partial void OnSelectedStatusFilterChanged(BookingStatusFilterOption? value)
    {
        UpdateSelectedStatusFilter();
        ApplyFilters();
    }

    private void NotifyBookingStateChanged()
    {
        OnPropertyChanged(nameof(HasBookings));
        OnPropertyChanged(nameof(Bookings));
        OnPropertyChanged(nameof(HasFilteredBookings));
        OnPropertyChanged(nameof(IsEmptyBookingsState));
        OnPropertyChanged(nameof(IsEmptyFilterState));
        OnPropertyChanged(nameof(IsEmptyState));
        OnPropertyChanged(nameof(CanResetFilters));
        OnPropertyChanged(nameof(BookingsCountText));
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptySubtitle));
        OnPropertyChanged(nameof(EmptyActionText));
        OnPropertyChanged(nameof(EmptyActionCommand));
    }

    [RelayCommand]
    private async Task CancelBookingAsync(Guid bookingId)
    {
        if (IsBusy)
            return;

        var shouldReload = false;

        try
        {
            IsBusy = true;
            await _bookingService.CancelBookingAsync(bookingId);
            shouldReload = true;
        }
        catch (Exception ex)
        {
            SetMessage(ExceptionMessageFormatter.ToUserMessage(ex), showFeedback: true);
        }
        finally
        {
            IsBusy = false;
        }

        if (shouldReload)
        {
            await LoadBookingsAsync();
            SetMessage("Бронирование отменено.", showFeedback: true);
        }
    }

    [RelayCommand]
    private async Task OpenDetailsAsync(BookingDto? booking)
    {
        if (booking is null || IsBusy || _openLotDetails is null)
            return;

        if (booking.FoodLotId == Guid.Empty)
        {
            SetMessage("Лот бронирования не найден.", showFeedback: true);
            return;
        }

        try
        {
            await _openLotDetails(booking.FoodLotId);
        }
        catch (Exception ex)
        {
            SetMessage(ExceptionMessageFormatter.ToUserMessage(ex), showFeedback: true);
        }
    }

    [RelayCommand]
    private async Task ShowCatalogAsync()
    {
        if (_openCatalog is null)
            return;

        try
        {
            await _openCatalog();
        }
        catch (Exception ex)
        {
            SetMessage(ExceptionMessageFormatter.ToUserMessage(ex), showFeedback: true);
        }
    }

    [RelayCommand]
    private void SelectStatusFilter(BookingStatusFilterOption? option)
    {
        if (option is null)
            return;

        SelectedStatusFilter = option;
    }

    [RelayCommand]
    private void ResetFilters()
    {
        SearchText = string.Empty;
        SelectedStatusFilter = StatusFilters.FirstOrDefault();
        SelectedPeriodFilter = PeriodFilters.FirstOrDefault();
        ApplyFilters();
    }

    private void SetMessage(string message, bool showFeedback)
    {
        StatusMessage = message;
        FeedbackMessage = showFeedback ? message : string.Empty;
    }

    private void ApplyFilters()
    {
        IEnumerable<BookingDto> query = _allBookings;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            query = query.Where(x =>
                Contains(x.LotTitle, search) ||
                Contains(x.FoodPointName, search) ||
                Contains(x.FoodPointAddress, search));
        }

        if (SelectedStatusFilter?.Status is BookingStatus selectedStatus)
            query = query.Where(x => x.Status == selectedStatus);

        query = ApplyPeriodFilter(query);

        FilteredBookings = new ObservableCollection<BookingDto>(
            query.OrderByDescending(x => x.ReservedAt));

        NotifyBookingStateChanged();
    }

    private IEnumerable<BookingDto> ApplyPeriodFilter(IEnumerable<BookingDto> query)
    {
        return SelectedPeriodFilter?.Kind switch
        {
            BookingPeriodKind.Today => query.Where(x =>
                x.ReservedAt != default &&
                x.ReservedAt.ToLocalTime().Date == DateTime.Today),

            BookingPeriodKind.SevenDays => query.Where(x =>
                x.ReservedAt != default &&
                x.ReservedAt.ToLocalTime() >= DateTime.Now.AddDays(-7)),

            BookingPeriodKind.ThirtyDays => query.Where(x =>
                x.ReservedAt != default &&
                x.ReservedAt.ToLocalTime() >= DateTime.Now.AddDays(-30)),

            _ => query
        };
    }

    private void RecalculateKpis()
    {
        ActiveBookingsCount = _allBookings.Count(x => x.Status == BookingStatus.Active);
        IssuedBookingsCount = _allBookings.Count(x => x.Status == BookingStatus.Issued);
        CancelledBookingsCount = _allBookings.Count(x => x.Status == BookingStatus.Cancelled);
    }

    private void UpdateSelectedStatusFilter()
    {
        foreach (var filter in StatusFilters)
            filter.IsSelected = filter == SelectedStatusFilter;
    }

    private static bool Contains(string? source, string query)
    {
        return source?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true;
    }
}

public sealed partial class BookingStatusFilterOption : ObservableObject
{
    public BookingStatusFilterOption(BookingStatus? status, string name)
    {
        Status = status;
        Name = name;
    }

    public BookingStatus? Status { get; }

    public string Name { get; }

    [ObservableProperty]
    private bool _isSelected;

    public static IReadOnlyList<BookingStatusFilterOption> CreateDefault()
    {
        return new[]
        {
            new BookingStatusFilterOption(null, "Все"),
            new BookingStatusFilterOption(BookingStatus.Active, "Активные"),
            new BookingStatusFilterOption(BookingStatus.Issued, "Выданы"),
            new BookingStatusFilterOption(BookingStatus.Cancelled, "Отменены")
        };
    }
}

public sealed class BookingPeriodFilterOption
{
    public BookingPeriodFilterOption(BookingPeriodKind kind, string name)
    {
        Kind = kind;
        Name = name;
    }

    public BookingPeriodKind Kind { get; }

    public string Name { get; }

    public static IReadOnlyList<BookingPeriodFilterOption> CreateDefault()
    {
        return new[]
        {
            new BookingPeriodFilterOption(BookingPeriodKind.All, "За всё время"),
            new BookingPeriodFilterOption(BookingPeriodKind.Today, "Сегодня"),
            new BookingPeriodFilterOption(BookingPeriodKind.SevenDays, "7 дней"),
            new BookingPeriodFilterOption(BookingPeriodKind.ThirtyDays, "30 дней")
        };
    }
}

public enum BookingPeriodKind
{
    All,
    Today,
    SevenDays,
    ThirtyDays
}
