using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReMeal.Application.Interfaces;
using ReMeal.Desktop.ViewModels.Base;
using ReMeal.Domain.Entities;
using System.Collections.ObjectModel;

namespace ReMeal.Desktop.ViewModels.Partner;

public partial class PartnerLotsViewModel : ViewModelBase
{
    private readonly ILotService _lotService;
    private readonly MainWindowViewModel _shell;

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

    public PartnerLotsViewModel(ILotService lotService, MainWindowViewModel shell)
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

            var items = ShowAvailableOnly
                ? await _lotService.GetAvailableLotsAsync()
                : await _lotService.GetAllLotsAsync();

            Lots = new ObservableCollection<FoodLot>(items);
            StatusMessage = $"Лотов: {items.Count}";
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

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedLot is null || IsBusy)
            return;

        try
        {
            IsBusy = true;
            await _lotService.DeleteLotAsync(SelectedLot.Id);
            StatusMessage = "Лот удалён.";
            await LoadAsync();
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
