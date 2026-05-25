using System.Collections.ObjectModel;
using Application.DTOs.Analytics;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScottPlot.Avalonia;

namespace ReMealApp.ViewModels.Analytics;

public partial class PartnerAnalyticsViewModel : ViewModelBase
{
    private readonly IPartnerAnalyticsService _analyticsService;
    private readonly IFoodPointService _foodPointService;

    private PartnerAnalyticsDashboardDto? _dashboard;

    private AvaPlot? _savedPortionsPlot;
    private AvaPlot? _bookingStatusesPlot;
    private AvaPlot? _topLotsPlot;
    private AvaPlot? _foodPointsPlot;
    private AvaPlot? _preventedWastePlot;

    public ObservableCollection<FoodPointFilterItem> FoodPoints { get; } = new();

    public ObservableCollection<AnalyticsPeriodItem> Periods { get; } = new();

    [ObservableProperty]
    private AnalyticsPeriodItem? _selectedPeriod;

    [ObservableProperty]
    private FoodPointFilterItem? _selectedFoodPoint;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _emptyStateMessage;

    [ObservableProperty]
    private int _issuedBookingsCount;

    [ObservableProperty]
    private int _savedPortionsCount;

    [ObservableProperty]
    private int _cancelledBookingsCount;

    [ObservableProperty]
    private int _activeBookingsCount;

    [ObservableProperty]
    private double _preventedWasteKg;

    public PartnerAnalyticsViewModel(
        IPartnerAnalyticsService analyticsService,
        IFoodPointService foodPointService)
    {
        _analyticsService = analyticsService;
        _foodPointService = foodPointService;

        InitializePeriods();
    }

    public void AttachPlots(
        AvaPlot savedPortionsPlot,
        AvaPlot bookingStatusesPlot,
        AvaPlot topLotsPlot,
        AvaPlot foodPointsPlot,
        AvaPlot preventedWastePlot)
    {
        _savedPortionsPlot = savedPortionsPlot;
        _bookingStatusesPlot = bookingStatusesPlot;
        _topLotsPlot = topLotsPlot;
        _foodPointsPlot = foodPointsPlot;
        _preventedWastePlot = preventedWastePlot;
    }

