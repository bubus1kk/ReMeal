using Application.DTOs.Profile;
using Application.DTOs.Users;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using ReMealApp.ViewModels;
using ReMealApp.ViewModels.Shell;
using System.Collections.ObjectModel;

namespace ReMealApp.ViewModels.Profile
{
    public partial class UserProfileViewModel : ViewModelBase
    {
        private const string DefaultAvatarPath = "/Assets/Profile/avatar-placeholder.png";

        private readonly IUserProfileService _userProfileService;
        private readonly IAuthService _authService;
        private readonly IProfileStatisticsService _profileStatisticsService;
        private readonly Action<string> _navigateToSection;
        private readonly Action<string> _showLogin;

        [ObservableProperty]
        private string _login = string.Empty;

        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _avatarPath = string.Empty;

        [ObservableProperty]
        private UserRole _role;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isOverviewSelected = true;

        [ObservableProperty]
        private bool _isDetailsSelected;

        [ObservableProperty]
        private bool _isEditModalOpen;

        [ObservableProperty]
        private string _editFullName = string.Empty;

        [ObservableProperty]
        private string _editEmail = string.Empty;

        [ObservableProperty]
        private string _editPhone = string.Empty;

        [ObservableProperty]
        private string _modalStatusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _hasKpis;

        [ObservableProperty]
        private bool _hasPartnerFoodPoints;

        [ObservableProperty]
        private bool _hasNoPartnerFoodPoints = true;

        [ObservableProperty]
        private bool _hasSelectedPartnerFoodPoint;

        [ObservableProperty]
        private PartnerFoodPointItemViewModel? _selectedPartnerFoodPoint;

        [ObservableProperty]
        private bool _hasRoleDistribution;

        [ObservableProperty]
        private bool _isLogoutConfirmationOpen;

        public UserProfileViewModel(
            IUserProfileService userProfileService,
            IAuthService authService,
            IProfileStatisticsService profileStatisticsService,
            Action<string> navigateToSection,
            Action<string> showLogin)
        {
            _userProfileService = userProfileService;
            _authService = authService;
            _profileStatisticsService = profileStatisticsService;
            _navigateToSection = navigateToSection;
            _showLogin = showLogin;
        }

        public ObservableCollection<ProfileKpiItemViewModel> Kpis { get; } = new();

        public ObservableCollection<ProfileFieldItemViewModel> ProfileFields { get; } = new();

        public ObservableCollection<PartnerFoodPointItemViewModel> PartnerFoodPoints { get; } = new();

        public ObservableCollection<RoleDistributionItemViewModel> RoleDistribution { get; } = new();

        public string AvatarImagePath => string.IsNullOrWhiteSpace(AvatarPath) ? DefaultAvatarPath : AvatarPath;

        public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? Login : FullName;

        public string CompactDisplayName => FormatCompactName(DisplayName);

        public bool HasCustomAvatar => !string.IsNullOrWhiteSpace(AvatarPath);

        public bool HasNoCustomAvatar => !HasCustomAvatar;

        public bool HasModalStatusMessage => !string.IsNullOrWhiteSpace(ModalStatusMessage);

        public string PhoneText => string.IsNullOrWhiteSpace(Phone) ? "Телефон не добавлен" : Phone;

        public string RoleText => Role switch
        {
            UserRole.StudentCustomer => "Покупатель",
            UserRole.FoodPointRepresentative => "Партнер",
            UserRole.Administrator => "Администратор",
            _ => Role.ToString()
        };

        public string RoleDescription => Role switch
        {
            UserRole.StudentCustomer => "Личный профиль покупателя",
            UserRole.FoodPointRepresentative => "Кабинет представителя точки питания",
            UserRole.Administrator => "Системный профиль администратора",
            _ => "Профиль пользователя"
        };

        public string RoleActionText => Role switch
        {
            UserRole.StudentCustomer => "Мои брони",
            UserRole.FoodPointRepresentative => "Управлять точками",
            UserRole.Administrator => "К аналитике",
            _ => "Открыть раздел"
        };

        public bool IsCustomer => Role == UserRole.StudentCustomer;

        public bool IsPartner => Role == UserRole.FoodPointRepresentative;

        public bool IsAdmin => Role == UserRole.Administrator;

        public bool IsEmptyCustomerStatisticsVisible => IsCustomer;

        public bool IsPartnerOverviewVisible => IsPartner;

        public bool IsAdminOverviewVisible => IsAdmin;

