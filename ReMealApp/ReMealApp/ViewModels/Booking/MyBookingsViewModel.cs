using Application.DTOs.Booking;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReMealApp.ViewModels;
using System.Collections.ObjectModel;

namespace ReMealApp.ViewModels.Booking;

public partial class MyBookingsViewModel : ViewModelBase
{
    private readonly IBookingService _bookingService;

    [ObservableProperty]
    private ObservableCollection<BookingDto> _bookings = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public MyBookingsViewModel(IBookingService bookingService)
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
            var result = await _bookingService.GetCurrentUserBookingsAsync();
            Bookings = new ObservableCollection<BookingDto>(result);
            StatusMessage = result.Count == 0
                ? "У вас пока нет бронирований."
                : $"Бронирований: {result.Count}";
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
            StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
        }
        finally
        {
            IsBusy = false;
        }

        if (shouldReload)
        {
            await LoadBookingsAsync();
            StatusMessage = "Бронирование отменено.";
        }
    }
}
