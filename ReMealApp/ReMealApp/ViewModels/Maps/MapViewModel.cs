using Application.DTOs.Maps;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReMealApp.ViewModels;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ReMealApp.ViewModels.Maps
{
    public partial class MapViewModel : ViewModelBase
    {
        private readonly IMapService _mapService;

        [ObservableProperty]
        private ObservableCollection<MapFoodPointItemViewModel> _foodPoints = new();

        [ObservableProperty]
        private ObservableCollection<NearestFoodPointItemViewModel> _nearestFoodPoints = new();

        [ObservableProperty]
        private MapFoodPointItemViewModel? _selectedFoodPoint;

        [ObservableProperty]
        private string _addressQuery = string.Empty;

        [ObservableProperty]
        private bool _searchPerformed;

        [ObservableProperty]
        private string _nearestEmptyStateText = "Нет доступных точек рядом";

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public MapViewModel(IMapService mapService)
        {
            _mapService = mapService;
        }

        public bool HasFoodPoints => FoodPoints.Count > 0;

        public bool HasNoFoodPoints => !HasFoodPoints && !IsBusy;

        public bool HasSelectedFoodPoint => SelectedFoodPoint is not null;

        public bool HasNearestFoodPoints => NearestFoodPoints.Count > 0;

        public bool HasNoNearestFoodPoints => SearchPerformed && !HasNearestFoodPoints && !IsBusy;

        public bool IsInitialPointListVisible => !SearchPerformed;

        public bool CanSearchNearest => !IsBusy;

        public string EmptyStateText => "Нет точек питания с координатами";

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Загружаем точки питания для карты...";

                var points = await _mapService.GetFoodPointsForMapAsync();
                FoodPoints = new ObservableCollection<MapFoodPointItemViewModel>(
                    points.Select(x => new MapFoodPointItemViewModel(x)));

                SelectedFoodPoint = FoodPoints.FirstOrDefault();
                StatusMessage = FoodPoints.Count == 0
                    ? EmptyStateText
                    : $"Точек на карте: {FoodPoints.Count}";
            }
            catch (Exception ex)
            {
                StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
                FoodPoints = new ObservableCollection<MapFoodPointItemViewModel>();
                SelectedFoodPoint = null;
            }
            finally
            {
                IsBusy = false;
                NotifyStateChanged();
            }
        }

        [RelayCommand]
        public async Task FindNearestAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(AddressQuery))
            {
                SearchPerformed = true;
                NearestFoodPoints.Clear();
                NearestEmptyStateText = "Введите адрес";
                StatusMessage = "Введите адрес для поиска ближайших точек.";
                NotifyNearestStateChanged();
                return;
            }

            try
            {
                IsBusy = true;
                SearchPerformed = true;
                NearestFoodPoints.Clear();
                NearestEmptyStateText = "Нет доступных точек рядом";
                StatusMessage = "Ищем ближайшие точки...";

                var nearestPoints = await _mapService.FindNearestFoodPointsAsync(
                    AddressQuery,
                    maxResults: 5);

                NearestFoodPoints = new ObservableCollection<NearestFoodPointItemViewModel>(
                    nearestPoints.Select(x => new NearestFoodPointItemViewModel(x)));

                if (NearestFoodPoints.Count == 0)
                {
                    NearestEmptyStateText = FoodPoints.Count == 0
                        ? "Нет доступных точек рядом"
                        : "Адрес не найден";
                    StatusMessage = NearestEmptyStateText;
                    return;
                }

                var firstNearest = NearestFoodPoints.First();
                ShowOnMap(firstNearest);
                StatusMessage = $"Найдено ближайших точек: {NearestFoodPoints.Count}";
            }
            catch (Exception ex)
            {
                NearestEmptyStateText = "Адрес не найден";
                StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
            finally
            {
                IsBusy = false;
                NotifyNearestStateChanged();
            }
        }

        [RelayCommand]
        public void SelectFoodPoint(MapFoodPointItemViewModel? foodPoint)
        {
            if (foodPoint is null)
                return;

            SelectedFoodPoint = foodPoint;
            StatusMessage = $"Выбрана точка: {foodPoint.Name}";
        }

        [RelayCommand]
        public void ShowOnMap(NearestFoodPointItemViewModel? foodPoint)
        {
            if (foodPoint is null)
                return;

            SelectedFoodPoint = FoodPoints.FirstOrDefault(x => x.Id == foodPoint.Id)
                ?? foodPoint.ToMapFoodPointItem();
            StatusMessage = $"Точка показана на карте: {foodPoint.Name}";
        }

        [RelayCommand]
        public void OpenLots(NearestFoodPointItemViewModel? foodPoint)
        {
            if (foodPoint is null)
                return;

            ShowOnMap(foodPoint);
            StatusMessage = "Переход к лотам выбранной точки будет добавлен на следующем этапе.";
        }

        partial void OnFoodPointsChanged(ObservableCollection<MapFoodPointItemViewModel> value)
        {
            NotifyStateChanged();
        }

        partial void OnNearestFoodPointsChanged(ObservableCollection<NearestFoodPointItemViewModel> value)
        {
            NotifyNearestStateChanged();
        }

        partial void OnSelectedFoodPointChanged(MapFoodPointItemViewModel? value)
        {
            OnPropertyChanged(nameof(HasSelectedFoodPoint));
        }

        partial void OnSearchPerformedChanged(bool value)
        {
            NotifyNearestStateChanged();
        }

        partial void OnIsBusyChanged(bool value)
        {
            NotifyStateChanged();
            NotifyNearestStateChanged();
            FindNearestCommand.NotifyCanExecuteChanged();
        }

        private void NotifyStateChanged()
        {
            OnPropertyChanged(nameof(HasFoodPoints));
            OnPropertyChanged(nameof(HasNoFoodPoints));
            OnPropertyChanged(nameof(EmptyStateText));
        }

        private void NotifyNearestStateChanged()
        {
            OnPropertyChanged(nameof(HasNearestFoodPoints));
            OnPropertyChanged(nameof(HasNoNearestFoodPoints));
            OnPropertyChanged(nameof(IsInitialPointListVisible));
            OnPropertyChanged(nameof(CanSearchNearest));
        }
    }

    public sealed class MapFoodPointItemViewModel
    {
        public MapFoodPointItemViewModel(MapFoodPointDto foodPoint)
        {
            Id = foodPoint.Id;
            Name = foodPoint.Name;
            Address = foodPoint.Address;
            Coordinates = foodPoint.Coordinates;
            AvailableLotCount = foodPoint.AvailableLotCount;
        }

        public Guid Id { get; }

        public string Name { get; }

        public string Address { get; }

        public CoordinatesDto Coordinates { get; }

        public int AvailableLotCount { get; }

        public string CoordinatesText => string.Create(
            CultureInfo.InvariantCulture,
            $"Latitude: {Coordinates.Latitude:F6}; Longitude: {Coordinates.Longitude:F6}");

        public string AvailableLotsText => AvailableLotCount.ToString(CultureInfo.InvariantCulture);

        public string AvailableLotsDisplayText => $"Доступно лотов: {AvailableLotCount}";
    }

    public sealed class NearestFoodPointItemViewModel
    {
        public NearestFoodPointItemViewModel(NearestFoodPointDto foodPoint)
        {
            Id = foodPoint.Id;
            Name = foodPoint.Name;
            Address = foodPoint.Address;
            Coordinates = foodPoint.Coordinates;
            DistanceKm = foodPoint.DistanceKm;
            AvailableLotCount = foodPoint.AvailableLotCount;
        }

        public Guid Id { get; }

        public string Name { get; }

        public string Address { get; }

        public CoordinatesDto Coordinates { get; }

        public double DistanceKm { get; }

        public int AvailableLotCount { get; }

        public string DistanceText => DistanceKm < 1
            ? $"{DistanceKm * 1000:N0} м"
            : $"{DistanceKm:N1} км";

        public string AvailableLotsDisplayText => $"Доступно лотов: {AvailableLotCount}";

        public MapFoodPointItemViewModel ToMapFoodPointItem()
        {
            return new MapFoodPointItemViewModel(
                new MapFoodPointDto(
                    Id,
                    Name,
                    Address,
                    Coordinates,
                    AvailableLotCount));
        }
    }
}