        public async Task LoadAsync()
        {
            try
            {
                var profile = await _userProfileService.GetCurrentProfileAsync();
                if (profile is null)
                {
                    StatusMessage = "Пользователь не авторизован.";
                    return;
                }

                ApplyProfile(profile);
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }

        public async Task LoadStatisticsAsync()
        {
            try
            {
                Kpis.Clear();
                PartnerFoodPoints.Clear();
                RoleDistribution.Clear();
                SelectedPartnerFoodPoint = null;

                if (IsPartner)
                {
                    var statistics = await _profileStatisticsService.GetCurrentPartnerStatisticsAsync();
                    ApplyPartnerStatistics(statistics);
                }
                else if (IsAdmin)
                {
                    var statistics = await _profileStatisticsService.GetAdminStatisticsAsync();
                    ApplyAdminStatistics(statistics);
                }

                HasKpis = Kpis.Count > 0;
                HasPartnerFoodPoints = PartnerFoodPoints.Count > 0;
                HasNoPartnerFoodPoints = !HasPartnerFoodPoints;
                HasRoleDistribution = RoleDistribution.Count > 0;
            }
            catch (Exception ex)
            {
                StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }

        public async Task ChangeAvatarAsync(string sourceFilePath)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ModalStatusMessage = string.Empty;

                var profile = await _userProfileService.UpdateCurrentAvatarAsync(new UpdateUserAvatarRequest
                {
                    SourceFilePath = sourceFilePath
                });

                if (profile is null)
                {
                    ModalStatusMessage = "Не удалось обновить фото профиля.";
                    return;
                }

                ApplyProfile(profile);
                ModalStatusMessage = "Фото профиля обновлено.";
            }
            catch (Exception ex)
            {
                ModalStatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void ShowOverview()
        {
            IsOverviewSelected = true;
            IsDetailsSelected = false;
        }

        [RelayCommand]
        private void ShowDetails()
        {
            IsOverviewSelected = false;
            IsDetailsSelected = true;
        }

        [RelayCommand]
        private void OpenEditModal()
        {
            EditFullName = FullName;
            EditEmail = Email;
            EditPhone = Phone;
            ModalStatusMessage = string.Empty;
            IsEditModalOpen = true;
        }

        [RelayCommand]
        private void CloseEditModal()
        {
            IsEditModalOpen = false;
            ModalStatusMessage = string.Empty;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ModalStatusMessage = string.Empty;

                var profile = await _userProfileService.UpdateCurrentProfileAsync(new UpdateUserProfileRequest
                {
                    FullName = EditFullName,
                    Email = EditEmail,
                    Phone = EditPhone
                });

                if (profile is null)
                {
                    ModalStatusMessage = "Не удалось сохранить профиль.";
                    return;
                }

                ApplyProfile(profile);
                ModalStatusMessage = "Профиль сохранен.";
            }
            catch (Exception ex)
            {
                ModalStatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void RoleAction()
        {
            var section = Role switch
            {
                UserRole.StudentCustomer => HomeViewModel.BookingsSection,
                UserRole.FoodPointRepresentative => HomeViewModel.FoodPointsSection,
                UserRole.Administrator => HomeViewModel.AnalyticsSection,
                _ => HomeViewModel.ProfileSection
            };

            _navigateToSection(section);
        }

        [RelayCommand]
        private void Logout()
        {
            IsLogoutConfirmationOpen = true;
        }

        [RelayCommand]
        private void CancelLogout()
        {
            IsLogoutConfirmationOpen = false;
        }

        [RelayCommand]
        private void ConfirmLogout()
        {
            try
            {
                var initialLogin = _authService.IsCurrentUserRemembered() ? Login : string.Empty;
                _authService.Logout(string.IsNullOrWhiteSpace(initialLogin));
                IsLogoutConfirmationOpen = false;
                _showLogin(initialLogin);
            }
            catch (Exception ex)
            {
                StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }

        partial void OnRoleChanged(UserRole value)
        {
            OnPropertyChanged(nameof(RoleText));
            OnPropertyChanged(nameof(RoleDescription));
            OnPropertyChanged(nameof(RoleActionText));
            OnPropertyChanged(nameof(IsCustomer));
            OnPropertyChanged(nameof(IsPartner));
            OnPropertyChanged(nameof(IsAdmin));
            OnPropertyChanged(nameof(IsEmptyCustomerStatisticsVisible));
            OnPropertyChanged(nameof(IsPartnerOverviewVisible));
            OnPropertyChanged(nameof(IsAdminOverviewVisible));
        }

        partial void OnAvatarPathChanged(string value)
        {
            OnPropertyChanged(nameof(AvatarImagePath));
            OnPropertyChanged(nameof(HasCustomAvatar));
            OnPropertyChanged(nameof(HasNoCustomAvatar));
        }

        partial void OnFullNameChanged(string value)
        {
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(CompactDisplayName));
        }

        partial void OnLoginChanged(string value)
        {
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(CompactDisplayName));
        }

        partial void OnPhoneChanged(string value)
        {
            OnPropertyChanged(nameof(PhoneText));
        }

        partial void OnSelectedPartnerFoodPointChanged(PartnerFoodPointItemViewModel? value)
        {
            HasSelectedPartnerFoodPoint = value is not null;
        }

        partial void OnModalStatusMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasModalStatusMessage));
        }

        private void ApplyProfile(UserProfileDto profile)
        {
            Login = profile.Login;
            FullName = profile.FullName;
            Email = profile.Email;
            Phone = profile.Phone;
            AvatarPath = profile.AvatarPath;
            Role = profile.Role;
            RefreshProfileFields();
        }

