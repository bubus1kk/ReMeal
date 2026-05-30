using Application.DTOs.Analytics;
using Application.DTOs.FoodPoints;
using Application.Interfaces;
using Domain.Entities;
using ReMealApp.ViewModels.Analytics;

namespace Tests.AnalyticsModule.ViewModels;

[TestClass]
public sealed class PartnerAnalyticsViewModelTests
{
    [TestMethod]
    public void Constructor_InitializesAvailablePeriodsWithoutLoadingData()
    {
        var (viewModel, analyticsService, _) = CreateViewModel();

        CollectionAssert.AreEqual(
            new[] { "7 дней", "30 дней", "Все время" },
            viewModel.Periods.Select(x => x.Title).ToArray());
        Assert.AreEqual(AnalyticsPeriod.Last7Days, viewModel.SelectedPeriod?.Value);
        Assert.IsEmpty(analyticsService.Calls);
    }

    [TestMethod]
    public async Task LoadAsync_LoadsFiltersDashboardMetricsAndCharts()
    {
        var (viewModel, analyticsService, foodPointService) = CreateViewModel();
        var northPoint = CreateFoodPoint("North cafe");
        var southPoint = CreateFoodPoint("South cafe");
        foodPointService.FoodPoints.Add(northPoint);
        foodPointService.FoodPoints.Add(southPoint);
        analyticsService.Dashboards.Enqueue(CreateFilledDashboard());

        await viewModel.LoadAsync();

        Assert.IsFalse(viewModel.IsLoading);
        Assert.IsNull(viewModel.ErrorMessage);
        Assert.IsNull(viewModel.EmptyStateMessage);
        Assert.HasCount(3, viewModel.FoodPoints);
        Assert.AreEqual("Все точки", viewModel.SelectedFoodPoint?.Name);
        Assert.IsNull(analyticsService.Calls.Single().FoodPointId);
        Assert.AreEqual(AnalyticsPeriod.Last7Days, analyticsService.Calls.Single().Period);

        Assert.AreEqual(2, viewModel.IssuedBookingsCount);
        Assert.AreEqual(10, viewModel.SavedPortionsCount);
        Assert.AreEqual(1, viewModel.CancelledBookingsCount);
        Assert.AreEqual(1, viewModel.ActiveBookingsCount);
        Assert.AreEqual(3.5d, viewModel.PreventedWasteKg);
        Assert.AreEqual(50d, viewModel.CompletionRate);
        Assert.AreEqual(25d, viewModel.CancellationRate);
        Assert.AreEqual("Dinner box", viewModel.MostPopularLot);
        Assert.AreEqual("North cafe", viewModel.BestFoodPoint);

        Assert.HasCount(2, viewModel.SavedPortionsChartItems);
        Assert.AreEqual("20.05", viewModel.SavedPortionsChartItems[0].Label);
        Assert.AreEqual(2d, viewModel.SavedPortionsChartItems[0].Value);
        Assert.AreEqual(55d, viewModel.SavedPortionsChartItems[0].Height, 0.001d);
        Assert.AreEqual(220d, viewModel.SavedPortionsChartItems[1].Height);

        Assert.HasCount(3, viewModel.BookingStatusesChartItems);
        Assert.AreEqual("Выданные", viewModel.BookingStatusesChartItems[0].Label);
        Assert.AreEqual(220d, viewModel.BookingStatusesChartItems[0].Height);

        Assert.HasCount(2, viewModel.TopLotsChartItems);
        Assert.AreEqual("Dinner box", viewModel.TopLotsChartItems[0].Label);
        Assert.AreEqual(320d, viewModel.TopLotsChartItems[0].Width);
        Assert.AreEqual(80d, viewModel.TopLotsChartItems[1].Width);

        Assert.HasCount(2, viewModel.FoodPointsChartItems);
        Assert.AreEqual("North cafe", viewModel.FoodPointsChartItems[0].Label);
        Assert.AreEqual(320d, viewModel.FoodPointsChartItems[0].Width);
        Assert.AreEqual(192d, viewModel.FoodPointsChartItems[1].Width);
    }

    [TestMethod]
    public async Task LoadAsync_WhenDashboardIsEmpty_ShowsEmptyStateAndClearsPreviousCharts()
    {
        var (viewModel, analyticsService, _) = CreateViewModel();
        analyticsService.Dashboards.Enqueue(CreateFilledDashboard());
        analyticsService.Dashboards.Enqueue(new PartnerAnalyticsDashboardDto());

        await viewModel.LoadAsync();
        await viewModel.RefreshAsync();

        Assert.AreEqual("Недостаточно данных для отображения аналитики.", viewModel.EmptyStateMessage);
        Assert.AreEqual(0, viewModel.IssuedBookingsCount);
        Assert.AreEqual(0, viewModel.SavedPortionsCount);
        Assert.AreEqual(0d, viewModel.CompletionRate);
        Assert.AreEqual(0d, viewModel.CancellationRate);
        Assert.AreEqual("-", viewModel.MostPopularLot);
        Assert.AreEqual("-", viewModel.BestFoodPoint);
        Assert.IsEmpty(viewModel.SavedPortionsChartItems);
        Assert.IsEmpty(viewModel.BookingStatusesChartItems);
        Assert.IsEmpty(viewModel.TopLotsChartItems);
        Assert.IsEmpty(viewModel.FoodPointsChartItems);
    }

