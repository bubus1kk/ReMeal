using Application.DTOs.FoodPoints;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using ReMealApp.ViewModels.Shell;
using System.Collections.ObjectModel;

namespace ReMealApp.ViewModels.Partner
{
    public partial class FoodPointViewModel : ViewModelBase
    {
        private readonly IFoodPointService _foodPointService;
        private readonly HomeViewModel _shell;

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

        [ObservableProperty]
        private bool _hasFoodPoints;

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
                await ReloadFoodPointsAsync();
                StatusMessage = FoodPoints.Count == 0
                    ? "У вас еще нет точек питания. Заполните форму и сохраните первую."
                    : $"Загружено точек питания: {FoodPoints.Count}.";
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

                if (SelectedFoodPoint is null)
                {
                    var created = await _foodPointService.CreateFoodPointAsync(new CreateFoodPointRequest(
                        Name,
                        Address,
                        Description,
                        Phone));

                    await ReloadFoodPointsAsync(created.Id);
                    StatusMessage = "Точка питания создана.";
                }
                else
                {
                    var updated = await _foodPointService.UpdateFoodPointAsync(new UpdateFoodPointRequest(
                        SelectedFoodPoint.Id,
                        Name,
                        Address,
                        Description,
                        Phone));

                    await ReloadFoodPointsAsync(updated.Id);
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
            if (SelectedFoodPoint is null || IsBusy)
            {
                StatusMessage = "Сначала выберите точку питания.";
                return;
            }

            try
            {
                IsBusy = true;
                await _foodPointService.DeactivateFoodPointAsync(SelectedFoodPoint.Id);
                await ReloadFoodPointsAsync(SelectedFoodPoint.Id);
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
            if (SelectedFoodPoint is null || IsBusy)
            {
                StatusMessage = "Сначала выберите точку питания.";
                return;
            }

            try
            {
                IsBusy = true;
                await _foodPointService.DeleteFoodPointAsync(SelectedFoodPoint.Id);
                StatusMessage = "Точка питания и связанные с ней лоты удалены.";
                await ReloadFoodPointsAsync();
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
            if (SelectedFoodPoint is null || !SelectedFoodPoint.IsActive)
            {
                StatusMessage = "Сначала выберите активную точку питания.";
                return;
            }

            _shell.OpenCreateLot(SelectedFoodPoint.Id);
        }

        [RelayCommand]
        private void NewFoodPoint()
        {
            SelectedFoodPoint = null;
            ClearForm();
            StatusMessage = "Заполните форму для новой точки питания.";
        }

        private async Task ReloadFoodPointsAsync(Guid? selectedFoodPointId = null)
        {
            var preferredId = selectedFoodPointId ?? SelectedFoodPoint?.Id;
            var foodPoints = await _foodPointService.GetCurrentPartnerFoodPointsAsync();
            FoodPoints = new ObservableCollection<FoodPoint>(foodPoints);
            HasFoodPoints = FoodPoints.Count > 0;
            SelectedFoodPoint = preferredId is Guid id
                ? FoodPoints.FirstOrDefault(x => x.Id == id)
                : FoodPoints.FirstOrDefault();

            ApplyFoodPoint(SelectedFoodPoint);
        }

        private void ApplyFoodPoint(FoodPoint? foodPoint)
        {
            HasFoodPoints = FoodPoints.Count > 0;
            CanCreateLot = foodPoint is { IsActive: true };
            FoodPointStateText = foodPoint is null
                ? "Новая точка"
                : foodPoint.IsActive ? "Активна" : "Деактивирована";

            if (foodPoint is null)
            {
                ClearFormFields();
                return;
            }

            Name = foodPoint.Name;
            Address = foodPoint.Address;
            Description = foodPoint.Description;
            Phone = foodPoint.Phone;
        }

        private void ClearForm()
        {
            CanCreateLot = false;
            FoodPointStateText = "Новая точка";
            ClearFormFields();
        }

        private void ClearFormFields()
        {
            Name = string.Empty;
            Address = string.Empty;
            Description = string.Empty;
            Phone = string.Empty;
        }

        partial void OnSelectedFoodPointChanged(FoodPoint? value)
        {
            ApplyFoodPoint(value);
        }
    }
}