        private void ApplyPartnerStatistics(PartnerProfileStatisticsDto statistics)
        {
            foreach (var kpi in statistics.Kpis)
                Kpis.Add(ProfileKpiItemViewModel.FromDto(kpi));

            var maxActiveLots = Math.Max(1, statistics.FoodPoints.Max(x => (int?)x.ActiveLots) ?? 0);
            foreach (var point in statistics.FoodPoints)
                PartnerFoodPoints.Add(PartnerFoodPointItemViewModel.FromDto(point, maxActiveLots));

            SelectedPartnerFoodPoint = PartnerFoodPoints.FirstOrDefault();
        }

        private void ApplyAdminStatistics(AdminProfileStatisticsDto statistics)
        {
            foreach (var kpi in statistics.Kpis)
                Kpis.Add(ProfileKpiItemViewModel.FromDto(kpi));

            foreach (var role in statistics.RoleDistribution)
                RoleDistribution.Add(RoleDistributionItemViewModel.FromDto(role));
        }

        private void RefreshProfileFields()
        {
            ProfileFields.Clear();
            ProfileFields.Add(new ProfileFieldItemViewModel("Логин", EmptyFallback(Login)));
            ProfileFields.Add(new ProfileFieldItemViewModel("Имя / ФИО", EmptyFallback(FullName)));
            ProfileFields.Add(new ProfileFieldItemViewModel("Email", EmptyFallback(Email)));
            ProfileFields.Add(new ProfileFieldItemViewModel("Телефон", PhoneText));
            ProfileFields.Add(new ProfileFieldItemViewModel("Роль", RoleText));
            ProfileFields.Add(new ProfileFieldItemViewModel("Дата регистрации", "Не хранится в текущей модели"));
        }

        private static string EmptyFallback(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Не указано" : value;
        }

        private static string FormatCompactName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var parts = value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 1)
                return parts[0];

            var initials = parts
                .Skip(1)
                .Where(x => x.Length > 0)
                .Take(2)
                .Select(x => $"{char.ToUpperInvariant(x[0])}.")
                .ToArray();

            return initials.Length == 0
                ? parts[0]
                : $"{parts[0]} {string.Concat(initials)}";
        }
    }

    public sealed class ProfileKpiItemViewModel
    {
        public ProfileKpiItemViewModel(string title, string value, string caption)
        {
            Title = title;
            Value = value;
            Caption = caption;
        }

        public string Title { get; }

        public string Value { get; }

        public string Caption { get; }

        public static ProfileKpiItemViewModel FromDto(ProfileKpiDto dto)
        {
            return new ProfileKpiItemViewModel(dto.Title, dto.Value, dto.Caption);
        }
    }

    public sealed class ProfileFieldItemViewModel
    {
        public ProfileFieldItemViewModel(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }

        public string Value { get; }
    }

    public sealed class PartnerFoodPointItemViewModel
    {
        public Guid FoodPointId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Address { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string Phone { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public DateTime CreatedAt { get; init; }

        public int TotalLots { get; init; }

        public int ActiveLots { get; init; }

        public int SoldOutLots { get; init; }

        public int ExpiredLots { get; init; }

        public int CancelledLots { get; init; }

        public int AvailableSets { get; init; }

        public double ActivityPercentage { get; init; }

        public string StatusText => IsActive ? "Активна" : "Деактивирована";

        public string DescriptionText => string.IsNullOrWhiteSpace(Description) ? "Описание не добавлено" : Description;

        public string CreatedAtText => CreatedAt == default ? "Не указано" : CreatedAt.ToLocalTime().ToString("dd.MM.yyyy");

        public static PartnerFoodPointItemViewModel FromDto(PartnerFoodPointStatisticsDto dto, int maxActiveLots)
        {
            return new PartnerFoodPointItemViewModel
            {
                FoodPointId = dto.FoodPointId,
                Name = dto.Name,
                Address = dto.Address,
                Description = dto.Description,
                Phone = dto.Phone,
                IsActive = dto.IsActive,
                CreatedAt = dto.CreatedAt,
                TotalLots = dto.TotalLots,
                ActiveLots = dto.ActiveLots,
                SoldOutLots = dto.SoldOutLots,
                ExpiredLots = dto.ExpiredLots,
                CancelledLots = dto.CancelledLots,
                AvailableSets = dto.AvailableSets,
                ActivityPercentage = dto.ActiveLots == 0 ? 0 : Math.Round(dto.ActiveLots * 100d / maxActiveLots, 1)
            };
        }
    }

    public sealed class RoleDistributionItemViewModel
    {
        public RoleDistributionItemViewModel(string title, int count, double percentage)
        {
            Title = title;
            Count = count;
            Percentage = percentage;
            Summary = $"{count:N0} - {percentage:N1}%";
        }

        public string Title { get; }

        public int Count { get; }

        public double Percentage { get; }

        public string Summary { get; }

        public static RoleDistributionItemViewModel FromDto(AdminRoleDistributionDto dto)
        {
            return new RoleDistributionItemViewModel(dto.RoleText, dto.Count, dto.Percentage);
        }
    }
}
