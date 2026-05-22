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
        private readonly IBookingService _bookingService;
        private readonly IProfileStatisticsService _profileStatisticsService;
        private readonly Action _exitApplication;

        [ObservableProperty]
        private ViewModelBase _currentViewModel;

        public MainWindowViewModel(
            IAuthService authService,
            IUserProfileService userProfileService,
            IFoodPointService foodPointService,
            ILotService lotService,
            IBookingService bookingService,
            IProfileStatisticsService profileStatisticsService,
            Action exitApplication)
        {
            _authService = authService;
            _userProfileService = userProfileService;
            _foodPointService = foodPointService;
            _lotService = lotService;
            _bookingService = bookingService;
            _profileStatisticsService = profileStatisticsService;
            _exitApplication = exitApplication;
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

        private LoginViewModel CreateLoginViewModel(string initialErrorMessage = "", string initialLogin = "")
        {
            return new LoginViewModel(_authService, ShowHomeAsync, initialErrorMessage, initialLogin);
        }

        private async Task ShowHomeAsync()
        {
            var homeViewModel = new HomeViewModel(
                _authService,
                _userProfileService,
                _foodPointService,
                _lotService,
                _bookingService,
                _profileStatisticsService,
                ShowLogin,
                _exitApplication);

            await homeViewModel.InitializeAsync();
            CurrentViewModel = homeViewModel;
        }

        private void ShowLogin(string initialLogin = "")
        {
            CurrentViewModel = CreateLoginViewModel(initialLogin: initialLogin);
        }
    }
}
