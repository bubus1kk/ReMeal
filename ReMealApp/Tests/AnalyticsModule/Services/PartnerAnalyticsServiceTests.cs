using Application.DTOs.Analytics;
using Domain.Entities;
using Domain.Enums;
using Tests.TestSupport;

namespace Tests.AnalyticsModule.Services;

[TestClass]
public sealed class PartnerAnalyticsServiceTests
{
    [TestMethod]
    public async Task GetDashboardAsync_WhenUserIsNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();

        await AssertEx.ThrowsAsync<UnauthorizedAccessException>(() =>
            database.PartnerAnalyticsService.GetDashboardAsync(AnalyticsPeriod.AllTime));
    }

    [TestMethod]
    public async Task GetDashboardAsync_WhenCurrentUserIsNotPartner_ThrowsUnauthorizedAccessException()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var customer = await database.AddUserAsync(UserRole.StudentCustomer);
        database.Auth.SetCurrentUser(customer);

        await AssertEx.ThrowsAsync<UnauthorizedAccessException>(() =>
            database.PartnerAnalyticsService.GetDashboardAsync(AnalyticsPeriod.AllTime));
    }

    [TestMethod]
    public async Task GetDashboardAsync_WhenPartnerHasNoBookings_ReturnsEmptyDashboard()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
        database.Auth.SetCurrentUser(partner);

        var dashboard =
            await database.PartnerAnalyticsService.GetDashboardAsync(AnalyticsPeriod.AllTime);

        Assert.AreEqual(0, dashboard.IssuedBookingsCount);
        Assert.AreEqual(0, dashboard.SavedPortionsCount);
        Assert.AreEqual(0, dashboard.CancelledBookingsCount);
        Assert.AreEqual(0, dashboard.ActiveBookingsCount);
        Assert.AreEqual(0d, dashboard.PreventedWasteKg);
        Assert.IsEmpty(dashboard.SavedPortionsByDay);
        Assert.IsEmpty(dashboard.BookingStatusDistribution);
        Assert.IsEmpty(dashboard.TopLotsByIssuedQuantity);
        Assert.IsEmpty(dashboard.IssuedByFoodPoint);
        Assert.IsEmpty(dashboard.PreventedWasteByDay);
    }

    [TestMethod]
    public async Task GetDashboardAsync_AllTime_AggregatesPartnerBookings()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
        var otherPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
        var customer = await database.AddUserAsync(UserRole.StudentCustomer);
        var today = DateTime.UtcNow.Date;

        var firstPoint = await database.AddFoodPointAsync(partner, "North cafe");
        var secondPoint = await database.AddFoodPointAsync(partner, "South cafe");
        var otherPoint = await database.AddFoodPointAsync(otherPartner, "Other cafe");
        var firstLot = await database.AddLotAsync(firstPoint, "Breakfast box", totalQuantity: 20);
        var secondLot = await database.AddLotAsync(secondPoint, "Dinner box", totalQuantity: 20);
        var otherLot = await database.AddLotAsync(otherPoint, "Foreign box", totalQuantity: 20);

        await AddBookingAsync(
            database,
            customer,
            firstLot,
            quantity: 2,
            status: BookingStatus.Issued,
            reservedAt: today.AddDays(-2).AddHours(9),
            issuedAt: today.AddDays(-2).AddHours(10));
        await AddBookingAsync(
            database,
            customer,
            firstLot,
            quantity: 3,
            status: BookingStatus.Issued,
            reservedAt: today.AddDays(-2).AddHours(11),
            issuedAt: today.AddDays(-2).AddHours(12));
        await AddBookingAsync(
            database,
            customer,
            secondLot,
            quantity: 4,
            status: BookingStatus.Issued,
            reservedAt: today.AddDays(-1).AddHours(9),
            issuedAt: today.AddDays(-1).AddHours(10));
        await AddBookingAsync(
            database,
            customer,
            secondLot,
            quantity: 1,
            status: BookingStatus.Cancelled,
            reservedAt: today.AddDays(-1).AddHours(11));
        await AddBookingAsync(
            database,
            customer,
            secondLot,
            quantity: 2,
            status: BookingStatus.Active,
            reservedAt: today.AddHours(9));
        await AddBookingAsync(
            database,
            customer,
            otherLot,
            quantity: 99,
            status: BookingStatus.Issued,
            reservedAt: today.AddHours(10),
            issuedAt: today.AddHours(11));

        database.Auth.SetCurrentUser(partner);

        var dashboard =
            await database.PartnerAnalyticsService.GetDashboardAsync(AnalyticsPeriod.AllTime);

        Assert.AreEqual(3, dashboard.IssuedBookingsCount);
        Assert.AreEqual(9, dashboard.SavedPortionsCount);
        Assert.AreEqual(1, dashboard.CancelledBookingsCount);
        Assert.AreEqual(1, dashboard.ActiveBookingsCount);
        Assert.AreEqual(3.15d, dashboard.PreventedWasteKg, 0.001d);

        Assert.HasCount(2, dashboard.SavedPortionsByDay);
        Assert.AreEqual(today.AddDays(-2), dashboard.SavedPortionsByDay[0].Date);
        Assert.AreEqual(5d, dashboard.SavedPortionsByDay[0].Value);
        Assert.AreEqual(today.AddDays(-1), dashboard.SavedPortionsByDay[1].Date);
        Assert.AreEqual(4d, dashboard.SavedPortionsByDay[1].Value);

        AssertStatusCount(dashboard, "Выданные", 3);
        AssertStatusCount(dashboard, "Отмененные", 1);
        AssertStatusCount(dashboard, "Активные", 1);

        Assert.HasCount(2, dashboard.TopLotsByIssuedQuantity);
        Assert.AreEqual(firstLot.Id, dashboard.TopLotsByIssuedQuantity[0].LotId);
        Assert.AreEqual("Breakfast box", dashboard.TopLotsByIssuedQuantity[0].LotTitle);
        Assert.AreEqual(5, dashboard.TopLotsByIssuedQuantity[0].IssuedQuantity);
        Assert.AreEqual(secondLot.Id, dashboard.TopLotsByIssuedQuantity[1].LotId);
        Assert.AreEqual(4, dashboard.TopLotsByIssuedQuantity[1].IssuedQuantity);

        Assert.HasCount(2, dashboard.IssuedByFoodPoint);
        Assert.AreEqual(firstPoint.Id, dashboard.IssuedByFoodPoint[0].FoodPointId);
        Assert.AreEqual("North cafe", dashboard.IssuedByFoodPoint[0].FoodPointName);
        Assert.AreEqual(5, dashboard.IssuedByFoodPoint[0].IssuedQuantity);
        Assert.AreEqual(secondPoint.Id, dashboard.IssuedByFoodPoint[1].FoodPointId);
        Assert.AreEqual(4, dashboard.IssuedByFoodPoint[1].IssuedQuantity);

        Assert.HasCount(2, dashboard.PreventedWasteByDay);
        Assert.AreEqual(1.75d, dashboard.PreventedWasteByDay[0].Value, 0.001d);
        Assert.AreEqual(1.4d, dashboard.PreventedWasteByDay[1].Value, 0.001d);
    }

    [TestMethod]
    public async Task GetDashboardAsync_WhenFoodPointFilterIsSet_UsesOnlyThatFoodPoint()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
        var customer = await database.AddUserAsync(UserRole.StudentCustomer);
        var today = DateTime.UtcNow.Date;

        var firstPoint = await database.AddFoodPointAsync(partner, "First cafe");
        var secondPoint = await database.AddFoodPointAsync(partner, "Second cafe");
        var firstLot = await database.AddLotAsync(firstPoint, "First lot", totalQuantity: 20);
        var secondLot = await database.AddLotAsync(secondPoint, "Second lot", totalQuantity: 20);

        await AddBookingAsync(
            database,
            customer,
            firstLot,
            quantity: 10,
            status: BookingStatus.Issued,
            reservedAt: today.AddDays(-1),
            issuedAt: today.AddDays(-1).AddHours(1));
        await AddBookingAsync(
            database,
            customer,
            secondLot,
            quantity: 4,
            status: BookingStatus.Issued,
            reservedAt: today,
            issuedAt: today.AddHours(1));
        await AddBookingAsync(
            database,
            customer,
            secondLot,
            quantity: 1,
            status: BookingStatus.Cancelled,
            reservedAt: today.AddHours(2));

        database.Auth.SetCurrentUser(partner);

        var dashboard =
            await database.PartnerAnalyticsService.GetDashboardAsync(
                AnalyticsPeriod.AllTime,
                secondPoint.Id);

        Assert.AreEqual(1, dashboard.IssuedBookingsCount);
        Assert.AreEqual(4, dashboard.SavedPortionsCount);
        Assert.AreEqual(1, dashboard.CancelledBookingsCount);
        Assert.AreEqual(0, dashboard.ActiveBookingsCount);
        Assert.AreEqual(secondLot.Id, dashboard.TopLotsByIssuedQuantity.Single().LotId);
        Assert.AreEqual(secondPoint.Id, dashboard.IssuedByFoodPoint.Single().FoodPointId);
        AssertStatusCount(dashboard, "Выданные", 1);
        AssertStatusCount(dashboard, "Отмененные", 1);
    }

    [TestMethod]
    public async Task GetDashboardAsync_WhenPeriodIsSelected_FiltersByReservationDate()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
        var customer = await database.AddUserAsync(UserRole.StudentCustomer);
        var today = DateTime.UtcNow.Date;
        var point = await database.AddFoodPointAsync(partner);
        var lot = await database.AddLotAsync(point, totalQuantity: 20);

        await AddBookingAsync(
            database,
            customer,
            lot,
            quantity: 1,
            status: BookingStatus.Issued,
            reservedAt: today.AddDays(-1),
            issuedAt: today.AddDays(-1).AddHours(1));
        await AddBookingAsync(
            database,
            customer,
            lot,
            quantity: 2,
            status: BookingStatus.Issued,
            reservedAt: today.AddDays(-20),
            issuedAt: today.AddDays(-20).AddHours(1));
        await AddBookingAsync(
            database,
            customer,
            lot,
            quantity: 3,
            status: BookingStatus.Issued,
            reservedAt: today.AddDays(-40),
            issuedAt: today.AddDays(-40).AddHours(1));

        database.Auth.SetCurrentUser(partner);

        var last7Days =
            await database.PartnerAnalyticsService.GetDashboardAsync(AnalyticsPeriod.Last7Days);
        var last30Days =
            await database.PartnerAnalyticsService.GetDashboardAsync(AnalyticsPeriod.Last30Days);
        var allTime =
            await database.PartnerAnalyticsService.GetDashboardAsync(AnalyticsPeriod.AllTime);

        Assert.AreEqual(1, last7Days.SavedPortionsCount);
        Assert.AreEqual(3, last30Days.SavedPortionsCount);
        Assert.AreEqual(6, allTime.SavedPortionsCount);
    }

    [TestMethod]
    public async Task GetDashboardAsync_TopLotsByIssuedQuantity_ReturnsOnlyFiveLargestLots()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
        var customer = await database.AddUserAsync(UserRole.StudentCustomer);
        var today = DateTime.UtcNow.Date;
        var point = await database.AddFoodPointAsync(partner);

        for (var quantity = 1; quantity <= 6; quantity++)
        {
            var lot =
                await database.AddLotAsync(
                    point,
                    $"Meal {quantity}",
                    totalQuantity: 20);

            await AddBookingAsync(
                database,
                customer,
                lot,
                quantity,
                status: BookingStatus.Issued,
                reservedAt: today.AddHours(quantity),
                issuedAt: today.AddHours(quantity + 1));
        }

        database.Auth.SetCurrentUser(partner);

        var dashboard =
            await database.PartnerAnalyticsService.GetDashboardAsync(AnalyticsPeriod.AllTime);

        CollectionAssert.AreEqual(
            new[] { "Meal 6", "Meal 5", "Meal 4", "Meal 3", "Meal 2" },
            dashboard.TopLotsByIssuedQuantity
                .Select(x => x.LotTitle)
                .ToArray());
    }

    private static async Task AddBookingAsync(
        SqliteTestDatabase database,
        User customer,
        FoodLot lot,
        int quantity,
        BookingStatus status,
        DateTime reservedAt,
        DateTime? issuedAt = null)
    {
        var booking = new Booking(
            customer.Id,
            lot.Id,
            quantity,
            lot.Price,
            reservedAt);

        switch (status)
        {
            case BookingStatus.Active:
                break;

            case BookingStatus.Cancelled:
                booking.Cancel(reservedAt.AddHours(1));
                break;

            case BookingStatus.Issued:
                booking.MarkIssued(issuedAt ?? reservedAt.AddHours(1));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        await database.BookingRepository.AddAsync(booking);
        await database.BookingRepository.SaveChangesAsync();
    }

    private static void AssertStatusCount(
        PartnerAnalyticsDashboardDto dashboard,
        string statusName,
        int expectedCount)
    {
        var status =
            dashboard.BookingStatusDistribution
                .Single(x => x.StatusName == statusName);

        Assert.AreEqual(expectedCount, status.Count);
    }
}