    [TestMethod]
    public async Task LoadAsync_WhenDashboardContainsZeroChartValues_UsesMinimumBarSizes()
    {
        var (viewModel, analyticsService, _) = CreateViewModel();
        analyticsService.Dashboards.Enqueue(new PartnerAnalyticsDashboardDto
        {
            SavedPortionsByDay =
            {
                new DateChartPointDto
                {
                    Date = new DateTime(2026, 5, 20),
                    Value = 0
                }
            },
            BookingStatusDistribution =
            {
                new StatusChartPointDto
                {
                    StatusName = "Выданные",
                    Count = 0
                }
            },
            TopLotsByIssuedQuantity =
            {
                new LotAnalyticsDto
                {
                    LotTitle = "Zero lot",
                    IssuedQuantity = 0
                }
            },
            IssuedByFoodPoint =
            {
                new FoodPointAnalyticsDto
                {
                    FoodPointName = "Zero cafe",
                    IssuedQuantity = 0
                }
            }
        });

        await viewModel.LoadAsync();

        Assert.AreEqual(12d, viewModel.SavedPortionsChartItems.Single().Height);
        Assert.AreEqual(12d, viewModel.BookingStatusesChartItems.Single().Height);
        Assert.AreEqual(20d, viewModel.TopLotsChartItems.Single().Width);
        Assert.AreEqual(20d, viewModel.FoodPointsChartItems.Single().Width);
    }

    [TestMethod]
    public async Task LoadAsync_WhenSelectedPeriodIsMissing_LoadsFoodPointsButSkipsDashboard()
    {
        var (viewModel, analyticsService, foodPointService) = CreateViewModel();
        foodPointService.FoodPoints.Add(CreateFoodPoint("North cafe"));
        viewModel.SelectedPeriod = null;

        await viewModel.LoadAsync();

        Assert.HasCount(2, viewModel.FoodPoints);
        Assert.IsEmpty(analyticsService.Calls);
        Assert.IsFalse(viewModel.IsLoading);
    }

    [TestMethod]
    public async Task LoadAsync_WhenServiceThrows_ShowsErrorAndStopsLoading()
    {
        var (viewModel, analyticsService, _) = CreateViewModel();
        analyticsService.Exception = new InvalidOperationException("service down");

        await viewModel.LoadAsync();

        Assert.AreEqual("Ошибка загрузки аналитики: service down", viewModel.ErrorMessage);
        Assert.IsFalse(viewModel.IsLoading);
    }

    [TestMethod]
    public async Task LoadAsync_WhenAlreadyLoading_IgnoresSecondRequest()
    {
        var (viewModel, analyticsService, _) = CreateViewModel();
        analyticsService.WaitBeforeReturning =
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var firstLoad = viewModel.LoadAsync();
        await WaitForCallsAsync(analyticsService, expectedCount: 1);

        await viewModel.LoadAsync();
        Assert.HasCount(1, analyticsService.Calls);

        analyticsService.WaitBeforeReturning.SetResult();
        await firstLoad;
    }

    [TestMethod]
    public async Task RefreshAsync_ReloadsDashboardWithoutReloadingFoodPointFilters()
    {
        var (viewModel, analyticsService, foodPointService) = CreateViewModel();
        foodPointService.FoodPoints.Add(CreateFoodPoint("North cafe"));
        analyticsService.Dashboards.Enqueue(new PartnerAnalyticsDashboardDto());
        analyticsService.Dashboards.Enqueue(new PartnerAnalyticsDashboardDto());

        await viewModel.LoadAsync();
        await viewModel.RefreshAsync();

        Assert.AreEqual(1, foodPointService.LoadCount);
        Assert.HasCount(2, analyticsService.Calls);
    }

    [TestMethod]
    public async Task SelectedFoodPointChanged_AfterInitialLoad_ReloadsDashboardWithFilter()
    {
        var (viewModel, analyticsService, foodPointService) = CreateViewModel();
        var northPoint = CreateFoodPoint("North cafe");
        var southPoint = CreateFoodPoint("South cafe");
        foodPointService.FoodPoints.Add(northPoint);
        foodPointService.FoodPoints.Add(southPoint);
        analyticsService.Dashboards.Enqueue(new PartnerAnalyticsDashboardDto());
        analyticsService.Dashboards.Enqueue(new PartnerAnalyticsDashboardDto());

        await viewModel.LoadAsync();
        viewModel.SelectedFoodPoint =
            viewModel.FoodPoints.Single(x => x.FoodPointId == southPoint.Id);

        await WaitForCallsAsync(analyticsService, expectedCount: 2);

        Assert.AreEqual(southPoint.Id, analyticsService.Calls[1].FoodPointId);
        Assert.AreEqual(AnalyticsPeriod.Last7Days, analyticsService.Calls[1].Period);
    }

