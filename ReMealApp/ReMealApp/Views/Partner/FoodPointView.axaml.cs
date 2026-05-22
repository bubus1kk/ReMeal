using Avalonia.Controls;
using Avalonia.Interactivity;
using ReMealApp.ViewModels.Partner;

namespace ReMealApp.Views.Partner
{
    public partial class FoodPointView : UserControl
    {
        public FoodPointView()
        {
            InitializeComponent();
        }

        private async void FoodPointRow_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not FoodPointViewModel viewModel ||
                sender is not Control { Tag: FoodPointListItemViewModel foodPoint })
            {
                return;
            }

            if (viewModel.OpenDetailsCommand.CanExecute(foodPoint))
                await viewModel.OpenDetailsCommand.ExecuteAsync(foodPoint);
        }

        private void CreateLot_Click(object? sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (DataContext is not FoodPointViewModel viewModel ||
                sender is not Control { Tag: FoodPointListItemViewModel foodPoint })
            {
                return;
            }

            if (viewModel.CreateLotForItemCommand.CanExecute(foodPoint))
                viewModel.CreateLotForItemCommand.Execute(foodPoint);
        }

        private void OpenLot_Click(object? sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (DataContext is not FoodPointViewModel viewModel ||
                sender is not Control { Tag: FoodPointLotItemViewModel lot })
            {
                return;
            }

            if (viewModel.OpenLotCommand.CanExecute(lot))
                viewModel.OpenLotCommand.Execute(lot);
        }
    }
}
