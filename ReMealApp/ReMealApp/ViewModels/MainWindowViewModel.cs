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

        private LoginViewModel CreateLoginViewModel()
        {
            return new LoginViewModel(_authService, ShowRegistration, ShowProfile, CreateProfileViewModel);
        }

        private RegisterViewModel CreateRegisterViewModel()
        {
            return new RegisterViewModel(_authService, ShowLogin, ShowProfile, CreateProfileViewModel);
        }

        private UserProfileViewModel CreateProfileViewModel()
        {
            return new UserProfileViewModel(_userProfileService, _authService, ShowLogin);
        }

        private void ShowLogin()
        {
            CurrentViewModel = CreateLoginViewModel();
        }

        private void ShowRegistration()
        {
            CurrentViewModel = CreateRegisterViewModel();
        }

        private void ShowProfile(UserProfileViewModel profileViewModel)
        {
            CurrentViewModel = profileViewModel;
        }
    }
}
