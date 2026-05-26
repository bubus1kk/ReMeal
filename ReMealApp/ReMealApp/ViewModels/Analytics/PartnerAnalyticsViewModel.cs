using System.Collections.ObjectModel;
using Application.DTOs.Analytics;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ReMealApp.ViewModels.Analytics;

public partial class PartnerAnalyticsViewModel : ViewModelBase
{
    private readonly IPartnerAnalyticsService _analyticsService;
    private readonly IFoodPointService _foodPointService;

    public ObservableCollection<FoodPointFilterItem> FoodPoints { get; } =
        new();

    public ObservableCollection<AnalyticsPeriodItem> Periods { get; } =
        new();

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

            var dashboard =
                await _analyticsService.GetDashboardAsync(
                    SelectedPeriod.Value,
                    SelectedFoodPoint?.FoodPointId);

            ApplyDashboard(dashboard);

            if (!HasAnyData(dashboard))
            {
                EmptyStateMessage =
                    "Недостаточно данных для отображения аналитики.";
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

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await LoadAsync();
    }

    partial void OnSelectedPeriodChanged(
        AnalyticsPeriodItem? value)
    {
        if (value is null)
            return;

        _ = LoadAsync();
    }

    partial void OnSelectedFoodPointChanged(
        FoodPointFilterItem? value)
    {
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

    public string Name { get; set; } =
        string.Empty;

    public override string ToString()
    {
        return Name;
    }
}