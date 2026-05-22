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

        [ObservableProperty]
        private ObservableCollection<FoodLot> _lots = new();

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public CatalogViewModel(
            ILotService lotService,
            IBookingService bookingService)
        {
            _lotService = lotService;
            _bookingService = bookingService;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                var items = await _lotService.GetAvailableLotsAsync();

                Lots = new ObservableCollection<FoodLot>(items);

                StatusMessage = items.Count == 0
                    ? "Сейчас нет доступных наборов."
                    : $"Доступных наборов: {items.Count}";
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
    }
}
