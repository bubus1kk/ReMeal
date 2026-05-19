using Application.DTOs.Lots;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReMealApp.ViewModels.Shell;

namespace ReMealApp.ViewModels.Partner
{
    public partial class CreateLotViewModel : ViewModelBase
    {
        private readonly IFoodPointService _foodPointService;
        private readonly ILotService _lotService;
        private readonly HomeViewModel _shell;

        private Guid? _editingLotId;

        [ObservableProperty]
        private string _foodPointName = string.Empty;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _composition = string.Empty;

        [ObservableProperty]
        private decimal _totalQuantity = 1;

        [ObservableProperty]
        private decimal _price;

        [ObservableProperty]
        private DateTimeOffset _pickupDeadline = DateTimeOffset.Now.AddHours(4);

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private bool _isCreateMode = true;

        [ObservableProperty]
        private bool _hasNoFoodPoint;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public CreateLotViewModel(
            IFoodPointService foodPointService,
            ILotService lotService,
            HomeViewModel shell)
        {
            _foodPointService = foodPointService;
            _lotService = lotService;
            _shell = shell;
        }

        public async Task RefreshAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                var foodPoint = await _foodPointService.GetCurrentPartnerFoodPointAsync();
                FoodPointName = foodPoint?.Name ?? string.Empty;
                HasNoFoodPoint = foodPoint is null || !foodPoint.IsActive;
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

        public async Task PrepareCreateAsync()
        {
            _editingLotId = null;
            IsEditMode = false;
            IsCreateMode = true;
            ResetFields();
            await RefreshAsync();
        }

        public async Task LoadForEditAsync(Guid lotId)
        {
            try
            {
                IsBusy = true;

                var entity = await _lotService.GetCurrentPartnerLotAsync(lotId);
                if (entity is null)
                {
                    StatusMessage = "Лот не найден.";
                    return;
                }

                var foodPoint = await _foodPointService.GetCurrentPartnerFoodPointAsync();
                FoodPointName = foodPoint?.Name ?? string.Empty;
                HasNoFoodPoint = foodPoint is null || !foodPoint.IsActive;

                _editingLotId = entity.Id;
                IsEditMode = true;
                IsCreateMode = false;
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

            if (HasNoFoodPoint)
            {
                StatusMessage = "Сначала создайте активную точку питания.";
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

                    StatusMessage = "Лот обновлен.";
                }
                else
                {
                    await _lotService.CreateLotAsync(new CreateLotRequest(
                        Title,
                        Description,
                        Composition,
                        (int)TotalQuantity,
                        Price,
                        pickupUtc));

                    StatusMessage = "Лот создан.";
                }

                ResetFields();
                _editingLotId = null;
                IsEditMode = false;
                IsCreateMode = true;

                await _shell.PartnerLots.LoadAsync();
                _shell.SelectedTabIndex = 3;
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
            IsCreateMode = true;
            StatusMessage = string.Empty;
            await RefreshAsync();
        }

        partial void OnIsEditModeChanged(bool value)
        {
            IsCreateMode = !value;
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
}
