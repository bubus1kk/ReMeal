using Application.DTOs.FoodPoints;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using ReMealApp.ViewModels.Shell;

namespace ReMealApp.ViewModels.Partner
{
    public partial class FoodPointViewModel : ViewModelBase
    {
        private readonly IFoodPointService _foodPointService;
        private readonly HomeViewModel _shell;

        [ObservableProperty]
        private FoodPoint? _currentFoodPoint;

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

        [ObservableProperty]
        private bool _hasFoodPoint;

        [ObservableProperty]
        private bool _canCreateLot;

        [ObservableProperty]
        private string _foodPointStateText = "Не создана";

        public FoodPointViewModel(IFoodPointService foodPointService, HomeViewModel shell)
        {
            _foodPointService = foodPointService;
            _shell = shell;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                CurrentFoodPoint = await _foodPointService.GetCurrentPartnerFoodPointAsync();
                ApplyFoodPoint(CurrentFoodPoint);
                StatusMessage = CurrentFoodPoint is null
                    ? "У вас еще нет точки питания. Заполните форму и сохраните ее."
                    : "Загружена ваша точка питания.";
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

            try
            {
                IsBusy = true;

                if (CurrentFoodPoint is null)
                {
                    await _foodPointService.CreateFoodPointAsync(new CreateFoodPointRequest(
                        Name,
                        Address,
                        Description,
                        Phone));

                    StatusMessage = "Точка питания создана.";
                }
                else
                {
                    await _foodPointService.UpdateFoodPointAsync(new UpdateFoodPointRequest(
                        CurrentFoodPoint.Id,
                        Name,
                        Address,
                        Description,
                        Phone));

                    StatusMessage = "Точка питания обновлена.";
                }

                await _shell.RefreshPartnerAsync();
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
        private async Task DeactivateAsync()
        {
            if (CurrentFoodPoint is null || IsBusy)
            {
                StatusMessage = "Сначала создайте точку питания.";
                return;
            }

            try
            {
                IsBusy = true;
                await _foodPointService.DeactivateFoodPointAsync(CurrentFoodPoint.Id);
                StatusMessage = "Точка питания деактивирована.";
                await _shell.RefreshPartnerAsync();
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
            if (CurrentFoodPoint is null || IsBusy)
            {
                StatusMessage = "Сначала создайте точку питания.";
                return;
            }

            try
            {
                IsBusy = true;
                await _foodPointService.DeleteFoodPointAsync(CurrentFoodPoint.Id);
                StatusMessage = "Точка питания и связанные с ней лоты удалены.";
                ClearForm();
                await _shell.RefreshPartnerAsync();
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
        private void CreateLot()
        {
            if (CurrentFoodPoint is null || !CurrentFoodPoint.IsActive)
            {
                StatusMessage = "Сначала создайте активную точку питания.";
                return;
            }

            _shell.OpenCreateLot();
        }

        private void ApplyFoodPoint(FoodPoint? foodPoint)
        {
            HasFoodPoint = foodPoint is not null;
            CanCreateLot = foodPoint is { IsActive: true };
            FoodPointStateText = foodPoint is null
                ? "Не создана"
                : foodPoint.IsActive ? "Активна" : "Деактивирована";

            if (foodPoint is null)
            {
                ClearForm();
                return;
            }

            Name = foodPoint.Name;
            Address = foodPoint.Address;
            Description = foodPoint.Description;
            Phone = foodPoint.Phone;
        }

        private void ClearForm()
        {
            CurrentFoodPoint = null;
            HasFoodPoint = false;
            CanCreateLot = false;
            FoodPointStateText = "Не создана";
            Name = string.Empty;
            Address = string.Empty;
            Description = string.Empty;
            Phone = string.Empty;
        }
    }
}
