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
    private const decimal WastePreventedKgPerPortion = 0.4m;

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

            await LoadBookingsCoreAsync();
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

    private async Task LoadBookingsCoreAsync()
    {
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

        RecalculateAnalytics(result);

        StatusMessage = $"Бронирований: {result.Count}";
    }

    private void LoadFilters()
    {
        FoodPoints.Clear();
        Statuses.Clear();

        FoodPoints.Add("Все точки");

        foreach (var point in _allBookings
                     .Select(x => x.FoodPointName)
                     .Distinct()
                     .OrderBy(x => x))
        {
            FoodPoints.Add(point);
        }

        Statuses.Add("Все статусы");

        foreach (var status in _allBookings
                     .Select(x => x.StatusText)
                     .Distinct()
                     .OrderBy(x => x))
        {
            Statuses.Add(status);
        }

        SelectedFoodPoint = "Все точки";
        SelectedStatus = "Все статусы";
    }

    private void ApplyFilters()
    {
        IEnumerable<BookingDto> query = _allBookings;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(x =>
                (x.UserName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.LotTitle?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.FoodPointName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(SelectedFoodPoint)
            && SelectedFoodPoint != "Все точки")
        {
            query = query.Where(x => x.FoodPointName == SelectedFoodPoint);
        }

        if (!string.IsNullOrWhiteSpace(SelectedStatus)
            && SelectedStatus != "Все статусы")
        {
            query = query.Where(x => x.StatusText == SelectedStatus);
        }

        if (SelectedDate.HasValue)
        {
            query = query.Where(x =>
                x.ReservedAt.Date == SelectedDate.Value.Date);
        }

        FilteredBookings = new ObservableCollection<BookingDto>(query);
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

        try
        {
            IsBusy = true;

            await _bookingService.ConfirmBookingAsync(bookingId);

            await LoadBookingsCoreAsync();

            StatusMessage = "Выдача подтверждена.";
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

    private void RecalculateAnalytics(IReadOnlyCollection<BookingDto> bookings)
    {
        IssuedBookingsCount = bookings.Count(x => x.Status == BookingStatus.Issued);

        SavedPortions = bookings
            .Where(x => x.Status == BookingStatus.Issued)
            .Sum(x => x.Quantity);

        PreventedWasteKg =
            Math.Round(SavedPortions * WastePreventedKgPerPortion, 1);

        CancelledBookingsCount =
            bookings.Count(x => x.Status == BookingStatus.Cancelled);
    }
}
