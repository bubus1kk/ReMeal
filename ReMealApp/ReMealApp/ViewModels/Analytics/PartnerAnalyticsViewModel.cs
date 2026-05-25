using System.Collections.ObjectModel;
using Application.DTOs.Analytics;
using Application.Interfaces;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScottPlot;
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

    private bool _isInitialized;

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

    [RelayCommand]
    private async Task ReloadAsync()
    {
        await LoadAsync();
    }

    public async Task InitializeAsync()
    {
        await LoadAsync();
    }

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

        ApplyDarkTheme(_savedPortionsPlot);
        ApplyDarkTheme(_bookingStatusesPlot);
        ApplyDarkTheme(_topLotsPlot);
        ApplyDarkTheme(_foodPointsPlot);
        ApplyDarkTheme(_preventedWastePlot);

        _isInitialized = true;
    }

    private static void ApplyDarkTheme(AvaPlot? plot)
    {
        if (plot is null)
            return;

        plot.Plot.FigureBackground.Color =
            ScottPlot.Colors.Transparent;

        plot.Plot.DataBackground.Color =
            ScottPlot.Color.FromHex("#1E1E1E");

        plot.Plot.Axes.Color(
            ScottPlot.Color.FromHex("#D0D0D0"));

        plot.Refresh();
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

        SelectedPeriod = Periods.FirstOrDefault();
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

            _dashboard =
                await _analyticsService.GetDashboardAsync(
                    SelectedPeriod.Value,
                    SelectedFoodPoint?.FoodPointId);

            ApplyDashboard(_dashboard);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                BuildCharts(_dashboard);
            });

            if (!HasAnyData(_dashboard))
            {
                EmptyStateMessage =
                    "Недостаточно данных для отображения аналитики.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.ToString();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedPeriodChanged(
        AnalyticsPeriodItem? value)
    {
    }

    partial void OnSelectedFoodPointChanged(
        FoodPointFilterItem? value)
    {
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await LoadAsync();
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
    }

    private void BuildCharts(
        PartnerAnalyticsDashboardDto dashboard)
    {
        if (!_isInitialized)
            return;

        BuildSavedPortionsChart(dashboard);
        BuildStatusesChart(dashboard);
        BuildTopLotsChart(dashboard);
        BuildFoodPointsChart(dashboard);
        BuildWasteChart(dashboard);
    }

    private void BuildSavedPortionsChart(
        PartnerAnalyticsDashboardDto dashboard)
    {
        if (_savedPortionsPlot is null)
            return;

        _savedPortionsPlot.Plot.Clear();

        double[] values = dashboard.SavedPortionsByDay
            .Select(x => (double)x.Value)
            .ToArray();

        if (values.Length == 0)
        {
            _savedPortionsPlot.Refresh();
            return;
        }

        _savedPortionsPlot.Plot.Add.Signal(values);

        _savedPortionsPlot.Refresh();
    }

    private void BuildStatusesChart(
        PartnerAnalyticsDashboardDto dashboard)
    {
        if (_bookingStatusesPlot is null)
            return;

        _bookingStatusesPlot.Plot.Clear();

        double[] values = dashboard.BookingStatusDistribution
            .Select(x => (double)x.Count)
            .ToArray();

        if (values.Length == 0)
        {
            _bookingStatusesPlot.Refresh();
            return;
        }

        try
        {
            _bookingStatusesPlot.Plot.Add.Pie(values);
        }
        catch
        {
            return;
        }

        _bookingStatusesPlot.Refresh();
    }

    private void BuildTopLotsChart(
        PartnerAnalyticsDashboardDto dashboard)
    {
        if (_topLotsPlot is null)
            return;

        _topLotsPlot.Plot.Clear();

        double[] values = dashboard.TopLotsByIssuedQuantity
            .Select(x => (double)x.IssuedQuantity)
            .ToArray();

        if (values.Length == 0)
        {
            _topLotsPlot.Refresh();
            return;
        }

        _topLotsPlot.Plot.Add.Bars(values);

        _topLotsPlot.Refresh();
    }

    private void BuildFoodPointsChart(
        PartnerAnalyticsDashboardDto dashboard)
    {
        if (_foodPointsPlot is null)
            return;

        _foodPointsPlot.Plot.Clear();

        double[] values = dashboard.IssuedByFoodPoint
            .Select(x => (double)x.IssuedQuantity)
            .ToArray();

        if (values.Length == 0)
        {
            _foodPointsPlot.Refresh();
            return;
        }

        _foodPointsPlot.Plot.Add.Bars(values);

        _foodPointsPlot.Refresh();
    }

    private void BuildWasteChart(
        PartnerAnalyticsDashboardDto dashboard)
    {
        if (_preventedWastePlot is null)
            return;

        _preventedWastePlot.Plot.Clear();

        double[] values = dashboard.PreventedWasteByDay
            .Select(x => x.Value)
            .ToArray();

        if (values.Length == 0)
        {
            _preventedWastePlot.Refresh();
            return;
        }

        _preventedWastePlot.Plot.Add.Signal(values);

        _preventedWastePlot.Refresh();
    }

    private static bool HasAnyData(
        PartnerAnalyticsDashboardDto dashboard)
    {
        return dashboard.IssuedBookingsCount > 0 ||
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

    public string Title { get; }

    public AnalyticsPeriod Value { get; }

    public override string ToString()
    {
        return Title;
    }
}

public sealed class FoodPointFilterItem
{
    public Guid? FoodPointId { get; set; }

    public string Name { get; set; } = string.Empty;

    public override string ToString()
    {
        return Name;
    }
}