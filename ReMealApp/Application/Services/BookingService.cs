using Application.DTOs.Booking;
using Application.DTOs.Users;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class BookingService : IBookingService
{
    private const int MaxActiveBookingsPerUser = 5;

    private readonly IBookingRepository _bookingRepository;
    private readonly IAuthService _authService;

    public BookingService(
        IBookingRepository bookingRepository,
        IAuthService authService)
    {
        _bookingRepository = bookingRepository;
        _authService = authService;
    }

    public async Task<BookingDto> BookLotAsync(
        Guid foodLotId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentCustomerAsync(cancellationToken);

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Количество должно быть положительным.");

        var lot = await _bookingRepository.GetLotByIdAsync(foodLotId, cancellationToken)
            ?? throw new KeyNotFoundException($"Лот '{foodLotId}' не найден.");

        var now = DateTime.UtcNow;
        if (lot.PickupDeadline <= now)
        {
            if (lot.Status != LotStatus.Expired && lot.Status != LotStatus.Cancelled)
            {
                lot.MarkExpired();
                await _bookingRepository.UpdateLotAsync(lot, cancellationToken);
                await _bookingRepository.SaveChangesAsync(cancellationToken);
            }

            throw new InvalidOperationException("Нельзя забронировать просроченный лот.");
        }

        if (lot.Status != LotStatus.Active)
            throw new InvalidOperationException("Можно бронировать только активные лоты.");

        if (lot.AvailableQuantity <= 0)
            throw new InvalidOperationException("Набор уже распродан.");

        if (quantity > lot.AvailableQuantity)
            throw new InvalidOperationException("Нельзя забронировать больше наборов, чем осталось.");

        var activeBookings = await _bookingRepository.CountActiveUserBookingsAsync(user.Id, cancellationToken);
        if (activeBookings >= MaxActiveBookingsPerUser)
            throw new InvalidOperationException($"У пользователя уже есть {MaxActiveBookingsPerUser} активных бронирований.");

        lot.DecreaseQuantity(quantity);

        var booking = new Booking(
            user.Id,
            lot.Id,
            quantity,
            lot.Price,
            now);

        await _bookingRepository.AddAsync(booking, cancellationToken);
        await _bookingRepository.UpdateLotAsync(lot, cancellationToken);
        await _bookingRepository.SaveChangesAsync(cancellationToken);

        booking = await _bookingRepository.GetByIdWithDetailsAsync(booking.Id, cancellationToken) ?? booking;
        return MapToDto(booking);
    }

    public async Task CancelBookingAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentCustomerAsync(cancellationToken);
        var booking = await _bookingRepository.GetByIdWithDetailsAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Бронирование '{bookingId}' не найдено.");

        if (booking.UserId != user.Id)
            throw new UnauthorizedAccessException("Нельзя отменить чужое бронирование.");

        if (booking.Status != BookingStatus.Active)
            throw new InvalidOperationException("Отменить можно только активное бронирование.");

        var lot = booking.FoodLot
            ?? await _bookingRepository.GetLotByIdAsync(booking.FoodLotId, cancellationToken)
            ?? throw new InvalidOperationException("Лот бронирования не найден.");

        booking.Cancel(DateTime.UtcNow);
        lot.ReturnReservedQuantity(booking.Quantity);

        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        await _bookingRepository.UpdateLotAsync(lot, cancellationToken);
        await _bookingRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ConfirmBookingAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var partner = await GetCurrentPartnerAsync(cancellationToken);
        var booking = await _bookingRepository.GetByIdWithDetailsAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Бронирование '{bookingId}' не найдено.");

        if (booking.FoodLot?.FoodPoint?.OwnerId != partner.Id)
            throw new UnauthorizedAccessException("Нельзя подтвердить выдачу по чужому лоту.");

        if (booking.Status != BookingStatus.Active)
            throw new InvalidOperationException("Подтвердить выдачу можно только для активного бронирования.");

        booking.MarkIssued(DateTime.UtcNow);

        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        await _bookingRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<BookingDto>> GetCurrentUserBookingsAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentCustomerAsync(cancellationToken);
        var bookings = await _bookingRepository.GetUserBookingsAsync(user.Id, cancellationToken);

        return bookings.Select(MapToDto).ToList();
    }

    public async Task<List<BookingDto>> GetCurrentPartnerBookingsAsync(CancellationToken cancellationToken = default)
    {
        var partner = await GetCurrentPartnerAsync(cancellationToken);
        var bookings = await _bookingRepository.GetPartnerBookingsAsync(partner.Id, cancellationToken);

        return bookings.Select(MapToDto).ToList();
    }

    private async Task<UserProfileDto> GetCurrentCustomerAsync(CancellationToken cancellationToken)
    {
        var user = await _authService.GetCurrentUserAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("Пользователь не авторизован.");

        if (user.Role != UserRole.StudentCustomer)
            throw new UnauthorizedAccessException("Бронирование доступно только покупателю.");

        return user;
    }

    private async Task<UserProfileDto> GetCurrentPartnerAsync(CancellationToken cancellationToken)
    {
        var user = await _authService.GetCurrentUserAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("Пользователь не авторизован.");

        if (user.Role != UserRole.FoodPointRepresentative)
            throw new UnauthorizedAccessException("Выдачу бронирований может подтверждать только партнер.");

        return user;
    }

    private static BookingDto MapToDto(Booking booking)
    {
        return new BookingDto
        {
            Id = booking.Id,
            UserName = booking.User?.FullName ?? string.Empty,
            UserLogin = booking.User?.Login ?? string.Empty,
            UserEmail = booking.User?.Email ?? string.Empty,
            LotTitle = booking.FoodLot?.Title ?? string.Empty,
            FoodPointName = booking.FoodLot?.FoodPoint?.Name ?? string.Empty,
            FoodPointAddress = booking.FoodLot?.FoodPoint?.Address ?? string.Empty,
            Quantity = booking.Quantity,
            PriceAtReservation = booking.PriceAtReservation,
            ReservedAt = booking.ReservedAt,
            CancelledAt = booking.CancelledAt,
            IssuedAt = booking.IssuedAt,
            Status = booking.Status,
            IsIssued = booking.Status == BookingStatus.Issued,
            IsPending = booking.Status == BookingStatus.Active,
            IsCancelled = booking.Status == BookingStatus.Cancelled
        };
    }
}
