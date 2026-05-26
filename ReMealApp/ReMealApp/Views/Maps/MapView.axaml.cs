using Avalonia.Controls;
using ReMealApp.ViewModels.Maps;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ReMealApp.Views.Maps
{
    public partial class MapView : UserControl
    {
        private MapViewModel? _viewModel;
        private bool _isLoaded;
        private bool _isUpdatingSelectionFromMap;

        public MapView()
        {
            InitializeComponent();

            Loaded += async (_, _) =>
            {
                _isLoaded = true;
                await RefreshMapAsync(resetPoints: true);
            };

            DetachedFromVisualTree += (_, _) =>
            {
                _isLoaded = false;
                UnsubscribeFromViewModel();
            };

            FoodPointMap.FoodPointSelected += FoodPointMap_FoodPointSelected;
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            UnsubscribeFromViewModel();

            _viewModel = DataContext as MapViewModel;
            if (_viewModel is null)
                return;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _viewModel.FoodPoints.CollectionChanged += FoodPoints_CollectionChanged;
            _ = RefreshMapAsync(resetPoints: true);
        }

        private async void FoodPointMap_FoodPointSelected(object? sender, MapFoodPointItemViewModel e)
        {
            if (_viewModel is null)
                return;

            _isUpdatingSelectionFromMap = true;
            try
            {
                _viewModel.SelectFoodPoint(e);
                await FoodPointMap.SelectFoodPointAsync(e);
            }
            finally
            {
                _isUpdatingSelectionFromMap = false;
            }
        }

        private async void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_viewModel is null)
                return;

            if (e.PropertyName == nameof(MapViewModel.FoodPoints))
            {
                _viewModel.FoodPoints.CollectionChanged += FoodPoints_CollectionChanged;
                await RefreshMapAsync(resetPoints: true);
                return;
            }

            if (e.PropertyName == nameof(MapViewModel.SelectedFoodPoint) &&
                !_isUpdatingSelectionFromMap)
            {
                await FoodPointMap.SelectFoodPointAsync(_viewModel.SelectedFoodPoint);
            }
        }

        private async void FoodPoints_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            await RefreshMapAsync(resetPoints: true);
        }

        private async Task RefreshMapAsync(bool resetPoints)
        {
            if (!_isLoaded || _viewModel is null || !resetPoints || _viewModel.FoodPoints.Count == 0)
                return;

            await FoodPointMap.SetFoodPointsAsync(
                _viewModel.FoodPoints.ToList(),
                _viewModel.SelectedFoodPoint);
        }

        private void UnsubscribeFromViewModel()
        {
            if (_viewModel is null)
                return;

            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel.FoodPoints.CollectionChanged -= FoodPoints_CollectionChanged;
            _viewModel = null;
        }
    }
}
