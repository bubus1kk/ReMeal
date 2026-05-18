using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReMeal.Application.DTOs.FoodPoints;
using ReMeal.Application.Interfaces;
using ReMeal.Desktop.ViewModels.Base;
using ReMeal.Domain.Entities;
using System.Collections.ObjectModel;

namespace ReMeal.Desktop.ViewModels.Partner;

public partial class FoodPointViewModel : ViewModelBase
{
    private readonly IFoodPointService _foodPointService;
    private readonly NavigationState _navigationState;
    private readonly MainWindowViewModel _shell;

    [ObservableProperty]
    private ObservableCollection<FoodPoint> _foodPoints = new();

    [ObservableProperty]
    private FoodPoint? _selectedFoodPoint;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public FoodPointViewModel(
        IFoodPointService foodPointService,
        NavigationState navigationState,
        MainWindowViewModel shell)
    {
        _foodPointService = foodPointService;
        _navigationState = navigationState;
        _shell = shell;
    }

    partial void OnSelectedFoodPointChanged(FoodPoint? value)
    {
        DeleteCommand.NotifyCanExecuteChanged();
        DeactivateCommand.NotifyCanExecuteChanged();

        if (value is null)
        {
            ClearForm();
            return;
        }

        _navigationState.SelectedFoodPointId = value.Id;
        Name = value.Name;
        Address = value.Address;
        Description = value.Description;
        Phone = value.Phone;
    }

    partial void OnIsBusyChanged(bool value)
    {
        DeleteCommand.NotifyCanExecuteChanged();
        DeactivateCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            var items = await _foodPointService.GetAllFoodPointsAsync();
            FoodPoints = new ObservableCollection<FoodPoint>(items);
            StatusMessage = $"Загружено точек: {items.Count}";
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
    private async Task SaveAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            if (SelectedFoodPoint is null)
            {
                var created = await _foodPointService.CreateFoodPointAsync(new CreateFoodPointRequest(
                    Name,
                    Address,
                    Description,
                    Phone,
                    NavigationState.DemoOwnerId));

                _navigationState.SelectedFoodPointId = created.Id;
                StatusMessage = "Точка питания создана.";
            }
            else
            {
                await _foodPointService.UpdateFoodPointAsync(new UpdateFoodPointRequest(
                    SelectedFoodPoint.Id,
                    Name,
                    Address,
                    Description,
                    Phone));

                StatusMessage = "Точка питания обновлена.";
            }

            await LoadAsync();
            await _shell.CreateLot.RefreshAsync(_navigationState.SelectedFoodPointId);
            ClearForm();
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

    [RelayCommand(CanExecute = nameof(CanModifySelected))]
    private async Task DeactivateAsync()
    {
        if (SelectedFoodPoint is null || IsBusy)
            return;

        try
        {
            IsBusy = true;
            await _foodPointService.DeactivateFoodPointAsync(SelectedFoodPoint.Id);
            StatusMessage = "Точка питания деактивирована.";
            await LoadAsync();
            await _shell.CreateLot.RefreshAsync();
            ClearForm();
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

    [RelayCommand(CanExecute = nameof(CanModifySelected))]
    private async Task DeleteAsync()
    {
        if (SelectedFoodPoint is null || IsBusy)
            return;

        try
        {
            IsBusy = true;

            var lotsCount = SelectedFoodPoint.Lots.Count;
            await _foodPointService.DeleteFoodPointAsync(SelectedFoodPoint.Id);

            StatusMessage = lotsCount > 0
                ? $"Точка питания и связанные лоты ({lotsCount}) удалены."
                : "Точка питания удалена.";

            await LoadAsync();
            await _shell.PartnerLots.LoadAsync();
            await _shell.CreateLot.RefreshAsync();
            ClearForm();
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

    private bool CanModifySelected() => SelectedFoodPoint is not null && !IsBusy;

    [RelayCommand]
    private void NewForm()
    {
        SelectedFoodPoint = null;
        ClearForm();
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void CreateLotForSelected()
    {
        if (_navigationState.SelectedFoodPointId is null)
        {
            StatusMessage = "Выберите точку питания.";
            return;
        }

        _shell.OpenCreateLot(_navigationState.SelectedFoodPointId);
    }

    private void ClearForm()
    {
        Name = string.Empty;
        Address = string.Empty;
        Description = string.Empty;
        Phone = string.Empty;
        _navigationState.SelectedFoodPointId = null;
    }
}
