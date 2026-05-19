using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReMeal.Application.DTOs.Lots;
using ReMeal.Application.Interfaces;
using ReMeal.Desktop.ViewModels.Base;
using ReMeal.Domain.Entities;
using System.Collections.ObjectModel;

namespace ReMeal.Desktop.ViewModels.Partner;

public partial class CreateLotViewModel : ViewModelBase
{
    private readonly IFoodPointService _foodPointService;
    private readonly ILotService _lotService;
    private readonly NavigationState _navigationState;
    private readonly MainWindowViewModel _shell;

    private Guid? _editingLotId;

    [ObservableProperty]
    private ObservableCollection<FoodPoint> _foodPoints = new();

    [ObservableProperty]
    private Guid? _selectedFoodPointId;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _composition = string.Empty;

    [ObservableProperty]
    private int _totalQuantity = 1;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private DateTimeOffset _pickupDeadline = DateTimeOffset.Now.AddHours(4);

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _hasNoFoodPoints;

    public CreateLotViewModel(
        IFoodPointService foodPointService,
        ILotService lotService,
        NavigationState navigationState,
        MainWindowViewModel shell)
    {
        _foodPointService = foodPointService;
        _lotService = lotService;
        _navigationState = navigationState;
        _shell = shell;
    }

    public async Task RefreshAsync(Guid? selectFoodPointId = null)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            var points = await _foodPointService.GetAllFoodPointsAsync();
            var activePoints = points.Where(x => x.IsActive).ToList();

            FoodPoints = new ObservableCollection<FoodPoint>(activePoints);
            HasNoFoodPoints = activePoints.Count == 0;

            var targetId = selectFoodPointId
                ?? _navigationState.SelectedFoodPointId
                ?? SelectedFoodPointId;

            if (targetId is Guid id && FoodPoints.Any(x => x.Id == id))
                SelectedFoodPointId = id;
            else if (FoodPoints.Count > 0 && SelectedFoodPointId is null)
                SelectedFoodPointId = FoodPoints[0].Id;
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

    public async Task PrepareCreateAsync(Guid? foodPointId = null)
    {
        _editingLotId = null;
        IsEditMode = false;
        ResetFields();
        await RefreshAsync(foodPointId);
    }

    public async Task LoadForEditAsync(Guid lotId)
    {
        try
        {
            IsBusy = true;

            var entity = await _lotService.GetLotAsync(lotId);
            if (entity is null)
            {
                StatusMessage = "Лот не найден.";
                return;
            }

            await RefreshAsync(entity.FoodPointId);

            _editingLotId = entity.Id;
            IsEditMode = true;
            SelectedFoodPointId = entity.FoodPointId;
            Title = entity.Title;
            Description = entity.Description;
            Composition = entity.Composition;
            TotalQuantity = entity.TotalQuantity;
            Price = entity.Price;
            PickupDeadline = new DateTimeOffset(entity.PickupDeadline.ToLocalTime());
            StatusMessage = "Режим редактирования.";
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

        if (SelectedFoodPointId is not Guid foodPointId)
        {
            StatusMessage = HasNoFoodPoints
                ? "Сначала создайте активную точку питания на вкладке «Точки питания»."
                : "Выберите точку питания.";
            return;
        }

        try
        {
            IsBusy = true;
            var pickupUtc = PickupDeadline.UtcDateTime;

            if (IsEditMode && _editingLotId is Guid lotId)
            {
                await _lotService.UpdateLotAsync(new UpdateLotRequest(
                    lotId,
                    Title,
                    Description,
                    Composition,
                    Price,
                    pickupUtc));

                StatusMessage = "Лот обновлён.";
            }
            else
            {
                await _lotService.CreateLotAsync(new CreateLotRequest(
                    foodPointId,
                    Title,
                    Description,
                    Composition,
                    TotalQuantity,
                    Price,
                    pickupUtc));

                StatusMessage = "Лот создан.";
            }

            ResetFields();
            _editingLotId = null;
            IsEditMode = false;
            SelectedFoodPointId = foodPointId;

            await _shell.PartnerLots.LoadAsync();
            _shell.SelectedTabIndex = 1;
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
    private async Task ResetForm()
    {
        ResetFields();
        _editingLotId = null;
        IsEditMode = false;
        StatusMessage = string.Empty;
        await RefreshAsync();
    }

    private void ResetFields()
    {
        Title = string.Empty;
        Description = string.Empty;
        Composition = string.Empty;
        TotalQuantity = 1;
        Price = 0;
        PickupDeadline = DateTimeOffset.Now.AddHours(4);
    }
}