    [TestMethod]
    public async Task SelectedPeriodChanged_AfterInitialLoad_ReloadsDashboardWithPeriod()
    {
        var (viewModel, analyticsService, _) = CreateViewModel();
        analyticsService.Dashboards.Enqueue(new PartnerAnalyticsDashboardDto());
        analyticsService.Dashboards.Enqueue(new PartnerAnalyticsDashboardDto());

        await viewModel.LoadAsync();
        viewModel.SelectedPeriod =
            viewModel.Periods.Single(x => x.Value == AnalyticsPeriod.Last30Days);

        await WaitForCallsAsync(analyticsService, expectedCount: 2);

        Assert.AreEqual(AnalyticsPeriod.Last30Days, analyticsService.Calls[1].Period);
        Assert.IsNull(analyticsService.Calls[1].FoodPointId);
    }

    private static (
        PartnerAnalyticsViewModel ViewModel,
        FakePartnerAnalyticsService AnalyticsService,
        FakeFoodPointService FoodPointService) CreateViewModel()
    {
        var analyticsService = new FakePartnerAnalyticsService();
        var foodPointService = new FakeFoodPointService();

        return (
            new PartnerAnalyticsViewModel(analyticsService, foodPointService),
            analyticsService,
            foodPointService);
    }

    private static PartnerAnalyticsDashboardDto CreateFilledDashboard()
    {
        return new PartnerAnalyticsDashboardDto
        {
            IssuedBookingsCount = 2,
            SavedPortionsCount = 10,
            CancelledBookingsCount = 1,
            ActiveBookingsCount = 1,
            PreventedWasteKg = 3.5,
            SavedPortionsByDay =
            {
                new DateChartPointDto
                {
                    Date = new DateTime(2026, 5, 20),
                    Value = 2
                },
                new DateChartPointDto
                {
                    Date = new DateTime(2026, 5, 21),
                    Value = 8
                }
            },
            BookingStatusDistribution =
            {
                new StatusChartPointDto
                {
                    StatusName = "Выданные",
                    Count = 2
                },
                new StatusChartPointDto
                {
                    StatusName = "Отмененные",
                    Count = 1
                },
                new StatusChartPointDto
                {
                    StatusName = "Активные",
                    Count = 1
                }
            },
            TopLotsByIssuedQuantity =
            {
                new LotAnalyticsDto
                {
                    LotTitle = "Dinner box",
                    IssuedQuantity = 8
                },
                new LotAnalyticsDto
                {
                    LotTitle = "Lunch box",
                    IssuedQuantity = 2
                }
            },
            IssuedByFoodPoint =
            {
                new FoodPointAnalyticsDto
                {
                    FoodPointName = "North cafe",
                    IssuedQuantity = 5
                },
                new FoodPointAnalyticsDto
                {
                    FoodPointName = "South cafe",
                    IssuedQuantity = 3
                }
            }
        };
    }

    private static FoodPoint CreateFoodPoint(string name)
    {
        return new FoodPoint(
            name,
            "Campus street, 1",
            "Fresh meals",
            "+10000000000",
            Guid.NewGuid());
    }

    private static async Task WaitForCallsAsync(
        FakePartnerAnalyticsService analyticsService,
        int expectedCount)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (analyticsService.Calls.Count >= expectedCount)
                return;

            await Task.Delay(10);
        }

        Assert.HasCount(expectedCount, analyticsService.Calls);
    }

    private sealed record AnalyticsCall(
        AnalyticsPeriod Period,
        Guid? FoodPointId);

    private sealed class FakePartnerAnalyticsService : IPartnerAnalyticsService
    {
        public List<AnalyticsCall> Calls { get; } = new();

        public Queue<PartnerAnalyticsDashboardDto> Dashboards { get; } = new();

        public Exception? Exception { get; set; }

        public TaskCompletionSource? WaitBeforeReturning { get; set; }

        public async Task<PartnerAnalyticsDashboardDto> GetDashboardAsync(
            AnalyticsPeriod period,
            Guid? foodPointId = null,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(new AnalyticsCall(period, foodPointId));

            if (Exception is not null)
                throw Exception;

            if (WaitBeforeReturning is not null)
                await WaitBeforeReturning.Task;

            if (Dashboards.Count == 0)
                return new PartnerAnalyticsDashboardDto();

            return Dashboards.Dequeue();
        }
    }

    private sealed class FakeFoodPointService : IFoodPointService
    {
        public List<FoodPoint> FoodPoints { get; } = new();

        public int LoadCount { get; private set; }

        public Task<FoodPoint> CreateFoodPointAsync(
            CreateFoodPointRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<FoodPoint> UpdateFoodPointAsync(
            UpdateFoodPointRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeactivateFoodPointAsync(
            Guid foodPointId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteFoodPointAsync(
            Guid foodPointId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<List<FoodPoint>> GetCurrentPartnerFoodPointsAsync(
            CancellationToken cancellationToken = default)
        {
            LoadCount++;

            return Task.FromResult(FoodPoints.ToList());
        }

        public Task<List<FoodPoint>> GetAllFoodPointsAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<FoodPoint?> GetFoodPointAsync(
            Guid foodPointId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
