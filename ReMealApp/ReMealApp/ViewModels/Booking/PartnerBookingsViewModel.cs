using Application.DTOs.Booking;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using ReMealApp.ViewModels;
using System.Collections.ObjectModel;

namespace ReMealApp.ViewModels.Booking;

public partial class PartnerBookingsViewModel : ViewModelBase
{
    private const decimal WastePreventedKgPerPortion = 0.35m;

    private readonly IBookingService _bookingService;

    private List<BookingDto> _allBookings = new();

    [ObservableProperty]
    private ObservableCollection<BookingDto> _filteredBookings = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private int _issuedBookingsCount;

    [ObservableProperty]
    private int _savedPortions;

    [ObservableProperty]
    private decimal _preventedWasteKg;

    [ObservableProperty]
    private int _cancelledBookingsCount;

    public ObservableCollection<string> FoodPoints { get; set; } = new();

    public ObservableCollection<string> Statuses { get; set; } = new();

    public bool HasBookings => _allBookings.Count > 0;

    public bool HasFilteredBookings => FilteredBookings.Count > 0;

    public bool IsEmptyBookingsState => !IsBusy && !HasBookings;

    public bool IsEmptyFilterState =>
        !IsBusy &&
        HasBookings &&
        !HasFilteredBookings;

    public bool IsFoodPointFilterEnabled =>
        FoodPoints.Any(x => x != "Нет точек");

    public bool IsStatusFilterEnabled =>
        Statuses.Any(x => x != "Нет статусов");

    public string BookingsCountText =>
        $"Бронирований по вашим лотам: {FilteredBookings.Count}";

    public string PreventedWasteDisplay =>
        PreventedWasteKg.ToString("0.##");

    [ObservableProperty]
    private string? _selectedFoodPoint;

    partial void OnSelectedFoodPointChanged(string? value)
    {
        ApplyFilters();
    }

    [ObservableProperty]
    private string? _selectedStatus;

    partial void OnSelectedStatusChanged(string? value)
    {
        ApplyFilters();
    }

    [ObservableProperty]
    private string? _searchText;

    partial void OnSearchTextChanged(string? value)
    {
        ApplyFilters();
    }

    [ObservableProperty]
    private DateTimeOffset? _selectedDate;

    partial void OnSelectedDateChanged(DateTimeOffset? value)
    {
        ApplyFilters();
    }

    public PartnerBookingsViewModel(IBookingService bookingService)
    {
        _bookingService = bookingService;
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

            var result = await _bookingService.GetCurrentPartnerBookingsAsync();

            foreach (var booking in result)
            {
                booking.IsIssued = booking.Status == BookingStatus.Issued;
                booking.IsPending = booking.Status == BookingStatus.Active;
                booking.IsCancelled = booking.Status == BookingStatus.Cancelled;
            }

            _allBookings = result.ToList();

            LoadFilters();

            ApplyFilters();

            StatusMessage = $"Бронирований: {result.Count}";
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

    private void LoadFilters()
    {
        FoodPoints.Clear();
        Statuses.Clear();

        var foodPoints = _allBookings
            .Select(x => x.FoodPointName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        if (foodPoints.Count == 0)
        {
            FoodPoints.Add("Нет точек");
        }
        else
        {
            FoodPoints.Add("Все точки");

            foreach (var point in foodPoints)
            {
                FoodPoints.Add(point);
            }
        }

        var statuses = _allBookings
            .Select(x => x.StatusText)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        if (statuses.Count == 0)
        {
            Statuses.Add("Нет статусов");
        }
        else
        {
            Statuses.Add("Все статусы");

            foreach (var status in statuses)
            {
                Statuses.Add(status);
            }
        }

        SelectedFoodPoint = FoodPoints.FirstOrDefault();
        SelectedStatus = Statuses.FirstOrDefault();

        OnPropertyChanged(nameof(IsFoodPointFilterEnabled));
        OnPropertyChanged(nameof(IsStatusFilterEnabled));
    }

    private void ApplyFilters()
    {
        IEnumerable<BookingDto> query = _allBookings;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(x =>
                (x.UserName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.UserLogin?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.UserEmail?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.LotTitle?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.FoodPointName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(SelectedFoodPoint)
            && SelectedFoodPoint != "Все точки"
            && SelectedFoodPoint != "Нет точек")
        {
            query = query.Where(x => x.FoodPointName == SelectedFoodPoint);
        }

        if (!string.IsNullOrWhiteSpace(SelectedStatus)
            && SelectedStatus != "Все статусы"
            && SelectedStatus != "Нет статусов")
        {
            query = query.Where(x => x.StatusText == SelectedStatus);
        }

        if (SelectedDate.HasValue)
        {
            query = query.Where(x =>
                x.ReservedAt.Date == SelectedDate.Value.Date);
        }

        var filtered = query.ToList();

        FilteredBookings = new ObservableCollection<BookingDto>(filtered);

        RecalculateAnalytics(filtered);
        NotifyBookingStateChanged();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedFoodPoint = "Все точки";
        SelectedStatus = "Все статусы";
        SelectedDate = null;

        ApplyFilters();
    }

    [RelayCommand]
    private async Task ConfirmBookingAsync(Guid bookingId)
    {
        if (IsBusy)
            return;

        var shouldReload = false;

        try
        {
            IsBusy = true;

            await _bookingService.ConfirmBookingAsync(bookingId);
            shouldReload = true;
        }
        catch (Exception ex)
        {
            StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
        }
        finally
        {
            IsBusy = false;
        }

        if (shouldReload)
        {
            await LoadBookingsAsync();
            StatusMessage = "Выдача подтверждена.";
        }
    }

    private void RecalculateAnalytics(IReadOnlyCollection<BookingDto> bookings)
    {
        IssuedBookingsCount = bookings.Count(x => x.Status == BookingStatus.Issued);

        SavedPortions = bookings
            .Where(x => x.Status == BookingStatus.Issued)
            .Sum(x => x.Quantity);

        PreventedWasteKg =
            Math.Round(SavedPortions * WastePreventedKgPerPortion, 2);

        CancelledBookingsCount =
            bookings.Count(x => x.Status == BookingStatus.Cancelled);

        OnPropertyChanged(nameof(PreventedWasteDisplay));
    }

    partial void OnFilteredBookingsChanged(ObservableCollection<BookingDto> value)
    {
        NotifyBookingStateChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        NotifyBookingStateChanged();
    }

    private void NotifyBookingStateChanged()
    {
        OnPropertyChanged(nameof(HasBookings));
        OnPropertyChanged(nameof(HasFilteredBookings));
        OnPropertyChanged(nameof(IsEmptyBookingsState));
        OnPropertyChanged(nameof(IsEmptyFilterState));
        OnPropertyChanged(nameof(BookingsCountText));
    }
}
