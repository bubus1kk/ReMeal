using Avalonia.Controls;
using ReMealApp.ViewModels.Analytics;
using ScottPlot.Avalonia;

namespace ReMealApp.Views.Analytics;

public partial class PartnerAnalyticsView : UserControl
{
    public PartnerAnalyticsView()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not PartnerAnalyticsViewModel vm)
            return;

        vm.AttachPlots(
            this.FindControl<AvaPlot>("SavedPortionsPlot")!,
            this.FindControl<AvaPlot>("BookingStatusesPlot")!,
            this.FindControl<AvaPlot>("TopLotsPlot")!,
            this.FindControl<AvaPlot>("FoodPointsPlot")!,
            this.FindControl<AvaPlot>("PreventedWastePlot")!);

        await vm.LoadAsync();
    }
}