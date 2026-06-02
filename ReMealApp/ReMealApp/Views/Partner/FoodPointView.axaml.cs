using Application.DTOs.Maps;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ReMealApp.ViewModels.Partner;

namespace ReMealApp.Views.Partner
{
    public partial class FoodPointView : UserControl
    {
        private CancellationTokenSource? _mapAddressLookupCancellation;

        public FoodPointView()
        {
            InitializeComponent();

            InlineMapPicker.CoordinatesApplied += InlineMapPicker_CoordinatesApplied;
            InlineMapPicker.CoordinatesSelected += InlineMapPicker_CoordinatesSelected;
            InlineMapPicker.Cancelled += InlineMapPicker_Cancelled;
        }

        private async void FindAddressOnMap_Click(object? sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (DataContext is not FoodPointViewModel viewModel)
            {
                return;
            }

            var coordinates = await viewModel.PrepareMapPickerCoordinatesAsync();

            if (coordinates is null)
            {
                return;
            }

            await ShowInlineMapPickerAsync(
                coordinates.Latitude,
                coordinates.Longitude);
        }

        private async Task ShowInlineMapPickerAsync(double latitude, double longitude)
        {
            CancelPendingMapAddressLookup();
            MapPickerOverlay.IsVisible = true;
            await InlineMapPicker.LoadLocationAsync(latitude, longitude);
        }

        private async void InlineMapPicker_CoordinatesSelected(object? sender, CoordinatesDto coordinates)
        {
            CancelPendingMapAddressLookup();
            _mapAddressLookupCancellation = new CancellationTokenSource();
            var cancellationToken = _mapAddressLookupCancellation.Token;

            try
            {
                if (DataContext is FoodPointViewModel viewModel)
                {
                    await viewModel.SetSelectedCoordinatesFromMapAsync(
                        coordinates.Latitude,
                        coordinates.Longitude,
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async void InlineMapPicker_CoordinatesApplied(object? sender, CoordinatesDto coordinates)
        {
            CancelPendingMapAddressLookup();

            if (DataContext is FoodPointViewModel viewModel)
            {
                await viewModel.SetSelectedCoordinatesFromMapAsync(
                    coordinates.Latitude,
                    coordinates.Longitude);
            }

            MapPickerOverlay.IsVisible = false;
        }

        private void InlineMapPicker_Cancelled(object? sender, EventArgs e)
        {
            CancelPendingMapAddressLookup();
            MapPickerOverlay.IsVisible = false;
        }

        private void CancelPendingMapAddressLookup()
        {
            _mapAddressLookupCancellation?.Cancel();
            _mapAddressLookupCancellation?.Dispose();
            _mapAddressLookupCancellation = null;
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
