using Application.DTOs.Booking;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using ReMealApp.ViewModels;
using System.Collections.ObjectModel;

namespace ReMealApp.ViewModels.Booking;

public partial class MyBookingsViewModel : ViewModelBase
{
    private readonly IBookingService _bookingService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private ObservableCollection<BookingDto> bookings = new();

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public MyBookingsViewModel(
        IBookingService bookingService,
        IAuthService authService)
    {
        _bookingService = bookingService;
        _authService = authService;
    }

    public async Task LoadAsync()
    {
        await LoadBookingsAsync();
    }

    [RelayCommand]
    private async Task LoadBookingsAsync()
    {
        try
        {
            var currentUser = await _authService.GetCurrentUserAsync();

            if (currentUser == null)
                return;

            var result = await _bookingService
                .GetUserBookingsAsync(currentUser.Id);

            Bookings = new ObservableCollection<BookingDto>(result);
        }
        catch (Exception ex)
        {
            StatusMessage = ExceptionMessageFormatter
                .ToUserMessage(ex);
        }
    }

    [RelayCommand]
    private async Task CancelBookingAsync(Guid bookingId)
    {
        try
        {
            await _bookingService.CancelBookingAsync(bookingId);

            await LoadBookingsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = ExceptionMessageFormatter
                .ToUserMessage(ex);
        }
    }
}