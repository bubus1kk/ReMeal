using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using ReMealApp.ViewModels.Auth;
using ReMealApp.ViewModels.Shell;

namespace ReMealApp.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly IUserProfileService _userProfileService;
        private readonly IFoodPointService _foodPointService;
        private readonly ILotService _lotService;

        [ObservableProperty]
        private ViewModelBase _currentViewModel;

        public MainWindowViewModel(
            IAuthService authService,
            IUserProfileService userProfileService,
            IFoodPointService foodPointService,
            ILotService lotService)
        {
            _authService = authService;
            _userProfileService = userProfileService;
            _foodPointService = foodPointService;
            _lotService = lotService;
            _currentViewModel = CreateLoginViewModel();
        }

        public async Task InitializeAsync()
        {
            try
            {
                var rememberedUser = await _authService.TryRestoreRememberedUserAsync();
                if (rememberedUser is not null)
                    await ShowHomeAsync();
            }
            catch (Exception ex)
            {
                CurrentViewModel = CreateLoginViewModel(ExceptionMessageFormatter.ToUserMessage(ex));
            }
        }

        private LoginViewModel CreateLoginViewModel(string initialErrorMessage = "")
        {
            return new LoginViewModel(_authService, ShowHomeAsync, initialErrorMessage);
        }

        private async Task ShowHomeAsync()
        {
            var homeViewModel = new HomeViewModel(
                _authService,
                _userProfileService,
                _foodPointService,
                _lotService,
                ShowLogin);

            await homeViewModel.InitializeAsync();
            CurrentViewModel = homeViewModel;
        }

        private void ShowLogin()
        {
            CurrentViewModel = CreateLoginViewModel();
        }
    }
}
