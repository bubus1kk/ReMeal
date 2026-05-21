using Application.DTOs.Lots;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using ReMealApp.Services;
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
        private string _imagePath = string.Empty;

        [ObservableProperty]
        private ObservableCollection<LotComponentEditorViewModel> _components = new();

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

        public IReadOnlyList<string> AvailableComponentUnits => LotComponent.AllowedUnits;

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
                ImagePath = entity.ImagePath ?? string.Empty;
                Components = CreateComponentEditors(entity.Components
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name));
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
                var componentRequests = BuildComponentRequests();
                if (componentRequests is null)
                    return;

                if (IsEditMode && _editingLotId is Guid lotId)
                {
                    await _lotService.UpdateLotAsync(new UpdateLotRequest(
                        lotId,
                        Title,
                        Description,
                        BuildLotComposition(componentRequests),
                        Price,
                        pickupUtc,
                        componentRequests,
                        ImagePath));

                    StatusMessage = "Лот обновлен.";
                }
                else
                {
                    await _lotService.CreateLotAsync(new CreateLotRequest(
                        SelectedFoodPoint.Id,
                        Title,
                        Description,
                        BuildLotComposition(componentRequests),
                        (int)TotalQuantity,
                        Price,
                        pickupUtc,
                        componentRequests,
                        ImagePath));

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
        private void AddComponent()
        {
            Components.Add(CreateComponentEditor());
        }

        [RelayCommand]
        private void RemoveComponent(LotComponentEditorViewModel? component)
        {
            if (component is null)
                return;

            Components.Remove(component);
        }

        [RelayCommand]
        private void RemoveLotImage()
        {
            ImagePath = string.Empty;
        }

        [RelayCommand]
        private void RemoveComponentImage(LotComponentEditorViewModel? component)
        {
            if (component is null)
                return;

            component.ImagePath = string.Empty;
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
            ImagePath = string.Empty;
            Components = new ObservableCollection<LotComponentEditorViewModel>
            {
                CreateComponentEditor()
            };
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

        private ObservableCollection<LotComponentEditorViewModel> CreateComponentEditors(
            IEnumerable<LotComponent> components)
        {
            return new ObservableCollection<LotComponentEditorViewModel>(
                components.Select(x => CreateComponentEditor(x)));
        }

        private LotComponentEditorViewModel CreateComponentEditor(LotComponent? component = null)
        {
            return new LotComponentEditorViewModel
            {
                Id = component?.Id,
                Name = component?.Name ?? string.Empty,
                Quantity = component?.Quantity ?? 1,
                Unit = component?.Unit ?? LotComponent.DefaultUnit,
                Composition = component?.Composition ?? string.Empty,
                ImagePath = component?.ImagePath ?? string.Empty,
                RemoveImageCommand = RemoveComponentImageCommand,
                RemoveCommand = RemoveComponentCommand
            };
        }

        public async Task SetLotImageFromSourceAsync(string sourcePath)
        {
            ImagePath = await ImageStorageService.SaveImageAsync(sourcePath, "Lots");
        }

        public async Task SetComponentImageFromSourceAsync(
            LotComponentEditorViewModel component,
            string sourcePath)
        {
            component.ImagePath = await ImageStorageService.SaveImageAsync(sourcePath, "LotComponents");
        }

        private IReadOnlyList<LotComponentRequest>? BuildComponentRequests()
        {
            var result = new List<LotComponentRequest>();

            for (var index = 0; index < Components.Count; index++)
            {
                var component = Components[index];
                var hasText = !string.IsNullOrWhiteSpace(component.Name) ||
                    !string.IsNullOrWhiteSpace(component.Composition) ||
                    !string.IsNullOrWhiteSpace(component.ImagePath);

                if (!hasText)
                    continue;

                if (string.IsNullOrWhiteSpace(component.Name))
                {
                    StatusMessage = "У каждого компонента должно быть название.";
                    return null;
                }

                if (component.Quantity <= 0)
                {
                    StatusMessage = "Количество компонента должно быть больше нуля.";
                    return null;
                }

                if (!AvailableComponentUnits.Contains(component.Unit))
                {
                    StatusMessage = "Выберите единицу измерения компонента из списка.";
                    return null;
                }

                result.Add(new LotComponentRequest(
                    component.Id,
                    component.Name,
                    (int)component.Quantity,
                    component.Unit,
                    component.Composition,
                    component.ImagePath,
                    result.Count));
            }

            return result;
        }

        private string BuildLotComposition(IReadOnlyList<LotComponentRequest> components)
        {
            if (components.Count == 0)
                return string.Empty;

            return string.Join("; ", components.Select(x => $"{x.Name.Trim()} — {x.Quantity} {x.Unit}"));
        }
    }

    public partial class LotComponentEditorViewModel : ObservableObject
    {
        public IReadOnlyList<string> AvailableUnits => LotComponent.AllowedUnits;

        [ObservableProperty]
        private Guid? _id;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private decimal _quantity = 1;

        [ObservableProperty]
        private string _unit = LotComponent.DefaultUnit;

        [ObservableProperty]
        private string _composition = string.Empty;

        [ObservableProperty]
        private string _imagePath = string.Empty;

        public IRelayCommand? RemoveImageCommand { get; set; }

        public IRelayCommand? RemoveCommand { get; set; }
    }
}
