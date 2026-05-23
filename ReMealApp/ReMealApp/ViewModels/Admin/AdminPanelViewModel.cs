using Application.DTOs.Admin;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using System.Collections.ObjectModel;

namespace ReMealApp.ViewModels.Admin
{
    public partial class AdminPanelViewModel : ViewModelBase
    {
        private readonly IAdminService _adminService;
        private readonly List<AdminUserDto> _allUsers = new();
        private readonly List<AdminFoodPointDto> _allFoodPoints = new();

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _hasAccess;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private AdminRoleFilterOption? _selectedRoleFilter;

        public AdminPanelViewModel(IAdminService adminService)
        {
            _adminService = adminService;
            RoleFilters =
            [
                new AdminRoleFilterOption(null, "Все роли"),
                new AdminRoleFilterOption(UserRole.StudentCustomer, "Покупатели"),
                new AdminRoleFilterOption(UserRole.FoodPointRepresentative, "Представители"),
                new AdminRoleFilterOption(UserRole.Administrator, "Администраторы")
            ];
            _selectedRoleFilter = RoleFilters[0];
        }

        public ObservableCollection<AdminUserRowViewModel> Users { get; } = new();

        public ObservableCollection<AdminFoodPointRowViewModel> FoodPoints { get; } = new();

        public ObservableCollection<AdminRoleFilterOption> RoleFilters { get; }

        public bool HasUsers => Users.Count > 0;

        public bool HasFoodPoints => FoodPoints.Count > 0;

        public async Task InitializeAsync()
        {
            await LoadAsync();
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                var users = await _adminService.GetUsersAsync();
                var foodPoints = await _adminService.GetFoodPointsAsync();
                _allUsers.Clear();
                _allUsers.AddRange(users);
                _allFoodPoints.Clear();
                _allFoodPoints.AddRange(foodPoints);
                ApplyUserFilter();
                ApplyFoodPoints();
                HasAccess = true;
                StatusMessage = $"Пользователей: {users.Count}. Точек питания: {foodPoints.Count}. Активность пользователей в текущей модели не хранится.";
            }
            catch (Exception ex)
            {
                HasAccess = false;
                Users.Clear();
                FoodPoints.Clear();
                OnPropertyChanged(nameof(HasUsers));
                OnPropertyChanged(nameof(HasFoodPoints));
                StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSelectedRoleFilterChanged(AdminRoleFilterOption? value)
        {
            ApplyUserFilter();
        }

        private void ApplyUserFilter()
        {
            if (!HasAccess && _allUsers.Count == 0)
                return;

            var role = SelectedRoleFilter?.Role;
            var visibleUsers = role is null
                ? _allUsers
                : _allUsers.Where(x => x.Role == role).ToList();

            Users.Clear();
            foreach (var user in visibleUsers)
                Users.Add(AdminUserRowViewModel.FromDto(user));

            OnPropertyChanged(nameof(HasUsers));
        }

        [RelayCommand]
        private async Task ActivateFoodPointAsync(Guid foodPointId)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await _adminService.ActivateFoodPointAsync(foodPointId);
                await RefreshFoodPointsAsync();
                StatusMessage = "Точка питания активирована.";
            }
            catch (Exception ex)
            {
                StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeactivateFoodPointAsync(Guid foodPointId)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await _adminService.DeactivateFoodPointAsync(foodPointId);
                await RefreshFoodPointsAsync();
                StatusMessage = "Точка питания деактивирована.";
            }
            catch (Exception ex)
            {
                StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshFoodPointsAsync()
        {
            var foodPoints = await _adminService.GetFoodPointsAsync();
            _allFoodPoints.Clear();
            _allFoodPoints.AddRange(foodPoints);
            ApplyFoodPoints();
        }

        private void ApplyFoodPoints()
        {
            FoodPoints.Clear();
            foreach (var foodPoint in _allFoodPoints)
                FoodPoints.Add(AdminFoodPointRowViewModel.FromDto(foodPoint));

            OnPropertyChanged(nameof(HasFoodPoints));
        }
    }

    public sealed class AdminUserRowViewModel
    {
        public Guid Id { get; init; }

        public string Login { get; init; } = string.Empty;

        public string FullName { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string Phone { get; init; } = string.Empty;

        public string RoleText { get; init; } = string.Empty;

        public string ActivityStatusText { get; init; } = string.Empty;

        public static AdminUserRowViewModel FromDto(AdminUserDto dto)
        {
            return new AdminUserRowViewModel
            {
                Id = dto.Id,
                Login = dto.Login,
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                RoleText = dto.RoleText,
                ActivityStatusText = dto.ActivityStatusText
            };
        }
    }

    public sealed class AdminFoodPointRowViewModel
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Address { get; init; } = string.Empty;

        public string Phone { get; init; } = string.Empty;

        public string OwnerText { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public string StatusText { get; init; } = string.Empty;

        public bool CanActivate => !IsActive;

        public bool CanDeactivate => IsActive;

        public static AdminFoodPointRowViewModel FromDto(AdminFoodPointDto dto)
        {
            return new AdminFoodPointRowViewModel
            {
                Id = dto.Id,
                Name = dto.Name,
                Address = dto.Address,
                Phone = dto.Phone,
                OwnerText = string.IsNullOrWhiteSpace(dto.OwnerLogin)
                    ? dto.OwnerName
                    : $"{dto.OwnerName} ({dto.OwnerLogin})",
                IsActive = dto.IsActive,
                StatusText = dto.StatusText
            };
        }
    }

    public sealed class AdminRoleFilterOption
    {
        public AdminRoleFilterOption(UserRole? role, string displayName)
        {
            Role = role;
            DisplayName = displayName;
        }

        public UserRole? Role { get; }

        public string DisplayName { get; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
