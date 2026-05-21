using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Domain.Enums;
using ReMealApp.ViewModels.Shell;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace ReMealApp.ViewModels.Partner
{
    public partial class PartnerLotsViewModel : ViewModelBase
    {
        private readonly ILotService _lotService;
        private readonly IFoodPointService _foodPointService;
        private readonly HomeViewModel _shell;
        private readonly List<PartnerLotListItemViewModel> _allLots = new();

        [ObservableProperty]
        private ObservableCollection<PartnerLotListItemViewModel> _lots = new();

        [ObservableProperty]
        private ObservableCollection<LotStatusFilterOption> _statusFilters = new();

        [ObservableProperty]
        private LotStatusFilterOption? _selectedStatusFilter;

        [ObservableProperty]
        private ObservableCollection<FoodPointFilterOption> _foodPointFilters = new();

        [ObservableProperty]
        private FoodPointFilterOption? _selectedFoodPointFilter;

        [ObservableProperty]
        private PartnerLotListItemViewModel? _selectedLot;

        [ObservableProperty]
        private bool _showAvailableOnly;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private int _activeLotCount;

        [ObservableProperty]
        private int _endingTodayCount;

        [ObservableProperty]
        private int _archivedLotCount;

        [ObservableProperty]
        private int _selectedLotsCount;

        [ObservableProperty]
        private bool _hasAnyLots;

        [ObservableProperty]
        private bool _hasVisibleLots;

        [ObservableProperty]
        private bool _isFoodPointFilterEnabled;

        [ObservableProperty]
        private LotDetailsViewModel? _currentDetails;

        public PartnerLotsViewModel(
            ILotService lotService,
            IFoodPointService foodPointService,
            HomeViewModel shell)
        {
            _lotService = lotService;
            _foodPointService = foodPointService;
            _shell = shell;
            StatusFilters = new ObservableCollection<LotStatusFilterOption>
            {
                new(LotStatusFilterKind.All, "Все"),
                new(LotStatusFilterKind.Active, "Активные"),
                new(LotStatusFilterKind.EndingToday, "Скоро завершатся"),
                new(LotStatusFilterKind.SoldOut, "Распроданные"),
                new(LotStatusFilterKind.Expired, "Просроченные"),
                new(LotStatusFilterKind.Cancelled, "Снятые с публикации")
            };
            SelectedStatusFilter = StatusFilters.FirstOrDefault();
        }

        public bool IsListMode => CurrentDetails is null;

        public bool IsDetailMode => CurrentDetails is not null;

        public bool IsCatalogEmpty => !IsBusy && !HasVisibleLots;

        public bool IsFilteredEmptyState => HasAnyLots && IsCatalogEmpty;

        public bool CanResetFilters => HasActiveFilters;

        public string EmptyTitle => HasAnyLots ? "Ничего не найдено" : "У вас пока нет лотов";

        public string EmptySubtitle => HasAnyLots
            ? "Измените поисковый запрос или сбросьте фильтры."
            : "Создайте первый лот, чтобы он появился в каталоге и стал доступен покупателям.";

        private bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(SearchQuery) ||
            ShowAvailableOnly ||
            SelectedStatusFilter?.Kind is not null and not LotStatusFilterKind.All ||
            SelectedFoodPointFilter?.Id is not null;

        partial void OnShowAvailableOnlyChanged(bool value) => ApplyFilters();

        partial void OnSearchQueryChanged(string value) => ApplyFilters();

        partial void OnSelectedStatusFilterChanged(LotStatusFilterOption? value) => ApplyFilters();

        partial void OnSelectedFoodPointFilterChanged(FoodPointFilterOption? value) => ApplyFilters();

        partial void OnCurrentDetailsChanged(LotDetailsViewModel? value)
        {
            OnPropertyChanged(nameof(IsListMode));
            OnPropertyChanged(nameof(IsDetailMode));
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await ReloadDataCoreAsync();
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

        [RelayCommand(CanExecute = nameof(CanEditSelectedLot))]
        private void EditSelected()
        {
            if (SelectedLot is null)
                return;

            _shell.OpenLotEditor(SelectedLot.Id);
        }

        [RelayCommand(CanExecute = nameof(CanDuplicateSelectedLot))]
        private void DuplicateSelected()
        {
            StatusMessage = "Дублирование лота пока не реализовано.";
        }

        [RelayCommand(CanExecute = nameof(CanCancelSelectedLot))]
        private async Task CancelSelectedAsync()
        {
            if (SelectedLot is null || IsBusy)
                return;

            try
            {
                IsBusy = true;
                await _lotService.DeleteLotAsync(SelectedLot.Id);
                await ReloadDataCoreAsync();
                StatusMessage = "Лот снят с публикации.";
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
        private void CreateNew()
        {
            _shell.OpenCreateLot();
        }

        [RelayCommand]
        private void ResetFilters()
        {
            SearchQuery = string.Empty;
            ShowAvailableOnly = false;
            SelectedStatusFilter = StatusFilters.FirstOrDefault();
            SelectedFoodPointFilter = FoodPointFilters.FirstOrDefault();
            ApplyFilters();
        }

        [RelayCommand]
        private async Task OpenDetailsAsync(PartnerLotListItemViewModel? item)
        {
            if (item is null || IsBusy)
                return;

            try
            {
                IsBusy = true;
                var lot = await _lotService.GetCurrentPartnerLotAsync(item.Id);
                CurrentDetails = CreateDetails(lot);
                StatusMessage = lot is null ? "Лот не найден." : string.Empty;
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

        private void BackToList()
        {
            CurrentDetails = null;
        }

        private void EditCurrentDetails()
        {
            if (CurrentDetails is not { HasLot: true })
                return;

            _shell.OpenLotEditor(CurrentDetails.Id);
        }

        private async Task CancelCurrentDetailsAsync()
        {
            if (CurrentDetails is not { HasLot: true } || IsBusy)
                return;

            try
            {
                IsBusy = true;
                await _lotService.DeleteLotAsync(CurrentDetails.Id);
                var updated = await _lotService.GetCurrentPartnerLotAsync(CurrentDetails.Id);
                CurrentDetails = CreateDetails(updated);
                await ReloadDataCoreAsync();
                StatusMessage = "Лот снят с публикации.";
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

        private LotDetailsViewModel CreateDetails(FoodLot? lot)
        {
            return new LotDetailsViewModel(
                lot,
                new RelayCommand(BackToList),
                new RelayCommand(EditCurrentDetails, () => lot is { Status: not LotStatus.Cancelled and not LotStatus.Expired }),
                new AsyncRelayCommand(CancelCurrentDetailsAsync, () => lot is { Status: not LotStatus.Cancelled and not LotStatus.Expired }));
        }

        private async Task ReloadDataCoreAsync()
        {
            await _lotService.MarkExpiredLotsAsync();

            var selectedPointId = SelectedFoodPointFilter?.Id;
            var items = await _lotService.GetCurrentPartnerLotsAsync();
            var foodPoints = await _foodPointService.GetCurrentPartnerFoodPointsAsync();

            ReplaceAllLots(items);
            RebuildFoodPointFilters(foodPoints, selectedPointId);
            RecalculateStatistics();
            ApplyFilters();
        }

        private bool CanEditSelectedLot()
        {
            return !IsBusy && SelectedLotsCount == 1 && SelectedLot?.CanEdit == true;
        }

        private bool CanDuplicateSelectedLot()
        {
            return false;
        }

        private bool CanCancelSelectedLot()
        {
            return !IsBusy && SelectedLotsCount == 1 && SelectedLot?.CanCancel == true;
        }

        private void ReplaceAllLots(IEnumerable<FoodLot> lots)
        {
            foreach (var item in _allLots)
                item.PropertyChanged -= OnLotSelectionChanged;

            _allLots.Clear();

            foreach (var lot in lots)
            {
                var item = new PartnerLotListItemViewModel(lot);
                item.PropertyChanged += OnLotSelectionChanged;
                _allLots.Add(item);
            }
        }

        private void RebuildFoodPointFilters(IReadOnlyList<FoodPoint> foodPoints, Guid? selectedPointId)
        {
            IsFoodPointFilterEnabled = foodPoints.Count > 0;

            if (foodPoints.Count == 0)
            {
                FoodPointFilters = new ObservableCollection<FoodPointFilterOption>
                {
                    new(null, "Нет точек")
                };
                SelectedFoodPointFilter = FoodPointFilters[0];
                return;
            }

            var options = new List<FoodPointFilterOption>
            {
                new(null, "Все точки")
            };

            options.AddRange(foodPoints.Select(x => new FoodPointFilterOption(x.Id, x.Name)));
            FoodPointFilters = new ObservableCollection<FoodPointFilterOption>(options);
            SelectedFoodPointFilter = selectedPointId is Guid id
                ? FoodPointFilters.FirstOrDefault(x => x.Id == id) ?? FoodPointFilters[0]
                : FoodPointFilters[0];
        }

        private void RecalculateStatistics()
        {
            ActiveLotCount = _allLots.Count(x => x.IsAvailableForSale);
            EndingTodayCount = _allLots.Count(x => x.IsEndingToday);
            ArchivedLotCount = _allLots.Count(x => x.Lot.Status == LotStatus.Cancelled);
        }

        private void ApplyFilters()
        {
            var filtered = _allLots.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var query = SearchQuery.Trim();
                filtered = filtered.Where(x =>
                    Contains(x.Title, query) ||
                    Contains(x.FoodPointName, query) ||
                    Contains(x.DescriptionPreviewSource, query));
            }

            filtered = SelectedStatusFilter?.Kind switch
            {
                LotStatusFilterKind.Active => filtered.Where(x => x.Lot.Status == LotStatus.Active),
                LotStatusFilterKind.EndingToday => filtered.Where(x => x.IsEndingToday),
                LotStatusFilterKind.SoldOut => filtered.Where(x => x.Lot.Status == LotStatus.SoldOut),
                LotStatusFilterKind.Expired => filtered.Where(x => x.Lot.Status == LotStatus.Expired),
                LotStatusFilterKind.Cancelled => filtered.Where(x => x.Lot.Status == LotStatus.Cancelled),
                _ => filtered
            };

            if (SelectedFoodPointFilter?.Id is Guid foodPointId)
                filtered = filtered.Where(x => x.Lot.FoodPointId == foodPointId);

            if (ShowAvailableOnly)
                filtered = filtered.Where(x => x.IsAvailableForSale);

            var visible = filtered.ToList();
            var visibleIds = visible.Select(x => x.Id).ToHashSet();

            foreach (var hiddenSelected in _allLots.Where(x => x.IsSelected && !visibleIds.Contains(x.Id)).ToList())
                hiddenSelected.IsSelected = false;

            Lots = new ObservableCollection<PartnerLotListItemViewModel>(visible);
            HasAnyLots = _allLots.Count > 0;
            HasVisibleLots = Lots.Count > 0;
            StatusMessage = HasAnyLots
                ? $"Показано лотов: {Lots.Count} из {_allLots.Count}"
                : "Ваших лотов: 0";
            RefreshEmptyStateProperties();
            UpdateSelectionState();
        }

        private static bool Contains(string? source, string query)
        {
            return source?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true;
        }

        private void OnLotSelectionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PartnerLotListItemViewModel.IsSelected))
                UpdateSelectionState();
        }

        private void UpdateSelectionState()
        {
            var selected = _allLots.Where(x => x.IsSelected).ToList();
            SelectedLotsCount = selected.Count;
            SelectedLot = selected.Count == 1 ? selected[0] : null;

            EditSelectedCommand.NotifyCanExecuteChanged();
            DuplicateSelectedCommand.NotifyCanExecuteChanged();
            CancelSelectedCommand.NotifyCanExecuteChanged();
        }

        partial void OnHasAnyLotsChanged(bool value) => RefreshEmptyStateProperties();

        partial void OnHasVisibleLotsChanged(bool value) => RefreshEmptyStateProperties();

        partial void OnIsBusyChanged(bool value)
        {
            RefreshEmptyStateProperties();
            EditSelectedCommand.NotifyCanExecuteChanged();
            DuplicateSelectedCommand.NotifyCanExecuteChanged();
            CancelSelectedCommand.NotifyCanExecuteChanged();
        }

        private void RefreshEmptyStateProperties()
        {
            OnPropertyChanged(nameof(IsCatalogEmpty));
            OnPropertyChanged(nameof(IsFilteredEmptyState));
            OnPropertyChanged(nameof(CanResetFilters));
            OnPropertyChanged(nameof(EmptyTitle));
            OnPropertyChanged(nameof(EmptySubtitle));
        }
    }
}
