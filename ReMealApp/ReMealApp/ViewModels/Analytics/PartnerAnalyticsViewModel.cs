using System.Collections.ObjectModel;
using Application.DTOs.Analytics;
using Application.Interfaces;
using Avalonia;
using Avalonia.Media;
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

    private const double PieChartSize = 220;
    private const double PieChartOuterRadius = 104;
    private const double PieChartInnerRadius = 58;
    private const double VerticalChartMaxHeight = 168;
    private const double ProgressMaxWidth = 390;

    private static readonly string[] PieChartPalette =
    {
        "#75C943",
        "#4FA3FF",
        "#FFB84D",
        "#FF6B6B",
        "#B277FF"
    };

    private bool
        _isInitialized;

    private bool
        _isUpdatingFilters;

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

    public ObservableCollection<AnalyticsBarItem>
        PreventedWasteChartItems
    { get; } = new();

    public ObservableCollection<AnalyticsPieSliceItem>
        BookingStatusPieSlices
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
    private bool
        _hasNoFoodPoints;

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

    public bool HasErrorMessage =>
        !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasEmptyStateMessage =>
        !string.IsNullOrWhiteSpace(EmptyStateMessage);

    public bool IsFoodPointFilterEnabled =>
        !HasNoFoodPoints &&
        FoodPoints.Any(x => x.FoodPointId.HasValue);

    public bool HasAnalyticsContent =>
        !IsLoading &&
        !HasNoFoodPoints &&
        !HasErrorMessage;

    public bool HasSavedPortionsChartData =>
        SavedPortionsChartItems.Count > 0;

    public bool HasNoSavedPortionsChartData =>
        !IsLoading &&
        !HasNoFoodPoints &&
        !HasSavedPortionsChartData;

    public bool HasBookingStatusesChartData =>
        BookingStatusPieSlices.Count > 0;

    public bool HasNoBookingStatusesChartData =>
        !IsLoading &&
        !HasNoFoodPoints &&
        !HasBookingStatusesChartData;

    public bool HasTopLotsChartData =>
        TopLotsChartItems.Count > 0;

    public bool HasNoTopLotsChartData =>
        !IsLoading &&
        !HasNoFoodPoints &&
        !HasTopLotsChartData;

    public bool HasFoodPointsChartData =>
        FoodPointsChartItems.Count > 0;

    public bool HasNoFoodPointsChartData =>
        !IsLoading &&
        !HasNoFoodPoints &&
        !HasFoodPointsChartData;

    public bool HasPreventedWasteChartData =>
        PreventedWasteChartItems.Count > 0;

    public bool HasNoPreventedWasteChartData =>
        !IsLoading &&
        !HasNoFoodPoints &&
        !HasPreventedWasteChartData;

    public int TotalBookingsCount =>
        IssuedBookingsCount +
        CancelledBookingsCount +
        ActiveBookingsCount;

    public string IssuedBookingsDisplay =>
        IssuedBookingsCount.ToString("N0");

    public string SavedPortionsDisplay =>
        SavedPortionsCount.ToString("N0");

    public string CancelledBookingsDisplay =>
        CancelledBookingsCount.ToString("N0");

    public string ActiveBookingsDisplay =>
        ActiveBookingsCount.ToString("N0");

    public string PreventedWasteDisplay =>
        $"{PreventedWasteKg:0.##} кг";

    public string IssuedBookingsCaption =>
        "За выбранный период";

    public string SavedPortionsCaption =>
        "По выданным бронированиям";

    public string CancelledBookingsCaption =>
        "За выбранный период";

    public string ActiveBookingsCaption =>
        "Активные бронирования";

    public string PreventedWasteCaption =>
        "0,35 кг за порцию";

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
                "90 дней",
                AnalyticsPeriod.Last90Days));

        Periods.Add(
            new AnalyticsPeriodItem(
                "Всё время",
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

            if (HasNoFoodPoints)
            {
                ClearDashboard();

                EmptyStateMessage =
                    "У вас пока нет точек питания. Аналитика появится после создания точки и публикации лотов.";

                _isInitialized = true;
                return;
            }

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
                    "Недостаточно данных для аналитики. Статистика появится после первых бронирований.";
            }

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            ClearDashboard();

            ErrorMessage =
                $"Ошибка загрузки аналитики: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            NotifyAnalyticsStateProperties();
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
        if (!_isInitialized ||
            _isUpdatingFilters)
        {
            return;
        }

        if (value is null)
            return;

        _ = LoadAsync();
    }

    partial void OnSelectedFoodPointChanged(
        FoodPointFilterItem? value)
    {
        if (!_isInitialized ||
            _isUpdatingFilters)
        {
            return;
        }

        _ = LoadAsync();
    }

    private async Task LoadFoodPointsAsync()
    {
        var selectedFoodPointId =
            SelectedFoodPoint?.FoodPointId;

        var foodPoints =
            await _foodPointService
                .GetCurrentPartnerFoodPointsAsync();

        _isUpdatingFilters = true;

        try
        {
            FoodPoints.Clear();

            if (foodPoints.Count == 0)
            {
                HasNoFoodPoints = true;

                FoodPoints.Add(
                    new FoodPointFilterItem
                    {
                        FoodPointId = null,
                        Name = "Нет точек питания"
                    });

                SelectedFoodPoint =
                    FoodPoints.FirstOrDefault();

                return;
            }

            HasNoFoodPoints = false;

            FoodPoints.Add(
                new FoodPointFilterItem
                {
                    FoodPointId = null,
                    Name = "Все точки"
                });

            foreach (var point in foodPoints
                .OrderBy(x => x.Name))
            {
                FoodPoints.Add(
                    new FoodPointFilterItem
                    {
                        FoodPointId = point.Id,
                        Name = point.Name
                    });
            }

            SelectedFoodPoint =
                FoodPoints.FirstOrDefault(x =>
                    x.FoodPointId == selectedFoodPointId) ??
                FoodPoints.FirstOrDefault();
        }
        finally
        {
            _isUpdatingFilters = false;
            OnPropertyChanged(nameof(IsFoodPointFilterEnabled));
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

        BuildPreventedWasteChartData(
            dashboard);

        NotifyAnalyticsStateProperties();
    }

    private void ClearDashboard()
    {
        IssuedBookingsCount = 0;
        SavedPortionsCount = 0;
        CancelledBookingsCount = 0;
        ActiveBookingsCount = 0;
        PreventedWasteKg = 0;
        CompletionRate = 0;
        CancellationRate = 0;
        MostPopularLot = "-";
        BestFoodPoint = "-";

        SavedPortionsChartItems.Clear();
        BookingStatusesChartItems.Clear();
        TopLotsChartItems.Clear();
        FoodPointsChartItems.Clear();
        PreventedWasteChartItems.Clear();
        BookingStatusPieSlices.Clear();

        NotifyAnalyticsStateProperties();
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

        if (items.Count == 0 ||
            items.All(x => x.Value <= 0))
        {
            return;
        }

        var maxValue =
            Math.Max(
                1,
                items.Max(x => x.Value));

        foreach (var item in items)
        {
            SavedPortionsChartItems.Add(
                AnalyticsBarItem.CreateVertical(
                    item.Date.ToString("dd.MM"),
                    item.Value,
                    maxValue,
                    VerticalChartMaxHeight));
        }
    }

    private void BuildBookingStatusesChartData(
        PartnerAnalyticsDashboardDto dashboard)
    {
        BookingStatusesChartItems.Clear();
        BookingStatusPieSlices.Clear();

        var items =
            dashboard.BookingStatusDistribution
                .Where(x => x.Count > 0)
                .ToList();

        if (items.Count == 0)
            return;

        var maxValue =
            items.Max(x => x.Count);

        var totalValue =
            items.Sum(x => x.Count);

        if (maxValue <= 0)
            maxValue = 1;

        var startAngle = -90d;

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];

            BookingStatusesChartItems.Add(
                AnalyticsBarItem.CreateVertical(
                    item.StatusName,
                    item.Count,
                    maxValue,
                    VerticalChartMaxHeight));

            var sweepAngle =
                item.Count /
                (double)totalValue * 360;

            BookingStatusPieSlices.Add(
                new AnalyticsPieSliceItem
                {
                    Label =
                        item.StatusName,

                    Count =
                        item.Count,

                    Percent =
                        Math.Round(
                            item.Count /
                            (double)totalValue * 100,
                            1),

                    Brush =
                        new SolidColorBrush(
                            Color.Parse(
                                PieChartPalette[
                                    index % PieChartPalette.Length])),

                    Geometry =
                        CreateDonutSliceGeometry(
                            startAngle,
                            sweepAngle)
                });

            startAngle += sweepAngle;
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

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];

            TopLotsChartItems.Add(
                AnalyticsBarItem.CreateProgress(
                    index + 1,
                    item.LotTitle,
                    item.IssuedQuantity,
                    maxValue,
                    ProgressMaxWidth));
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

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];

            FoodPointsChartItems.Add(
                AnalyticsBarItem.CreateProgress(
                    index + 1,
                    item.FoodPointName,
                    item.IssuedQuantity,
                    maxValue,
                    ProgressMaxWidth));
        }
    }

    private void BuildPreventedWasteChartData(
        PartnerAnalyticsDashboardDto dashboard)
    {
        PreventedWasteChartItems.Clear();

        var items =
            dashboard.PreventedWasteByDay
                .ToList();

        if (items.Count == 0 ||
            items.All(x => x.Value <= 0))
        {
            return;
        }

        var maxValue =
            Math.Max(
                1,
                items.Max(x => x.Value));

        foreach (var item in items)
        {
            PreventedWasteChartItems.Add(
                AnalyticsBarItem.CreateVertical(
                    item.Date.ToString("dd.MM"),
                    item.Value,
                    maxValue,
                    VerticalChartMaxHeight));
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

    private static Geometry CreateDonutSliceGeometry(
        double startAngle,
        double sweepAngle)
    {
        var center =
            new Point(
                PieChartSize / 2,
                PieChartSize / 2);

        var visibleSweepAngle =
            Math.Clamp(
                sweepAngle,
                0.1,
                359.99);

        var endAngle =
            startAngle + visibleSweepAngle;

        var outerStart =
            GetArcPoint(
                center,
                PieChartOuterRadius,
                startAngle);

        var outerEnd =
            GetArcPoint(
                center,
                PieChartOuterRadius,
                endAngle);

        var innerStart =
            GetArcPoint(
                center,
                PieChartInnerRadius,
                startAngle);

        var innerEnd =
            GetArcPoint(
                center,
                PieChartInnerRadius,
                endAngle);

        var geometry =
            new StreamGeometry();

        using var context =
            geometry.Open();

        var isLargeArc =
            visibleSweepAngle > 180;

        context.BeginFigure(
            outerStart,
            true);

        context.ArcTo(
            outerEnd,
            new Size(
                PieChartOuterRadius,
                PieChartOuterRadius),
            0,
            isLargeArc,
            SweepDirection.Clockwise);

        context.LineTo(innerEnd);

        context.ArcTo(
            innerStart,
            new Size(
                PieChartInnerRadius,
                PieChartInnerRadius),
            0,
            isLargeArc,
            SweepDirection.CounterClockwise);

        context.EndFigure(true);

        return geometry;
    }

    private static Point GetArcPoint(
        Point center,
        double radius,
        double angle)
    {
        var radians =
            angle * Math.PI / 180;

        return new Point(
            center.X + Math.Cos(radians) * radius,
            center.Y + Math.Sin(radians) * radius);
    }

    private void NotifyAnalyticsStateProperties()
    {
        OnPropertyChanged(nameof(IsFoodPointFilterEnabled));
        OnPropertyChanged(nameof(HasAnalyticsContent));
        OnPropertyChanged(nameof(HasSavedPortionsChartData));
        OnPropertyChanged(nameof(HasNoSavedPortionsChartData));
        OnPropertyChanged(nameof(HasBookingStatusesChartData));
        OnPropertyChanged(nameof(HasNoBookingStatusesChartData));
        OnPropertyChanged(nameof(HasTopLotsChartData));
        OnPropertyChanged(nameof(HasNoTopLotsChartData));
        OnPropertyChanged(nameof(HasFoodPointsChartData));
        OnPropertyChanged(nameof(HasNoFoodPointsChartData));
        OnPropertyChanged(nameof(HasPreventedWasteChartData));
        OnPropertyChanged(nameof(HasNoPreventedWasteChartData));
        OnPropertyChanged(nameof(TotalBookingsCount));
    }

    partial void OnIssuedBookingsCountChanged(int value)
    {
        OnPropertyChanged(nameof(IssuedBookingsDisplay));
        OnPropertyChanged(nameof(TotalBookingsCount));
    }

    partial void OnSavedPortionsCountChanged(int value)
    {
        OnPropertyChanged(nameof(SavedPortionsDisplay));
    }

    partial void OnCancelledBookingsCountChanged(int value)
    {
        OnPropertyChanged(nameof(CancelledBookingsDisplay));
        OnPropertyChanged(nameof(TotalBookingsCount));
    }

    partial void OnActiveBookingsCountChanged(int value)
    {
        OnPropertyChanged(nameof(ActiveBookingsDisplay));
        OnPropertyChanged(nameof(TotalBookingsCount));
    }

    partial void OnPreventedWasteKgChanged(double value)
    {
        OnPropertyChanged(nameof(PreventedWasteDisplay));
    }

    partial void OnHasNoFoodPointsChanged(bool value)
    {
        NotifyAnalyticsStateProperties();
    }

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasErrorMessage));
        OnPropertyChanged(nameof(HasAnalyticsContent));
    }

    partial void OnEmptyStateMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasEmptyStateMessage));
    }

    partial void OnIsLoadingChanged(bool value)
    {
        NotifyAnalyticsStateProperties();
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
    public int Rank
    { get; set; }

    public string Label
    { get; set; } = string.Empty;

    public double Value
    { get; set; }

    public double Height
    { get; set; }

    public double Width
    { get; set; }

    public string RankText =>
        Rank > 0
            ? Rank.ToString()
            : string.Empty;

    public string DisplayValue =>
        Math.Abs(Value % 1) < 0.001
            ? Value.ToString("0")
            : Value.ToString("0.##");

    public static AnalyticsBarItem CreateVertical(
        string label,
        double value,
        double maxValue,
        double maxHeight)
    {
        var height =
            value <= 0
                ? 0
                : Math.Max(
                    10,
                    value / maxValue * maxHeight);

        return new AnalyticsBarItem
        {
            Label = label,
            Value = value,
            Height = height
        };
    }

    public static AnalyticsBarItem CreateProgress(
        int rank,
        string label,
        double value,
        double maxValue,
        double maxWidth)
    {
        var width =
            value <= 0
                ? 0
                : Math.Max(
                    14,
                    value / maxValue * maxWidth);

        return new AnalyticsBarItem
        {
            Rank = rank,
            Label = label,
            Value = value,
            Width = width
        };
    }
}

public sealed class AnalyticsPieSliceItem
{
    public string Label
    { get; set; } = string.Empty;

    public int Count
    { get; set; }

    public double Percent
    { get; set; }

    public IBrush Brush
    { get; set; } = Brushes.Transparent;

    public Geometry Geometry
    { get; set; } = new StreamGeometry();

    public string DisplayValue =>
        $"{Count} ({Percent:0.#}%)";
}
