using Avalonia.Controls;
using Avalonia.Interactivity;
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

    private async void OnLoaded(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not PartnerAnalyticsViewModel vm)
            return;

        vm.AttachPlots(
            this.FindControl<AvaPlot>("SavedPortionsChart")!,
            this.FindControl<AvaPlot>("StatusesChart")!,
            this.FindControl<AvaPlot>("LotsChart")!,
            this.FindControl<AvaPlot>("FoodPointsChart")!,
            this.FindControl<AvaPlot>("WasteChart")!);

        await vm.LoadAsync();
    }
}