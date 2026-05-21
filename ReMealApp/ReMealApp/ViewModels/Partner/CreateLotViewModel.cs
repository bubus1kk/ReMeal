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
        private const int TitleMaxLength = 200;
        private const int DescriptionMaxLength = 2000;
        private const int ComponentNameMaxLength = 200;
        private const int ComponentCompositionMaxLength = 2000;

        private readonly IFoodPointService _foodPointService;
        private readonly ILotService _lotService;
        private readonly HomeViewModel _shell;

        private Guid? _editingLotId;
        private LotFormSnapshot? _originalSnapshot;

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
        private DateTimeOffset? _pickupDeadline;

        [ObservableProperty]
        private TimeSpan? _pickupTime;

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

        [ObservableProperty]
        private string _foodPointError = string.Empty;

        [ObservableProperty]
        private string _titleError = string.Empty;

        [ObservableProperty]
        private string _descriptionError = string.Empty;

        [ObservableProperty]
        private string _totalQuantityError = string.Empty;

        [ObservableProperty]
        private string _priceError = string.Empty;

        [ObservableProperty]
        private string _pickupDeadlineError = string.Empty;

        [ObservableProperty]
        private string _componentsError = string.Empty;

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

        public string FormTitle => IsEditMode ? "Редактирование лота" : "Создание лота";

        public string FormSubtitle => "Заполните информацию о лоте и его составе";

        public string ResetButtonText => IsEditMode ? "Сбросить" : "Сбросить";

        public bool HasFoodPoints => FoodPoints.Count > 0;

        public bool HasComponents => Components.Count > 0;

        public bool HasNoComponents => !HasComponents;

        public bool HasLotImage => !string.IsNullOrWhiteSpace(ImagePath);

        public bool CanSave => !IsBusy && SelectedFoodPoint is { IsActive: true };

        public bool IsFoodPointSelectionEnabled => IsCreateMode && HasFoodPoints;

        public bool IsTotalQuantityEditable => IsCreateMode;

        public string TitleCounterText => $"{Title.Length}/{TitleMaxLength}";

        public string DescriptionCounterText => $"{Description.Length}/{DescriptionMaxLength}";

        public DateTime? PickupDate
        {
            get => PickupDeadline?.DateTime.Date;
            set
            {
                if (value is null)
                {
                    PickupDeadline = null;
                    return;
                }

                var localDate = value.Value.Date;
                PickupDeadline = new DateTimeOffset(localDate, TimeZoneInfo.Local.GetUtcOffset(localDate));
            }
        }

        public bool HasPickupTime => PickupTime.HasValue;

        public bool HasPickupDate => PickupDeadline.HasValue;

        public string PickupDateDisplayText => PickupDeadline?.ToString("dd.MM.yyyy") ?? string.Empty;

        public string PickupTimeDisplayText => PickupTime?.ToString(@"hh\:mm") ?? string.Empty;

        public bool HasFoodPointError => !string.IsNullOrWhiteSpace(FoodPointError);

        public bool HasTitleError => !string.IsNullOrWhiteSpace(TitleError);

        public bool HasDescriptionError => !string.IsNullOrWhiteSpace(DescriptionError);

        public bool HasTotalQuantityError => !string.IsNullOrWhiteSpace(TotalQuantityError);

        public bool HasPriceError => !string.IsNullOrWhiteSpace(PriceError);

        public bool HasPickupDeadlineError => !string.IsNullOrWhiteSpace(PickupDeadlineError);

        public bool HasComponentsError => !string.IsNullOrWhiteSpace(ComponentsError);

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
            ResetCreateFields();
            ClearValidation();

            await ReloadFoodPointsAsync(foodPointId);
            _originalSnapshot = CaptureSnapshot();
        }

        public async Task LoadForEditAsync(Guid lotId)
        {
            try
            {
                IsBusy = true;
                ClearValidation();

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
                StatusMessage = string.Empty;
                _originalSnapshot = CaptureSnapshot();
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

            ClearValidation();

            if (!ValidateForm())
                return;

            try
            {
                IsBusy = true;
                var pickupUtc = BuildPickupDeadlineUtc();
                var componentRequests = BuildComponentRequests();

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
                        SelectedFoodPoint!.Id,
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

                ResetCreateFields();
                _editingLotId = null;
                _originalSnapshot = null;
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
            ComponentsError = string.Empty;
            RefreshComponentState();
        }

        [RelayCommand]
        private void RemoveComponent(LotComponentEditorViewModel? component)
        {
            if (component is null)
                return;

            Components.Remove(component);
            ComponentsError = string.Empty;
            RefreshComponentState();
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
            ClearValidation();
            StatusMessage = string.Empty;

            if (IsEditMode)
            {
                if (_originalSnapshot is not null)
                {
                    ApplySnapshot(_originalSnapshot);
                    return;
                }

                if (_editingLotId is Guid lotId)
                    await LoadForEditAsync(lotId);

                return;
            }

            if (_originalSnapshot is not null)
            {
                ApplySnapshot(_originalSnapshot);
                return;
            }

            ResetCreateFields();
            await ReloadFoodPointsAsync(null);
            _originalSnapshot = CaptureSnapshot();
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

        partial void OnFoodPointsChanged(ObservableCollection<FoodPoint> value)
        {
            OnPropertyChanged(nameof(HasFoodPoints));
            OnPropertyChanged(nameof(IsFoodPointSelectionEnabled));
        }

        partial void OnSelectedFoodPointChanged(FoodPoint? value)
        {
            HasNoFoodPoint = value is not { IsActive: true };
            FoodPointError = string.Empty;
            OnPropertyChanged(nameof(CanSave));
        }

        partial void OnTitleChanged(string value)
        {
            TitleError = string.Empty;
            OnPropertyChanged(nameof(TitleCounterText));
        }

        partial void OnDescriptionChanged(string value)
        {
            DescriptionError = string.Empty;
            OnPropertyChanged(nameof(DescriptionCounterText));
        }

        partial void OnImagePathChanged(string value)
        {
            OnPropertyChanged(nameof(HasLotImage));
        }

        partial void OnComponentsChanged(ObservableCollection<LotComponentEditorViewModel> value)
        {
            RefreshComponentState();
        }

        partial void OnTotalQuantityChanged(decimal value)
        {
            TotalQuantityError = string.Empty;
        }

        partial void OnPriceChanged(decimal value)
        {
            PriceError = string.Empty;
        }

        partial void OnPickupDeadlineChanged(DateTimeOffset? value)
        {
            PickupDeadlineError = string.Empty;
            OnPropertyChanged(nameof(PickupDate));
            OnPropertyChanged(nameof(HasPickupDate));
            OnPropertyChanged(nameof(PickupDateDisplayText));
        }

        partial void OnPickupTimeChanged(TimeSpan? value)
        {
            PickupDeadlineError = string.Empty;
            OnPropertyChanged(nameof(HasPickupTime));
            OnPropertyChanged(nameof(PickupTimeDisplayText));
        }

        partial void OnIsEditModeChanged(bool value)
        {
            IsCreateMode = !value;
            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(ResetButtonText));
            OnPropertyChanged(nameof(IsFoodPointSelectionEnabled));
            OnPropertyChanged(nameof(IsTotalQuantityEditable));
        }

        partial void OnIsCreateModeChanged(bool value)
        {
            OnPropertyChanged(nameof(IsFoodPointSelectionEnabled));
            OnPropertyChanged(nameof(IsTotalQuantityEditable));
        }

        partial void OnHasNoFoodPointChanged(bool value)
        {
            OnPropertyChanged(nameof(CanSave));
        }

        partial void OnIsBusyChanged(bool value)
        {
            OnPropertyChanged(nameof(CanSave));
        }

        partial void OnFoodPointErrorChanged(string value) => OnPropertyChanged(nameof(HasFoodPointError));

        partial void OnTitleErrorChanged(string value) => OnPropertyChanged(nameof(HasTitleError));

        partial void OnDescriptionErrorChanged(string value) => OnPropertyChanged(nameof(HasDescriptionError));

        partial void OnTotalQuantityErrorChanged(string value) => OnPropertyChanged(nameof(HasTotalQuantityError));

        partial void OnPriceErrorChanged(string value) => OnPropertyChanged(nameof(HasPriceError));

        partial void OnPickupDeadlineErrorChanged(string value) => OnPropertyChanged(nameof(HasPickupDeadlineError));

        partial void OnComponentsErrorChanged(string value) => OnPropertyChanged(nameof(HasComponentsError));

        private async Task ReloadFoodPointsAsync(Guid? selectedFoodPointId)
        {
            var foodPoints = await _foodPointService.GetCurrentPartnerFoodPointsAsync();
            FoodPoints = new ObservableCollection<FoodPoint>(foodPoints);
            SelectedFoodPoint = selectedFoodPointId is Guid id
                ? FoodPoints.FirstOrDefault(x => x.Id == id) ?? FoodPoints.FirstOrDefault(x => x.IsActive) ?? FoodPoints.FirstOrDefault()
                : FoodPoints.FirstOrDefault(x => x.IsActive) ?? FoodPoints.FirstOrDefault();
            HasNoFoodPoint = SelectedFoodPoint is not { IsActive: true };
        }

        private void ResetCreateFields()
        {
            Title = string.Empty;
            Description = string.Empty;
            Composition = string.Empty;
            ImagePath = string.Empty;
            Components = new ObservableCollection<LotComponentEditorViewModel>();
            TotalQuantity = 1;
            Price = 0;
            PickupDeadline = null;
            PickupTime = null;
        }

        private bool ValidateForm()
        {
            var isValid = true;

            if (SelectedFoodPoint is not { IsActive: true })
            {
                FoodPointError = FoodPoints.Count == 0
                    ? "Нет доступных точек питания."
                    : "Выберите активную точку питания.";
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                TitleError = "Введите название лота.";
                isValid = false;
            }
            else if (Title.Length > TitleMaxLength)
            {
                TitleError = $"Название не должно превышать {TitleMaxLength} символов.";
                isValid = false;
            }

            if (Description.Length > DescriptionMaxLength)
            {
                DescriptionError = $"Описание не должно превышать {DescriptionMaxLength} символов.";
                isValid = false;
            }

            if (TotalQuantity <= 0)
            {
                TotalQuantityError = "Количество должно быть больше нуля.";
                isValid = false;
            }

            if (Price <= 0)
            {
                PriceError = "Цена должна быть больше нуля.";
                isValid = false;
            }

            if (PickupDeadline is null || PickupTime is null)
            {
                PickupDeadlineError = "Укажите дату и время получения.";
                isValid = false;
            }
            else if (BuildPickupDeadlineUtc() <= DateTime.UtcNow)
            {
                PickupDeadlineError = "Срок получения должен быть в будущем.";
                isValid = false;
            }

            var hasComponentErrors = false;
            foreach (var component in Components)
            {
                component.ClearValidation();

                if (string.IsNullOrWhiteSpace(component.Name))
                {
                    component.NameError = "Введите название компонента.";
                    hasComponentErrors = true;
                }
                else if (component.Name.Length > ComponentNameMaxLength)
                {
                    component.NameError = $"Не больше {ComponentNameMaxLength} символов.";
                    hasComponentErrors = true;
                }

                if (component.Quantity <= 0)
                {
                    component.QuantityError = "Больше нуля.";
                    hasComponentErrors = true;
                }

                if (!AvailableComponentUnits.Contains(component.Unit))
                {
                    component.UnitError = "Выберите единицу.";
                    hasComponentErrors = true;
                }

                if (component.Composition.Length > ComponentCompositionMaxLength)
                {
                    component.CompositionError = $"Не больше {ComponentCompositionMaxLength} символов.";
                    hasComponentErrors = true;
                }
            }

            if (hasComponentErrors)
            {
                ComponentsError = "Проверьте поля добавленных компонентов.";
                isValid = false;
            }

            StatusMessage = isValid
                ? string.Empty
                : "Проверьте поля формы.";

            return isValid;
        }

        private void ClearValidation()
        {
            FoodPointError = string.Empty;
            TitleError = string.Empty;
            DescriptionError = string.Empty;
            TotalQuantityError = string.Empty;
            PriceError = string.Empty;
            PickupDeadlineError = string.Empty;
            ComponentsError = string.Empty;

            foreach (var component in Components)
                component.ClearValidation();
        }

        private DateTime BuildPickupDeadlineUtc()
        {
            var localDeadline = PickupDeadline!.Value.Date + PickupTime!.Value;
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
            return CreateComponentEditor(
                component?.Id,
                component?.Name ?? string.Empty,
                component?.Quantity ?? 1,
                component?.Unit ?? LotComponent.DefaultUnit,
                component?.Composition ?? string.Empty,
                component?.ImagePath ?? string.Empty);
        }

        private LotComponentEditorViewModel CreateComponentEditor(LotComponentSnapshot component)
        {
            return CreateComponentEditor(
                component.Id,
                component.Name,
                component.Quantity,
                component.Unit,
                component.Composition,
                component.ImagePath);
        }

        private LotComponentEditorViewModel CreateComponentEditor(
            Guid? id,
            string name,
            decimal quantity,
            string unit,
            string composition,
            string imagePath)
        {
            return new LotComponentEditorViewModel
            {
                Id = id,
                Name = name,
                Quantity = quantity,
                Unit = unit,
                Composition = composition,
                ImagePath = imagePath,
                RemoveImageCommand = RemoveComponentImageCommand,
                RemoveCommand = RemoveComponentCommand
            };
        }

        private IReadOnlyList<LotComponentRequest> BuildComponentRequests()
        {
            var result = new List<LotComponentRequest>();

            for (var index = 0; index < Components.Count; index++)
            {
                var component = Components[index];

                result.Add(new LotComponentRequest(
                    component.Id,
                    component.Name,
                    (int)component.Quantity,
                    component.Unit,
                    component.Composition,
                    component.ImagePath,
                    index));
            }

            return result;
        }

        private string BuildLotComposition(IReadOnlyList<LotComponentRequest> components)
        {
            if (components.Count == 0)
                return string.Empty;

            return string.Join("; ", components.Select(x => $"{x.Name.Trim()} — {x.Quantity} {x.Unit}"));
        }

        private LotFormSnapshot CaptureSnapshot()
        {
            return new LotFormSnapshot(
                _editingLotId,
                SelectedFoodPoint?.Id,
                Title,
                Description,
                Composition,
                ImagePath,
                Components.Select(x => new LotComponentSnapshot(
                    x.Id,
                    x.Name,
                    x.Quantity,
                    x.Unit,
                    x.Composition,
                    x.ImagePath)).ToList(),
                TotalQuantity,
                Price,
                PickupDeadline,
                PickupTime,
                IsEditMode);
        }

        private void ApplySnapshot(LotFormSnapshot snapshot)
        {
            _editingLotId = snapshot.EditingLotId;
            IsEditMode = snapshot.IsEditMode;
            IsCreateMode = !snapshot.IsEditMode;
            SelectedFoodPoint = snapshot.FoodPointId is Guid id
                ? FoodPoints.FirstOrDefault(x => x.Id == id)
                : FoodPoints.FirstOrDefault(x => x.IsActive) ?? FoodPoints.FirstOrDefault();
            Title = snapshot.Title;
            Description = snapshot.Description;
            Composition = snapshot.Composition;
            ImagePath = snapshot.ImagePath;
            Components = new ObservableCollection<LotComponentEditorViewModel>(
                snapshot.Components.Select(CreateComponentEditor));
            TotalQuantity = snapshot.TotalQuantity;
            Price = snapshot.Price;
            PickupDeadline = snapshot.PickupDeadline;
            PickupTime = snapshot.PickupTime;
            ClearValidation();
            RefreshComponentState();
        }

        private void RefreshComponentState()
        {
            OnPropertyChanged(nameof(HasComponents));
            OnPropertyChanged(nameof(HasNoComponents));
        }

        private sealed record LotFormSnapshot(
            Guid? EditingLotId,
            Guid? FoodPointId,
            string Title,
            string Description,
            string Composition,
            string ImagePath,
            IReadOnlyList<LotComponentSnapshot> Components,
            decimal TotalQuantity,
            decimal Price,
            DateTimeOffset? PickupDeadline,
            TimeSpan? PickupTime,
            bool IsEditMode);

        private sealed record LotComponentSnapshot(
            Guid? Id,
            string Name,
            decimal Quantity,
            string Unit,
            string Composition,
            string ImagePath);
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

        [ObservableProperty]
        private string _nameError = string.Empty;

        [ObservableProperty]
        private string _quantityError = string.Empty;

        [ObservableProperty]
        private string _unitError = string.Empty;

        [ObservableProperty]
        private string _compositionError = string.Empty;

        public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

        public bool HasNameError => !string.IsNullOrWhiteSpace(NameError);

        public bool HasQuantityError => !string.IsNullOrWhiteSpace(QuantityError);

        public bool HasUnitError => !string.IsNullOrWhiteSpace(UnitError);

        public bool HasCompositionError => !string.IsNullOrWhiteSpace(CompositionError);

        public IRelayCommand? RemoveImageCommand { get; set; }

        public IRelayCommand? RemoveCommand { get; set; }

        public void ClearValidation()
        {
            NameError = string.Empty;
            QuantityError = string.Empty;
            UnitError = string.Empty;
            CompositionError = string.Empty;
        }

        partial void OnImagePathChanged(string value) => OnPropertyChanged(nameof(HasImage));

        partial void OnNameChanged(string value)
        {
            NameError = string.Empty;
        }

        partial void OnQuantityChanged(decimal value)
        {
            QuantityError = string.Empty;
        }

        partial void OnUnitChanged(string value)
        {
            UnitError = string.Empty;
        }

        partial void OnCompositionChanged(string value)
        {
            CompositionError = string.Empty;
        }

        partial void OnNameErrorChanged(string value) => OnPropertyChanged(nameof(HasNameError));

        partial void OnQuantityErrorChanged(string value) => OnPropertyChanged(nameof(HasQuantityError));

        partial void OnUnitErrorChanged(string value) => OnPropertyChanged(nameof(HasUnitError));

        partial void OnCompositionErrorChanged(string value) => OnPropertyChanged(nameof(HasCompositionError));
    }
}
