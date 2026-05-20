using Application.DTOs.Lots;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using ReMealApp.ViewModels.Shell;
using System.Collections.ObjectModel;

namespace ReMealApp.ViewModels.Partner
{
    public partial class CreateLotViewModel : ViewModelBase
    {
        private readonly IFoodPointService _foodPointService;
        private readonly ILotService _lotService;
        private readonly HomeViewModel _shell;

        private Guid? _editingLotId;

        [ObservableProperty]
        private ObservableCollection<FoodPoint> _foodPoints = new();

        [ObservableProperty]
        private FoodPoint? _selectedFoodPoint;

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
        private TimeSpan _pickupTime = DateTimeOffset.Now.AddHours(4).TimeOfDay;

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
                await ReloadFoodPointsAsync(SelectedFoodPoint?.Id);
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

        public async Task PrepareCreateAsync(Guid? foodPointId = null)
        {
            _editingLotId = null;
            IsEditMode = false;
            IsCreateMode = true;
            ResetFields();
            await ReloadFoodPointsAsync(foodPointId);
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

                await ReloadFoodPointsAsync(entity.FoodPointId);

                _editingLotId = entity.Id;
                IsEditMode = true;
                IsCreateMode = false;
                Title = entity.Title;
                Description = entity.Description;
                Composition = entity.Composition;
                TotalQuantity = entity.TotalQuantity;
                Price = entity.Price;
                var localDeadline = new DateTimeOffset(entity.PickupDeadline.ToLocalTime());
                PickupDeadline = localDeadline;
                PickupTime = localDeadline.TimeOfDay;
                StatusMessage = "Режим редактирования.";
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
        private async Task SaveAsync()
        {
            if (IsBusy)
                return;

            if (SelectedFoodPoint is not { IsActive: true })
            {
                StatusMessage = "Сначала выберите активную точку питания.";
                return;
            }

            try
            {
                IsBusy = true;
                var pickupUtc = BuildPickupDeadlineUtc();

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
                        SelectedFoodPoint.Id,
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
                await _shell.OpenPartnerLotsAsync();
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

        partial void OnSelectedFoodPointChanged(FoodPoint? value)
        {
            HasNoFoodPoint = value is not { IsActive: true };
        }

        private async Task ReloadFoodPointsAsync(Guid? selectedFoodPointId)
        {
            var foodPoints = await _foodPointService.GetCurrentPartnerFoodPointsAsync();
            FoodPoints = new ObservableCollection<FoodPoint>(foodPoints);
            SelectedFoodPoint = selectedFoodPointId is Guid id
                ? FoodPoints.FirstOrDefault(x => x.Id == id) ?? FoodPoints.FirstOrDefault(x => x.IsActive) ?? FoodPoints.FirstOrDefault()
                : FoodPoints.FirstOrDefault(x => x.IsActive) ?? FoodPoints.FirstOrDefault();
            HasNoFoodPoint = SelectedFoodPoint is not { IsActive: true };
        }

        private void ResetFields()
        {
            var defaultDeadline = DateTimeOffset.Now.AddHours(4);

            Title = string.Empty;
            Description = string.Empty;
            Composition = string.Empty;
            TotalQuantity = 1;
            Price = 0;
            PickupDeadline = defaultDeadline;
            PickupTime = defaultDeadline.TimeOfDay;
        }

        private DateTime BuildPickupDeadlineUtc()
        {
            var localDeadline = PickupDeadline.Date + PickupTime;
            var offset = TimeZoneInfo.Local.GetUtcOffset(localDeadline);
            return new DateTimeOffset(localDeadline, offset).UtcDateTime;
        }
    }
}
