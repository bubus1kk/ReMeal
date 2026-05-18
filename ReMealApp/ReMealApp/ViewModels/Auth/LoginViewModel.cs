using Application.DTOs.Auth;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;

namespace ReMealApp.ViewModels.Auth
{
    public enum AuthMode
    {
        Login,
        Register
    }

    public partial class LoginViewModel : ViewModelBase
    {
        private const int MaxInputLength = 100;

        private readonly IAuthService _authService;
        private readonly Func<Task> _showProfileAsync;

        [ObservableProperty]
        private AuthMode _currentMode = AuthMode.Login;

        [ObservableProperty]
        private string _login = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _rememberMe = true;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private bool _acceptedTerms;

        [ObservableProperty]
        private UserRole _selectedRole = UserRole.StudentCustomer;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isPasswordVisible;

        [ObservableProperty]
        private bool _isConfirmPasswordVisible;

        public LoginViewModel(IAuthService authService, Func<Task> showProfileAsync)
        {
            _authService = authService;
            _showProfileAsync = showProfileAsync;
        }

        public UserRole[] Roles { get; } =
        [
            UserRole.StudentCustomer,
            UserRole.FoodPointRepresentative,
            UserRole.Administrator
        ];

        public bool IsLoginMode => CurrentMode == AuthMode.Login;

        public bool IsRegisterMode => CurrentMode == AuthMode.Register;

        public bool IsPasswordHidden => !IsPasswordVisible;

        public bool IsConfirmPasswordHidden => !IsConfirmPasswordVisible;

        public string PasswordVisibilityText => IsPasswordVisible ? "Скрыть" : "Показать";

        public string ConfirmPasswordVisibilityText => IsConfirmPasswordVisible ? "Скрыть" : "Показать";

        [RelayCommand]
        private void ShowLogin()
        {
            ErrorMessage = string.Empty;
            CurrentMode = AuthMode.Login;
        }

        [RelayCommand]
        private void ShowRegister()
        {
            ErrorMessage = string.Empty;
            CurrentMode = AuthMode.Register;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            ErrorMessage = string.Empty;

            var result = await _authService.LoginAsync(new LoginUserRequest
            {
                Login = Login,
                Password = Password,
                RememberMe = RememberMe
            });

            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage ?? "Не удалось войти.";
                return;
            }

            await _showProfileAsync();
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            ErrorMessage = string.Empty;

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Пароли не совпадают.";
                return;
            }

            var result = await _authService.RegisterAsync(new RegisterUserRequest
            {
                Login = Login,
                Password = Password,
                FullName = Name,
                Email = Email,
                Phone = Phone,
                Role = SelectedRole
            });

            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage ?? "Не удалось зарегистрироваться.";
                return;
            }

            await _showProfileAsync();
        }

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        [RelayCommand]
        private void ToggleConfirmPasswordVisibility()
        {
            IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
        }

        partial void OnCurrentModeChanged(AuthMode value)
        {
            OnPropertyChanged(nameof(IsLoginMode));
            OnPropertyChanged(nameof(IsRegisterMode));
        }

        partial void OnLoginChanged(string value)
        {
            Login = LimitInput(value);
        }

        partial void OnEmailChanged(string value)
        {
            Email = LimitInput(value);
        }

        partial void OnPasswordChanged(string value)
        {
            Password = LimitInput(value);
        }

        partial void OnNameChanged(string value)
        {
            Name = LimitInput(value);
        }

        partial void OnPhoneChanged(string value)
        {
            Phone = LimitInput(value);
        }

        partial void OnConfirmPasswordChanged(string value)
        {
            ConfirmPassword = LimitInput(value);
        }

        partial void OnIsPasswordVisibleChanged(bool value)
        {
            OnPropertyChanged(nameof(IsPasswordHidden));
            OnPropertyChanged(nameof(PasswordVisibilityText));
        }

        partial void OnIsConfirmPasswordVisibleChanged(bool value)
        {
            OnPropertyChanged(nameof(IsConfirmPasswordHidden));
            OnPropertyChanged(nameof(ConfirmPasswordVisibilityText));
        }

        private static string LimitInput(string value)
        {
            return value.Length <= MaxInputLength ? value : value[..MaxInputLength];
        }
    }
}
