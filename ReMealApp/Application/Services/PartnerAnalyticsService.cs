using Application.DTOs.Analytics;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public sealed class PartnerAnalyticsService : IPartnerAnalyticsService
    {
        private const double WastePerPortionKg = 0.35;

        private readonly IAuthService _authService;
        private readonly IBookingRepository _bookingRepository;

        public PartnerAnalyticsService(
            IAuthService authService,
            IBookingRepository bookingRepository)
        {
            _authService = authService;
            _bookingRepository = bookingRepository;
        }

        public async Task<PartnerAnalyticsDashboardDto> GetDashboardAsync(
            AnalyticsPeriod period,
            Guid? foodPointId = null,
            CancellationToken cancellationToken = default)
        {
            var currentUser =
                await _authService.GetCurrentUserAsync(cancellationToken)
                ?? throw new UnauthorizedAccessException(
                    "Пользователь не авторизован.");

            if (currentUser.Role != UserRole.FoodPointRepresentative)
            {
                throw new UnauthorizedAccessException(
                    "Аналитика доступна только партнеру.");
            }

            var bookings =
                await _bookingRepository.GetPartnerBookingsAsync(
                    currentUser.Id,
                    cancellationToken);

            bookings = ApplyFoodPointFilter(
                bookings,
                foodPointId);

            var issuedBookings = bookings
                .Where(x => x.Status == BookingStatus.Issued)
                .Where(x => IsInPeriod(x.IssuedAt, period))
                .ToList();

            var cancelledBookings = bookings
                .Where(x => x.Status == BookingStatus.Cancelled)
                .Where(x => IsInPeriod(x.CancelledAt ?? x.ReservedAt, period))
                .ToList();

            var activeBookings = bookings
                .Where(x => x.Status == BookingStatus.Active)
                .Where(x => IsInPeriod(x.ReservedAt, period))
                .ToList();

            var periodBookings = bookings
                .Where(x => IsBookingInPeriod(x, period))
                .ToList();

            var savedPortions =
                issuedBookings.Sum(x => x.Quantity);

            return new PartnerAnalyticsDashboardDto
            {
                IssuedBookingsCount = issuedBookings.Count,

                SavedPortionsCount = savedPortions,

                CancelledBookingsCount = cancelledBookings.Count,

                ActiveBookingsCount = activeBookings.Count,

                PreventedWasteKg = Math.Round(
                    savedPortions * WastePerPortionKg,
                    2),

                SavedPortionsByDay =
                    BuildSavedPortionsByDay(issuedBookings),

                BookingStatusDistribution =
                    BuildBookingStatusDistribution(periodBookings),

                TopLotsByIssuedQuantity =
                    BuildTopLotsByIssuedQuantity(issuedBookings),

                IssuedByFoodPoint =
                    BuildIssuedByFoodPoint(issuedBookings),

                PreventedWasteByDay =
                    BuildPreventedWasteByDay(issuedBookings)
            };
        }

        private static List<Booking> ApplyFoodPointFilter(
            List<Booking> bookings,
            Guid? foodPointId)
        {
            var query = bookings.AsEnumerable();

            if (foodPointId.HasValue)
            {
                query = query.Where(x =>
                    x.FoodLot?.FoodPointId == foodPointId.Value);
            }

            return query.ToList();
        }

        private static bool IsBookingInPeriod(
            Booking booking,
            AnalyticsPeriod period)
        {
            var date = booking.Status switch
            {
                BookingStatus.Issued => booking.IssuedAt,
                BookingStatus.Cancelled => booking.CancelledAt ?? booking.ReservedAt,
                BookingStatus.Active => booking.ReservedAt,
                _ => booking.ReservedAt
            };

            return IsInPeriod(date, period);
        }

        private static bool IsInPeriod(
            DateTime? date,
            AnalyticsPeriod period)
        {
            if (period == AnalyticsPeriod.AllTime)
                return true;

            if (!date.HasValue)
                return false;

            var fromDate = GetPeriodStartDate(period);

            return !fromDate.HasValue ||
                date.Value.Date >= fromDate.Value;
        }

        private static DateTime? GetPeriodStartDate(
            AnalyticsPeriod period)
        {
            return period switch
            {
                AnalyticsPeriod.Last7Days =>
                    DateTime.UtcNow.Date.AddDays(-6),

                AnalyticsPeriod.Last30Days =>
                    DateTime.UtcNow.Date.AddDays(-29),

                _ => null
            };
        }

        private static List<DateChartPointDto> BuildSavedPortionsByDay(
            List<Booking> issuedBookings)
        {
            return issuedBookings
                .Where(x => x.IssuedAt.HasValue)
                .GroupBy(x => x.IssuedAt!.Value.Date)
                .OrderBy(x => x.Key)
                .Select(x => new DateChartPointDto
                {
                    Date = x.Key,
                    Value = x.Sum(b => b.Quantity)
                })
                .ToList();
        }

        private static List<StatusChartPointDto> BuildBookingStatusDistribution(
            List<Booking> bookings)
        {
            var counts =
                bookings
                    .GroupBy(x => x.Status)
                    .ToDictionary(x => x.Key, x => x.Count());

            return Enum.GetValues<BookingStatus>()
                .Select(status => new StatusChartPointDto
                {
                    StatusName = GetStatusName(status),
                    Count = counts.GetValueOrDefault(status)
                })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        private static List<LotAnalyticsDto> BuildTopLotsByIssuedQuantity(
            List<Booking> issuedBookings)
        {
            return issuedBookings
                .Where(x => x.FoodLot != null)
                .GroupBy(x => x.FoodLot!)
                .Select(x => new LotAnalyticsDto
                {
                    LotId = x.Key.Id,
                    LotTitle = x.Key.Title,
                    IssuedQuantity = x.Sum(b => b.Quantity)
                })
                .OrderByDescending(x => x.IssuedQuantity)
                .Take(5)
                .ToList();
        }

        private static List<FoodPointAnalyticsDto> BuildIssuedByFoodPoint(
            List<Booking> issuedBookings)
        {
            return issuedBookings
                .Where(x => x.FoodLot?.FoodPoint != null)
                .GroupBy(x => x.FoodLot!.FoodPoint!)
                .Select(x => new FoodPointAnalyticsDto
                {
                    FoodPointId = x.Key.Id,
                    FoodPointName = x.Key.Name,
                    IssuedQuantity = x.Sum(b => b.Quantity)
                })
                .OrderByDescending(x => x.IssuedQuantity)
                .ToList();
        }

        private static List<DateChartPointDto> BuildPreventedWasteByDay(
            List<Booking> issuedBookings)
        {
            return issuedBookings
                .Where(x => x.IssuedAt.HasValue)
                .GroupBy(x => x.IssuedAt!.Value.Date)
                .OrderBy(x => x.Key)
                .Select(x => new DateChartPointDto
                {
                    Date = x.Key,
                    Value = Math.Round(
                        x.Sum(b => b.Quantity) * WastePerPortionKg,
                        2)
                })
                .ToList();
        }

        private static string GetStatusName(
            BookingStatus status)
        {
            return status switch
            {
                BookingStatus.Active => "Активные",
                BookingStatus.Cancelled => "Отмененные",
                BookingStatus.Issued => "Выданные",
                _ => status.ToString()
            };
        }
    }
}
