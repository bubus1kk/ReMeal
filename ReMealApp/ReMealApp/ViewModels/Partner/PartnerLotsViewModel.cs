using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Domain.Enums;
using ReMealApp.ViewModels.Shell;
using System.Collections.ObjectModel;

namespace ReMealApp.ViewModels.Partner
{
    public partial class PartnerLotsViewModel : ViewModelBase
    {
        private readonly ILotService _lotService;
        private readonly HomeViewModel _shell;

        [ObservableProperty]
        private ObservableCollection<FoodLot> _lots = new();

        [ObservableProperty]
        private FoodLot? _selectedLot;

        [ObservableProperty]
        private bool _showAvailableOnly;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public PartnerLotsViewModel(ILotService lotService, HomeViewModel shell)
        {
            _lotService = lotService;
            _shell = shell;
        }

        partial void OnShowAvailableOnlyChanged(bool value) => _ = LoadAsync();

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await _lotService.MarkExpiredLotsAsync();

                var items = await _lotService.GetCurrentPartnerLotsAsync();
                if (ShowAvailableOnly)
                {
                    var now = DateTime.UtcNow;
                    items = items
                        .Where(x => x.Status == LotStatus.Active && x.AvailableQuantity > 0 && x.PickupDeadline > now)
                        .ToList();
                }

                Lots = new ObservableCollection<FoodLot>(items);
                StatusMessage = $"Ваших лотов: {items.Count}";
            }
            catch (Exception ex)
            {
                StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            if (SelectedLot is null || IsBusy)
            {
                StatusMessage = "Выберите лот.";
                return;
            }

            try
            {
                IsBusy = true;
                await _lotService.DeleteLotAsync(SelectedLot.Id);
                StatusMessage = "Лот удален.";
                await LoadAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void EditSelected()
        {
            if (SelectedLot is null)
            {
                StatusMessage = "Выберите лот для редактирования.";
                return;
            }

            _shell.OpenLotEditor(SelectedLot.Id);
        }

        [RelayCommand]
        private void CreateNew()
        {
            _shell.OpenCreateLot();
        }
    }
}
