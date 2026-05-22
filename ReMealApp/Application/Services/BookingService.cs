using Application.DTOs.Booking;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;

    public BookingService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<bool> BookLotAsync(Guid userId, Guid foodLotId, int quantity)
    {
        var lot = await _bookingRepository.GetLotByIdAsync(foodLotId);

        if (lot == null)
            return false;

        if (lot.Status != LotStatus.Active)
            return false;

        if (lot.AvailableQuantity < quantity)
            return false;

        lot.AvailableQuantity -= quantity;

        if (lot.AvailableQuantity == 0)
        {
            lot.Status = LotStatus.SoldOut;
        }

        await _bookingRepository.UpdateLotAsync(lot);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FoodLotId = foodLotId,
            Quantity = quantity,
            BookingDate = DateTime.Now,
            Status = BookingStatus.Active
        };

        await _bookingRepository.AddAsync(booking);

        return true;
    }

    public async Task<bool> CancelBookingAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);

        if (booking == null)
            return false;

        if (booking.Status != BookingStatus.Active)
            return false;

        var lot = await _bookingRepository.GetLotByIdAsync(booking.FoodLotId);

        if (lot != null)
        {
            lot.AvailableQuantity += booking.Quantity;

            if (lot.Status == LotStatus.SoldOut &&
                lot.AvailableQuantity > 0)
            {
                lot.Status = LotStatus.Active;
            }

            await _bookingRepository.UpdateLotAsync(lot);
        }

        booking.Status = BookingStatus.Cancelled;

        await _bookingRepository.UpdateAsync(booking);

        return true;
    }

    public async Task<bool> ConfirmBookingAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);

        if (booking == null)
            return false;

        booking.Status = BookingStatus.Completed;

        await _bookingRepository.UpdateAsync(booking);

        return true;
    }

    public async Task<bool> RejectBookingAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);

        if (booking == null)
            return false;

        if (booking.Status != BookingStatus.Active)
            return false;

        var lot = await _bookingRepository.GetLotByIdAsync(booking.FoodLotId);

        if (lot != null)
        {
            lot.AvailableQuantity += booking.Quantity;

            if (lot.Status == LotStatus.SoldOut &&
                lot.AvailableQuantity > 0)
            {
                lot.Status = LotStatus.Active;
            }

            await _bookingRepository.UpdateLotAsync(lot);
        }

        booking.Status = BookingStatus.Rejected;

        await _bookingRepository.UpdateAsync(booking);

        return true;
    }

    public async Task<List<BookingDto>> GetUserBookingsAsync(Guid userId)
    {
        var bookings = await _bookingRepository.GetUserBookingsAsync(userId);

        return bookings.Select(x => new BookingDto
        {
            Id = x.Id,
            UserName = x.User.FullName,
            LotTitle = x.FoodLot.Title,
            Quantity = x.Quantity,
            BookingDate = x.BookingDate,
            Status = x.Status
        }).ToList();
    }

    public async Task<List<BookingDto>> GetPartnerBookingsAsync(Guid partnerId)
    {
        var bookings = await _bookingRepository.GetPartnerBookingsAsync(partnerId);

        return bookings.Select(x => new BookingDto
        {
            Id = x.Id,
            UserName = x.User.FullName,
            LotTitle = x.FoodLot.Title,
            Quantity = x.Quantity,
            BookingDate = x.BookingDate,
            Status = x.Status
        }).ToList();
    }
}