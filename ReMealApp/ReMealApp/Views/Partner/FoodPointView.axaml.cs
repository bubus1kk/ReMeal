using Application.DTOs.Maps;
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

            InlineMapPicker.CoordinatesApplied += InlineMapPicker_CoordinatesApplied;
            InlineMapPicker.Cancelled += InlineMapPicker_Cancelled;
        }

        private async void FindAddressOnMap_Click(object? sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (DataContext is not FoodPointViewModel viewModel ||
                !viewModel.FindAddressOnMapCommand.CanExecute(null))
            {
                return;
            }

            await viewModel.FindAddressOnMapCommand.ExecuteAsync(null);

            if (viewModel is not
                {
                    HasSelectedCoordinates: true,
                    Latitude: double latitude,
                    Longitude: double longitude
                })
            {
                return;
            }

            await ShowInlineMapPickerAsync(latitude, longitude);
        }

        private async Task ShowInlineMapPickerAsync(double latitude, double longitude)
        {
            MapPickerOverlay.IsVisible = true;
            await InlineMapPicker.LoadLocationAsync(latitude, longitude);
        }

        private void InlineMapPicker_CoordinatesApplied(object? sender, CoordinatesDto coordinates)
        {
            if (DataContext is FoodPointViewModel viewModel)
            {
                viewModel.SetSelectedCoordinates(
                    coordinates.Latitude,
                    coordinates.Longitude);
            }

            MapPickerOverlay.IsVisible = false;
        }

        private void InlineMapPicker_Cancelled(object? sender, EventArgs e)
        {
            MapPickerOverlay.IsVisible = false;
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
