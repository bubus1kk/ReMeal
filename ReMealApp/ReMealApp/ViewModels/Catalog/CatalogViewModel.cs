using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using ReMealApp.ViewModels;
using System.Collections.ObjectModel;

namespace ReMealApp.ViewModels.Catalog
{
    public partial class CatalogViewModel : ViewModelBase
    {
        private readonly ILotService _lotService;
        private readonly IBookingService _bookingService;
        private Guid? _foodPointFilterId;

        [ObservableProperty]
        private ObservableCollection<FoodLot> _lots = new();

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isFoodPointFilterActive;

        [ObservableProperty]
        private bool _isBusy;

        public CatalogViewModel(
            ILotService lotService,
            IBookingService bookingService)
        {
            _lotService = lotService;
            _bookingService = bookingService;
        }

        public string CatalogTitle => IsFoodPointFilterActive
            ? "Лоты выбранной точки"
            : "Доступные пищевые наборы";

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                var items = await _lotService.GetAvailableLotsAsync();
                if (_foodPointFilterId is Guid foodPointId)
                {
                    items = items
                        .Where(x => x.FoodPointId == foodPointId)
                        .ToList();
                }

                Lots = new ObservableCollection<FoodLot>(items);

                StatusMessage = BuildStatusMessage(items.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = ExceptionMessageFormatter
                    .ToUserMessage(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadAllAsync()
        {
            _foodPointFilterId = null;
            IsFoodPointFilterActive = false;
            await LoadAsync();
        }

        public async Task LoadForFoodPointAsync(Guid foodPointId)
        {
            _foodPointFilterId = foodPointId;
            IsFoodPointFilterActive = true;
            await LoadAsync();
        }

        [RelayCommand]
        private Task ClearFoodPointFilterAsync()
        {
            return LoadAllAsync();
        }

        [RelayCommand]
        private async Task BookLotAsync(Guid lotId)
        {
            try
            {
                await _bookingService.BookLotAsync(
                    lotId,
                    1);

                await LoadAsync();

                StatusMessage = "Бронирование создано.";
            }
            catch (Exception ex)
            {
                StatusMessage = ExceptionMessageFormatter
                    .ToUserMessage(ex);
            }
        }

        partial void OnIsFoodPointFilterActiveChanged(bool value)
        {
            OnPropertyChanged(nameof(CatalogTitle));
        }

        private string BuildStatusMessage(int itemCount)
        {
            if (IsFoodPointFilterActive)
            {
                return itemCount == 0
                    ? "У выбранной точки нет доступных лотов."
                    : $"Доступных лотов выбранной точки: {itemCount}";
            }

            return itemCount == 0
                ? "Сейчас нет доступных наборов."
                : $"Доступных наборов: {itemCount}";
        }
    }
}
