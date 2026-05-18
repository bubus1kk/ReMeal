using Application.DTOs.Auth;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReMealApp.ViewModels.Profile;

namespace ReMealApp.ViewModels.Auth
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly Action _showRegister;
        private readonly Action<UserProfileViewModel> _showProfile;
        private readonly Func<UserProfileViewModel> _createProfileViewModel;

        [ObservableProperty]
        private string _login = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public LoginViewModel(
            IAuthService authService,
            Action showRegister,
            Action<UserProfileViewModel> showProfile,
            Func<UserProfileViewModel> createProfileViewModel)
        {
            _authService = authService;
            _showRegister = showRegister;
            _showProfile = showProfile;
            _createProfileViewModel = createProfileViewModel;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            ErrorMessage = string.Empty;

            var result = await _authService.LoginAsync(new LoginUserRequest
            {
                Login = Login,
                Password = Password
            });

            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage ?? "Не удалось войти.";
                return;
            }

            var profileViewModel = _createProfileViewModel();
            await profileViewModel.LoadAsync();
            _showProfile(profileViewModel);
        }

        [RelayCommand]
        private void OpenRegistration()
        {
            _showRegister();
        }
    }
}
