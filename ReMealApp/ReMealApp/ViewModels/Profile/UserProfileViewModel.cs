using Application.DTOs.Users;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using ReMealApp.ViewModels;

namespace ReMealApp.ViewModels.Profile
{
    public partial class UserProfileViewModel : ViewModelBase
    {
        private readonly IUserProfileService _userProfileService;
        private readonly IAuthService _authService;
        private readonly Action _showLogin;

        [ObservableProperty]
        private string _login = string.Empty;

        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private UserRole _role;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public UserProfileViewModel(IUserProfileService userProfileService, IAuthService authService, Action showLogin)
        {
            _userProfileService = userProfileService;
            _authService = authService;
            _showLogin = showLogin;
        }

        public string RoleText => Role switch
        {
            UserRole.StudentCustomer => "Покупатель / студент",
            UserRole.FoodPointRepresentative => "Представитель точки питания",
            UserRole.Administrator => "Администратор",
            _ => Role.ToString()
        };

        public async Task LoadAsync()
        {
            var profile = await _userProfileService.GetCurrentProfileAsync();
            if (profile is null)
            {
                StatusMessage = "Пользователь не авторизован.";
                return;
            }

            ApplyProfile(profile);
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            StatusMessage = string.Empty;

            var profile = await _userProfileService.UpdateCurrentProfileAsync(new UpdateUserProfileRequest
            {
                FullName = FullName,
                Email = Email,
                Phone = Phone
            });

            if (profile is null)
            {
                StatusMessage = "Не удалось сохранить профиль.";
                return;
            }

            ApplyProfile(profile);
            StatusMessage = "Профиль сохранён.";
        }

        [RelayCommand]
        private void Logout()
        {
            _authService.Logout();
            _showLogin();
        }

        partial void OnRoleChanged(UserRole value)
        {
            OnPropertyChanged(nameof(RoleText));
        }

        private void ApplyProfile(UserProfileDto profile)
        {
            Login = profile.Login;
            FullName = profile.FullName;
            Email = profile.Email;
            Phone = profile.Phone;
            Role = profile.Role;
        }
    }
}
