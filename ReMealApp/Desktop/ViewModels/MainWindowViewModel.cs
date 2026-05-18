using CommunityToolkit.Mvvm.ComponentModel;
using ReMeal.Application.Interfaces;
using ReMeal.Desktop.ViewModels.Base;
using ReMeal.Desktop.ViewModels.Partner;

namespace ReMeal.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(
        IFoodPointService foodPointService,
        ILotService lotService,
        NavigationState navigationState)
    {
        FoodPoints = new FoodPointViewModel(foodPointService, navigationState, this);
        PartnerLots = new PartnerLotsViewModel(lotService, this);
        CreateLot = new CreateLotViewModel(foodPointService, lotService, navigationState, this);
    }

    public FoodPointViewModel FoodPoints { get; }

    public PartnerLotsViewModel PartnerLots { get; }

    public CreateLotViewModel CreateLot { get; }

    [ObservableProperty]
    private int _selectedTabIndex;

    partial void OnSelectedTabIndexChanged(int value) => _ = RefreshTabAsync(value);

    public async Task InitializeAsync()
    {
        await RefreshTabAsync(SelectedTabIndex);
    }

    public async Task RefreshTabAsync(int tabIndex)
    {
        switch (tabIndex)
        {
            case 0:
                await FoodPoints.LoadAsync();
                break;
            case 1:
                await PartnerLots.LoadAsync();
                break;
            case 2:
                await CreateLot.RefreshAsync();
                break;
        }
    }

    public async void OpenLotEditor(Guid lotId)
    {
        await CreateLot.LoadForEditAsync(lotId);
        SelectedTabIndex = 2;
    }

    public async void OpenCreateLot(Guid? foodPointId = null)
    {
        await CreateLot.PrepareCreateAsync(foodPointId);
        SelectedTabIndex = 2;
    }
}
