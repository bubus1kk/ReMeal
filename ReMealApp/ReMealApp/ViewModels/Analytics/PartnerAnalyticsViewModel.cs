using System.Collections.ObjectModel;
using Application.DTOs.Analytics;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ReMealApp.ViewModels.Analytics;

public partial class PartnerAnalyticsViewModel
    : ViewModelBase
{
    private readonly IPartnerAnalyticsService
        _analyticsService;

    private readonly IFoodPointService
        _foodPointService;

    private bool
        _isInitialized;

    public ObservableCollection<FoodPointFilterItem>
        FoodPoints
    { get; } = new();

    public ObservableCollection<AnalyticsPeriodItem>
        Periods
    { get; } = new();

    public ObservableCollection<AnalyticsBarItem>
        SavedPortionsChartItems
    { get; } = new();

    public ObservableCollection<AnalyticsBarItem>
        BookingStatusesChartItems
    { get; } = new();

    public ObservableCollection<AnalyticsBarItem>
        TopLotsChartItems
    { get; } = new();

    public ObservableCollection<AnalyticsBarItem>
        FoodPointsChartItems
    { get; } = new();

    [ObservableProperty]
    private AnalyticsPeriodItem?
        _selectedPeriod;

    [ObservableProperty]
    private FoodPointFilterItem?
        _selectedFoodPoint;

    [ObservableProperty]
    private bool
        _isLoading;

    [ObservableProperty]
    private string?
        _errorMessage;

    [ObservableProperty]
    private string?
        _emptyStateMessage;

    [ObservableProperty]
    private int
        _issuedBookingsCount;

    [ObservableProperty]
    private int
        _savedPortionsCount;

    [ObservableProperty]
    private int
        _cancelledBookingsCount;

    [ObservableProperty]
    private int
        _activeBookingsCount;

    [ObservableProperty]
    private double
        _preventedWasteKg;

    [ObservableProperty]
    private double
        _completionRate;

    [ObservableProperty]
    private double
        _cancellationRate;

    [ObservableProperty]
    private string
        _mostPopularLot = "-";

    [ObservableProperty]
    private string
        _bestFoodPoint = "-";

    public PartnerAnalyticsViewModel(
        IPartnerAnalyticsService analyticsService,
        IFoodPointService foodPointService)
    {
        _analyticsService =
            analyticsService;

        _foodPointService =
            foodPointService;

        InitializePeriods();
    }

    private void InitializePeriods()
    {
        Periods.Add(
            new AnalyticsPeriodItem(
                "7 дней",
                AnalyticsPeriod.Last7Days));

        Periods.Add(
            new AnalyticsPeriodItem(
                "30 дней",
                AnalyticsPeriod.Last30Days));

        Periods.Add(
            new AnalyticsPeriodItem(
                "Все время",
                AnalyticsPeriod.AllTime));

        SelectedPeriod =
            Periods.FirstOrDefault();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;

            ErrorMessage = null;

            EmptyStateMessage = null;

            await LoadFoodPointsAsync();

            if (SelectedPeriod is null)
                return;

            var dashboard =
                await _analyticsService
                    .GetDashboardAsync(
                        SelectedPeriod.Value,
                        SelectedFoodPoint
                            ?.FoodPointId);

            ApplyDashboard(dashboard);

            if (!HasAnyData(dashboard))
            {
                EmptyStateMessage =
                    "Недостаточно данных для отображения аналитики.";
            }

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"Ошибка загрузки аналитики: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await LoadAsync();
    }

    partial void OnSelectedPeriodChanged(
        AnalyticsPeriodItem? value)
    {
        if (!_isInitialized)
            return;

        if (value is null)
            return;

        _ = LoadAsync();
    }

    partial void OnSelectedFoodPointChanged(
        FoodPointFilterItem? value)
    {
        if (!_isInitialized)
            return;

        _ = LoadAsync();
    }

    private async Task LoadFoodPointsAsync()
    {
        if (FoodPoints.Count > 0)
            return;

        var foodPoints =
            await _foodPointService
                .GetCurrentPartnerFoodPointsAsync();

        FoodPoints.Clear();

        FoodPoints.Add(
            new FoodPointFilterItem
            {
                FoodPointId = null,
                Name = "Все точки"
            });

        foreach (var point in foodPoints)
        {
            FoodPoints.Add(
                new FoodPointFilterItem
                {
                    FoodPointId = point.Id,
                    Name = point.Name
                });
        }

        if (SelectedFoodPoint is null)
        {
            SelectedFoodPoint =
                FoodPoints.FirstOrDefault();
        }
    }

    private void ApplyDashboard(
        PartnerAnalyticsDashboardDto dashboard)
    {
        IssuedBookingsCount =
            dashboard.IssuedBookingsCount;

        SavedPortionsCount =
            dashboard.SavedPortionsCount;

        CancelledBookingsCount =
            dashboard.CancelledBookingsCount;

        ActiveBookingsCount =
            dashboard.ActiveBookingsCount;

        PreventedWasteKg =
            dashboard.PreventedWasteKg;

        CalculateAdditionalMetrics();

        BuildSavedPortionsChartData(
            dashboard);

        BuildBookingStatusesChartData(
            dashboard);

        BuildTopLotsChartData(
            dashboard);

        BuildFoodPointsChartData(
            dashboard);
    }

    private void CalculateAdditionalMetrics()
    {
        var totalBookings =
            IssuedBookingsCount +
            CancelledBookingsCount +
            ActiveBookingsCount;

        if (totalBookings <= 0)
        {
            CompletionRate = 0;
            CancellationRate = 0;
            return;
        }

        CompletionRate =
            Math.Round(
                IssuedBookingsCount /
                (double)totalBookings * 100,
                1);

        CancellationRate =
            Math.Round(
                CancelledBookingsCount /
                (double)totalBookings * 100,
                1);
    }

    private void BuildSavedPortionsChartData(
        PartnerAnalyticsDashboardDto dashboard)
    {
        SavedPortionsChartItems.Clear();

        var items =
            dashboard.SavedPortionsByDay
                .ToList();

        if (items.Count == 0)
            return;

        var maxValue =
            items.Max(x => x.Value);

        if (maxValue <= 0)
            maxValue = 1;

        foreach (var item in items)
        {
            var calculatedHeight =
                item.Value /
                maxValue * 220;

            if (calculatedHeight < 12)
            {
                calculatedHeight = 12;
            }

            SavedPortionsChartItems.Add(
                new AnalyticsBarItem
                {
                    Label =
                        item.Date
                            .ToString("dd.MM"),

                    Value =
                        item.Value,

                    Height =
                        calculatedHeight
                });
        }
    }

    private void BuildBookingStatusesChartData(
        PartnerAnalyticsDashboardDto dashboard)
    {
        BookingStatusesChartItems.Clear();

        var items =
            dashboard.BookingStatusDistribution
                .ToList();

        if (items.Count == 0)
            return;

        var maxValue =
            items.Max(x => x.Count);

        if (maxValue <= 0)
            maxValue = 1;

        foreach (var item in items)
        {
            var calculatedHeight =
                item.Count /
                (double)maxValue * 220;

            if (calculatedHeight < 12)
            {
                calculatedHeight = 12;
            }

            BookingStatusesChartItems.Add(
                new AnalyticsBarItem
                {
                    Label =
                        item.StatusName,

                    Value =
                        item.Count,

                    Height =
                        calculatedHeight
                });
        }
    }

    private void BuildTopLotsChartData(
        PartnerAnalyticsDashboardDto dashboard)
    {
        TopLotsChartItems.Clear();

        var items =
            dashboard.TopLotsByIssuedQuantity
                .ToList();

        if (items.Count == 0)
        {
            MostPopularLot = "-";
            return;
        }

        var maxValue =
            items.Max(
                x => x.IssuedQuantity);

        if (maxValue <= 0)
            maxValue = 1;

        MostPopularLot =
            items.First().LotTitle;

        foreach (var item in items)
        {
            var calculatedWidth =
                item.IssuedQuantity /
                (double)maxValue * 320;

            if (calculatedWidth < 20)
            {
                calculatedWidth = 20;
            }

            TopLotsChartItems.Add(
                new AnalyticsBarItem
                {
                    Label =
                        item.LotTitle,

                    Value =
                        item.IssuedQuantity,

                    Width =
                        calculatedWidth
                });
        }
    }

    private void BuildFoodPointsChartData(
        PartnerAnalyticsDashboardDto dashboard)
    {
        FoodPointsChartItems.Clear();

        var items =
            dashboard.IssuedByFoodPoint
                .ToList();

        if (items.Count == 0)
        {
            BestFoodPoint = "-";
            return;
        }

        var maxValue =
            items.Max(
                x => x.IssuedQuantity);

        if (maxValue <= 0)
            maxValue = 1;

        BestFoodPoint =
            items.First().FoodPointName;

        foreach (var item in items)
        {
            var calculatedWidth =
                item.IssuedQuantity /
                (double)maxValue * 320;

            if (calculatedWidth < 20)
            {
                calculatedWidth = 20;
            }

            FoodPointsChartItems.Add(
                new AnalyticsBarItem
                {
                    Label =
                        item.FoodPointName,

                    Value =
                        item.IssuedQuantity,

                    Width =
                        calculatedWidth
                });
        }
    }

    private static bool HasAnyData(
        PartnerAnalyticsDashboardDto dashboard)
    {
        return
            dashboard.IssuedBookingsCount > 0 ||
            dashboard.SavedPortionsCount > 0 ||
            dashboard.CancelledBookingsCount > 0 ||
            dashboard.ActiveBookingsCount > 0;
    }
}

public sealed class AnalyticsPeriodItem
{
    public AnalyticsPeriodItem(
        string title,
        AnalyticsPeriod value)
    {
        Title = title;
        Value = value;
    }

    public string Title
    { get; }

    public AnalyticsPeriod Value
    { get; }

    public override string ToString()
    {
        return Title;
    }
}

public sealed class FoodPointFilterItem
{
    public Guid? FoodPointId
    { get; set; }

    public string Name
    { get; set; } = string.Empty;

    public override string ToString()
    {
        return Name;
    }
}

public sealed class AnalyticsBarItem
{
    public string Label
    { get; set; } = string.Empty;

    public double Value
    { get; set; }

    public double Height
    { get; set; }

    public double Width
    { get; set; }
}