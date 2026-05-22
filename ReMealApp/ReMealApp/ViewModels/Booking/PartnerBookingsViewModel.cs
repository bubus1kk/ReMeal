using Application.DTOs.Booking;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using ReMealApp.ViewModels;
using System.Collections.ObjectModel;

namespace ReMealApp.ViewModels.Booking;

public partial class PartnerBookingsViewModel : ViewModelBase
{
    private readonly IBookingService _bookingService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private ObservableCollection<BookingDto> bookings = new();

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public PartnerBookingsViewModel(
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
            var currentUser = await _authService
                .GetCurrentUserAsync();

            if (currentUser == null)
                return;

            var result = await _bookingService
                .GetPartnerBookingsAsync(currentUser.Id);

            Bookings = new ObservableCollection<BookingDto>(result);
        }
        catch (Exception ex)
        {
            StatusMessage = ExceptionMessageFormatter
                .ToUserMessage(ex);
        }
    }

    [RelayCommand]
    private async Task ConfirmBookingAsync(Guid bookingId)
    {
        try
        {
            await _bookingService
                .ConfirmBookingAsync(bookingId);

            await LoadBookingsAsync();

            StatusMessage = "Выдача подтверждена.";
        }
        catch (Exception ex)
        {
            StatusMessage = ExceptionMessageFormatter
                .ToUserMessage(ex);
        }
    }

    [RelayCommand]
    private async Task RejectBookingAsync(Guid bookingId)
    {
        try
        {
            await _bookingService
                .RejectBookingAsync(bookingId);

            await LoadBookingsAsync();

            StatusMessage = "Бронирование отклонено.";
        }
        catch (Exception ex)
        {
            StatusMessage = ExceptionMessageFormatter
                .ToUserMessage(ex);
        }
    }
}