    private void InitializePeriods()
    {
        Periods.Add(new AnalyticsPeriodItem("7 дней", AnalyticsPeriod.Last7Days));
        Periods.Add(new AnalyticsPeriodItem("30 дней", AnalyticsPeriod.Last30Days));
        Periods.Add(new AnalyticsPeriodItem("Все время", AnalyticsPeriod.AllTime));

        SelectedPeriod = Periods.FirstOrDefault();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            EmptyStateMessage = null;

            await LoadFoodPointsAsync();

            if (SelectedPeriod is null)
                return;

            _dashboard = await _analyticsService.GetDashboardAsync(
                SelectedPeriod.Value,
                SelectedFoodPoint?.FoodPointId);

            ApplyDashboard(_dashboard);

            BuildCharts(_dashboard);

            if (!HasAnyData(_dashboard))
            {
                EmptyStateMessage = "Недостаточно данных для отображения аналитики.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedPeriodChanged(AnalyticsPeriodItem? value)
    {
        if (value is not null)
            _ = LoadAsync();
    }

    partial void OnSelectedFoodPointChanged(FoodPointFilterItem? value)
    {
        _ = LoadAsync();
    }

    private async Task LoadFoodPointsAsync()
    {
        if (FoodPoints.Count > 0)
            return;

        var foodPoints = await _foodPointService.GetCurrentPartnerFoodPointsAsync();

        FoodPoints.Clear();

        FoodPoints.Add(new FoodPointFilterItem
        {
            FoodPointId = null,
            Name = "Все точки"
        });

        foreach (var point in foodPoints)
        {
            FoodPoints.Add(new FoodPointFilterItem
            {
                FoodPointId = point.Id,
                Name = point.Name
            });
        }

        SelectedFoodPoint ??= FoodPoints.FirstOrDefault();
    }

    private void ApplyDashboard(PartnerAnalyticsDashboardDto dashboard)
    {
        IssuedBookingsCount = dashboard.IssuedBookingsCount;
        SavedPortionsCount = dashboard.SavedPortionsCount;
        CancelledBookingsCount = dashboard.CancelledBookingsCount;
        ActiveBookingsCount = dashboard.ActiveBookingsCount;
        PreventedWasteKg = dashboard.PreventedWasteKg;
    }

    private void BuildCharts(PartnerAnalyticsDashboardDto dashboard)
    {
        BuildSavedPortionsChart(dashboard);
        BuildStatusesChart(dashboard);
        BuildTopLotsChart(dashboard);
        BuildFoodPointsChart(dashboard);
        BuildWasteChart(dashboard);
    }

    private void BuildSavedPortionsChart(PartnerAnalyticsDashboardDto dashboard)
    {
        if (_savedPortionsPlot is null)
            return;

        _savedPortionsPlot.Plot.Clear();

        var values = dashboard.SavedPortionsByDay
            .Select(x => (double)x.Value)
            .ToArray();

        var labels = dashboard.SavedPortionsByDay
            .Select(x => x.Date.ToString("dd.MM"))
            .ToArray();

        _savedPortionsPlot.Plot.Add.Signal(values);

        _savedPortionsPlot.Plot.Axes.Bottom.TickGenerator =
            new ScottPlot.TickGenerators.NumericManual(
                Enumerable.Range(0, labels.Length)
                    .Select(i => new ScottPlot.Tick(i, labels[i]))
                    .ToArray());

        _savedPortionsPlot.Refresh();
    }

    private void BuildStatusesChart(PartnerAnalyticsDashboardDto dashboard)
    {
        if (_bookingStatusesPlot is null)
            return;

        _bookingStatusesPlot.Plot.Clear();

        double[] values = dashboard.BookingStatusDistribution
            .Select(x => (double)x.Count)
            .ToArray();

        string[] labels = dashboard.BookingStatusDistribution
            .Select(x => $"{x.StatusName} ({x.Count})")
            .ToArray();

        var pie = _bookingStatusesPlot.Plot.Add.Pie(values);

        for (int i = 0; i < pie.Slices.Count; i++)
        {
            pie.Slices[i].Label = labels[i];
        }

        _bookingStatusesPlot.Plot.Legend.IsVisible = true;

        _bookingStatusesPlot.Refresh();
    }

    private void BuildTopLotsChart(PartnerAnalyticsDashboardDto dashboard)
    {
        if (_topLotsPlot is null)
            return;

        _topLotsPlot.Plot.Clear();

        double[] values = dashboard.TopLotsByIssuedQuantity
            .Select(x => (double)x.IssuedQuantity)
            .ToArray();

        string[] labels = dashboard.TopLotsByIssuedQuantity
            .Select(x => x.LotTitle)
            .ToArray();

        _topLotsPlot.Plot.Add.Bars(values);

        _topLotsPlot.Plot.Axes.Bottom.TickGenerator =
            new ScottPlot.TickGenerators.NumericManual(
                Enumerable.Range(0, labels.Length)
                    .Select(i => new ScottPlot.Tick(i, labels[i]))
                    .ToArray());

        _topLotsPlot.Refresh();
    }

    private void BuildFoodPointsChart(PartnerAnalyticsDashboardDto dashboard)
    {
        if (_foodPointsPlot is null)
            return;

        _foodPointsPlot.Plot.Clear();

        double[] values = dashboard.IssuedByFoodPoint
            .Select(x => (double)x.IssuedQuantity)
            .ToArray();

        string[] labels = dashboard.IssuedByFoodPoint
            .Select(x => x.FoodPointName)
            .ToArray();

        _foodPointsPlot.Plot.Add.Bars(values);

        _foodPointsPlot.Plot.Axes.Bottom.TickGenerator =
            new ScottPlot.TickGenerators.NumericManual(
                Enumerable.Range(0, labels.Length)
                    .Select(i => new ScottPlot.Tick(i, labels[i]))
                    .ToArray());

        _foodPointsPlot.Refresh();
    }

    private void BuildWasteChart(PartnerAnalyticsDashboardDto dashboard)
    {
        if (_preventedWastePlot is null)
            return;

        _preventedWastePlot.Plot.Clear();

        double[] values = dashboard.PreventedWasteByDay
            .Select(x => x.Value)
            .ToArray();

        string[] labels = dashboard.PreventedWasteByDay
            .Select(x => x.Date.ToString("dd.MM"))
            .ToArray();

        _preventedWastePlot.Plot.Add.Signal(values);

        _preventedWastePlot.Plot.Axes.Bottom.TickGenerator =
            new ScottPlot.TickGenerators.NumericManual(
                Enumerable.Range(0, labels.Length)
                    .Select(i => new ScottPlot.Tick(i, labels[i]))
                    .ToArray());

        _preventedWastePlot.Refresh();
    }

    private static bool HasAnyData(PartnerAnalyticsDashboardDto dashboard)
    {
        return dashboard.IssuedBookingsCount > 0 ||
               dashboard.SavedPortionsCount > 0 ||
               dashboard.CancelledBookingsCount > 0 ||
               dashboard.ActiveBookingsCount > 0;
    }
}

public sealed class AnalyticsPeriodItem
{
    public AnalyticsPeriodItem(string title, AnalyticsPeriod value)
    {
        Title = title;
        Value = value;
    }

    public string Title { get; }

    public AnalyticsPeriod Value { get; }

    public override string ToString() => Title;
}

public sealed class FoodPointFilterItem
{
    public Guid? FoodPointId { get; set; }

    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}