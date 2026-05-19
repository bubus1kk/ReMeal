using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Enums;
using ReMealApp.ViewModels.Catalog;
using ReMealApp.ViewModels.Partner;
using ReMealApp.ViewModels.Profile;

namespace ReMealApp.ViewModels.Shell
{
    public partial class HomeViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private int _selectedTabIndex;

        [ObservableProperty]
        private bool _isPartner;

        [ObservableProperty]
        private bool _isCatalogVisible = true;

        public HomeViewModel(
            IAuthService authService,
            IUserProfileService userProfileService,
            IFoodPointService foodPointService,
            ILotService lotService,
            Action showLogin)
        {
            _authService = authService;
            Profile = new UserProfileViewModel(userProfileService, authService, showLogin);
            Catalog = new CatalogViewModel(lotService);
            FoodPoint = new FoodPointViewModel(foodPointService, this);
            PartnerLots = new PartnerLotsViewModel(lotService, this);
            CreateLot = new CreateLotViewModel(foodPointService, lotService, this);
        }

        public UserProfileViewModel Profile { get; }

        public CatalogViewModel Catalog { get; }

        public FoodPointViewModel FoodPoint { get; }

        public PartnerLotsViewModel PartnerLots { get; }

        public CreateLotViewModel CreateLot { get; }

        public async Task InitializeAsync()
        {
            try
            {
                await Profile.LoadAsync();

                var currentUser = await _authService.GetCurrentUserAsync();
                IsPartner = currentUser?.Role == UserRole.FoodPointRepresentative;
                IsCatalogVisible = currentUser?.Role != UserRole.FoodPointRepresentative;

                if (IsPartner)
                {
                    await FoodPoint.LoadAsync();
                    await PartnerLots.LoadAsync();
                    await CreateLot.RefreshAsync();
                }
                else
                {
                    await Catalog.LoadAsync();
                }
            }
            catch (Exception ex)
            {
                Profile.StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
                IsPartner = false;
                IsCatalogVisible = false;
            }
        }

        partial void OnSelectedTabIndexChanged(int value)
        {
            _ = RefreshTabAsync(value);
        }

        public async Task RefreshPartnerAsync()
        {
            await FoodPoint.LoadAsync();
            await PartnerLots.LoadAsync();
            await CreateLot.RefreshAsync();
        }

        public async void OpenLotEditor(Guid lotId)
        {
            try
            {
                await CreateLot.LoadForEditAsync(lotId);
                SelectedTabIndex = 4;
            }
            catch (Exception ex)
            {
                Profile.StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }

        public async void OpenCreateLot()
        {
            try
            {
                await CreateLot.PrepareCreateAsync();
                SelectedTabIndex = 4;
            }
            catch (Exception ex)
            {
                Profile.StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }

        private async Task RefreshTabAsync(int tabIndex)
        {
            try
            {
                switch (tabIndex)
                {
                    case 1 when IsCatalogVisible:
                        await Catalog.LoadAsync();
                        break;
                    case 2 when IsPartner:
                        await FoodPoint.LoadAsync();
                        break;
                    case 3 when IsPartner:
                        await PartnerLots.LoadAsync();
                        break;
                    case 4 when IsPartner:
                        await CreateLot.RefreshAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                Profile.StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }
    }
}
