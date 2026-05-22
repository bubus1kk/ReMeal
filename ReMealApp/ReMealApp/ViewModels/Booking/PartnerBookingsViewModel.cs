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

    [ObservableProperty]
    private ObservableCollection<BookingDto> _bookings = new();

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
            Bookings = new ObservableCollection<BookingDto>(result);
            RecalculateAnalytics(result);
            StatusMessage = result.Count == 0
                ? "По вашим лотам пока нет бронирований."
                : $"Бронирований по вашим лотам: {result.Count}";
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
        PreventedWasteKg = Math.Round(SavedPortions * WastePreventedKgPerPortion, 1);
        CancelledBookingsCount = bookings.Count(x => x.Status == BookingStatus.Cancelled);
    }
}
