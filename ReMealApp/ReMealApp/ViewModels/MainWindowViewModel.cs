using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using ReMealApp.ViewModels.Auth;
using ReMealApp.ViewModels.Profile;

namespace ReMealApp.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly IUserProfileService _userProfileService;

        [ObservableProperty]
        private ViewModelBase _currentViewModel;

        public MainWindowViewModel(IAuthService authService, IUserProfileService userProfileService)
        {
            _authService = authService;
            _userProfileService = userProfileService;
            _currentViewModel = CreateLoginViewModel();
        }

        public async Task InitializeAsync()
        {
            var rememberedUser = await _authService.TryRestoreRememberedUserAsync();
            if (rememberedUser is not null)
                await ShowProfile();
        }

        private LoginViewModel CreateLoginViewModel()
        {
            return new LoginViewModel(_authService, ShowProfile);
        }

        private UserProfileViewModel CreateProfileViewModel()
        {
            return new UserProfileViewModel(_userProfileService, _authService, ShowLogin);
        }

        private async Task ShowProfile()
        {
            var profileViewModel = CreateProfileViewModel();
            await profileViewModel.LoadAsync();
            CurrentViewModel = profileViewModel;
        }

        private void ShowLogin()
        {
            CurrentViewModel = CreateLoginViewModel();
        }
    }
}
