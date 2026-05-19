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

        [ObservableProperty]
        private ObservableCollection<FoodLot> _lots = new();

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public CatalogViewModel(ILotService lotService)
        {
            _lotService = lotService;
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
                StatusMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
