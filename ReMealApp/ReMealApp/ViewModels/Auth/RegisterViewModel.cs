using Application.DTOs.Auth;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using ReMealApp.ViewModels.Profile;

namespace ReMealApp.ViewModels.Auth
{
    public partial class RegisterViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly Action _showLogin;
        private readonly Action<UserProfileViewModel> _showProfile;
        private readonly Func<UserProfileViewModel> _createProfileViewModel;

        [ObservableProperty]
        private string _login = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private UserRole _selectedRole = UserRole.StudentCustomer;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public RegisterViewModel(
            IAuthService authService,
            Action showLogin,
            Action<UserProfileViewModel> showProfile,
            Func<UserProfileViewModel> createProfileViewModel)
        {
            _authService = authService;
            _showLogin = showLogin;
            _showProfile = showProfile;
            _createProfileViewModel = createProfileViewModel;
        }

        public UserRole[] Roles { get; } =
        [
            UserRole.StudentCustomer,
            UserRole.FoodPointRepresentative,
            UserRole.Administrator
        ];

        [RelayCommand]
        private async Task RegisterAsync()
        {
            ErrorMessage = string.Empty;

            var result = await _authService.RegisterAsync(new RegisterUserRequest
            {
                Login = Login,
                Password = Password,
                FullName = FullName,
                Email = Email,
                Phone = Phone,
                Role = SelectedRole
            });

            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage ?? "Не удалось зарегистрироваться.";
                return;
            }

            var profileViewModel = _createProfileViewModel();
            await profileViewModel.LoadAsync();
            _showProfile(profileViewModel);
        }

        [RelayCommand]
        private void OpenLogin()
        {
            _showLogin();
        }
    }
}
