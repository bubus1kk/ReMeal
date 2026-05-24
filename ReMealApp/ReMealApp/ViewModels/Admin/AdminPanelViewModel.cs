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
        private readonly List<AdminLotDto> _allLots = new();
        private readonly List<AdminBookingDto> _allBookings = new();

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

        public ObservableCollection<AdminLotRowViewModel> Lots { get; } = new();

        public ObservableCollection<AdminBookingRowViewModel> Bookings { get; } = new();

        public ObservableCollection<AdminRoleFilterOption> RoleFilters { get; }

        public bool HasUsers => Users.Count > 0;

        public bool HasFoodPoints => FoodPoints.Count > 0;

        public bool HasLots => Lots.Count > 0;

        public bool HasBookings => Bookings.Count > 0;

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
                var lots = await _adminService.GetLotsAsync();
                var bookings = await _adminService.GetBookingsAsync();
                _allUsers.Clear();
                _allUsers.AddRange(users);
                _allFoodPoints.Clear();
                _allFoodPoints.AddRange(foodPoints);
                _allLots.Clear();
                _allLots.AddRange(lots);
                _allBookings.Clear();
                _allBookings.AddRange(bookings);
                ApplyUserFilter();
                ApplyFoodPoints();
                ApplyLots();
                ApplyBookings();
                HasAccess = true;
                StatusMessage = $"Пользователей: {users.Count}. Точек питания: {foodPoints.Count}. Лотов: {lots.Count}. Бронирований: {bookings.Count}.";
            }
            catch (Exception ex)
            {
                HasAccess = false;
                Users.Clear();
                FoodPoints.Clear();
                Lots.Clear();
                Bookings.Clear();
                OnPropertyChanged(nameof(HasUsers));
                OnPropertyChanged(nameof(HasFoodPoints));
                OnPropertyChanged(nameof(HasLots));
                OnPropertyChanged(nameof(HasBookings));
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

        [RelayCommand]
        private async Task ActivateUserAsync(Guid userId)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await _adminService.ActivateUserAsync(userId);
                await RefreshUsersAsync();
                StatusMessage = "Пользователь активирован.";
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
        private async Task DeactivateUserAsync(Guid userId)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await _adminService.DeactivateUserAsync(userId);
                await RefreshUsersAsync();
                StatusMessage = "Пользователь деактивирован.";
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

        private async Task RefreshUsersAsync()
        {
            var users = await _adminService.GetUsersAsync();
            _allUsers.Clear();
            _allUsers.AddRange(users);
            ApplyUserFilter();
        }

        [RelayCommand]
        private async Task CancelLotAsync(Guid lotId)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await _adminService.CancelLotAsync(lotId);
                await RefreshLotsAsync();
                StatusMessage = "Лот отменен.";
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

        private async Task RefreshLotsAsync()
        {
            var lots = await _adminService.GetLotsAsync();
            _allLots.Clear();
            _allLots.AddRange(lots);
            ApplyLots();
        }

        private void ApplyLots()
        {
            Lots.Clear();
            foreach (var lot in _allLots)
                Lots.Add(AdminLotRowViewModel.FromDto(lot));

            OnPropertyChanged(nameof(HasLots));
        }

        private void ApplyBookings()
        {
            Bookings.Clear();
            foreach (var booking in _allBookings)
                Bookings.Add(AdminBookingRowViewModel.FromDto(booking));

            OnPropertyChanged(nameof(HasBookings));
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

        public bool IsActive { get; init; }

        public bool CanActivate => !IsActive;

        public bool CanDeactivate => IsActive;

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
                ActivityStatusText = dto.ActivityStatusText,
                IsActive = dto.IsActive
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

    public sealed class AdminLotRowViewModel
    {
        public Guid Id { get; init; }

        public string Title { get; init; } = string.Empty;

        public string FoodPointName { get; init; } = string.Empty;

        public string OwnerName { get; init; } = string.Empty;

        public decimal Price { get; init; }

        public int TotalQuantity { get; init; }

        public int AvailableQuantity { get; init; }

        public DateTime PickupDeadline { get; init; }

        public string StatusText { get; init; } = string.Empty;

        public bool CanCancel { get; init; }

        public string QuantityText => $"{AvailableQuantity}/{TotalQuantity}";

        public static AdminLotRowViewModel FromDto(AdminLotDto dto)
        {
            return new AdminLotRowViewModel
            {
                Id = dto.Id,
                Title = dto.Title,
                FoodPointName = dto.FoodPointName,
                OwnerName = dto.OwnerName,
                Price = dto.Price,
                TotalQuantity = dto.TotalQuantity,
                AvailableQuantity = dto.AvailableQuantity,
                PickupDeadline = dto.PickupDeadline,
                StatusText = dto.StatusText,
                CanCancel = dto.CanCancel
            };
        }
    }

    public sealed class AdminBookingRowViewModel
    {
        public Guid Id { get; init; }

        public string CustomerName { get; init; } = string.Empty;

        public string LotTitle { get; init; } = string.Empty;

        public string FoodPointName { get; init; } = string.Empty;

        public int Quantity { get; init; }

        public decimal PriceAtReservation { get; init; }

        public decimal TotalPrice { get; init; }

        public DateTime ReservedAt { get; init; }

        public string StatusText { get; init; } = string.Empty;

        public static AdminBookingRowViewModel FromDto(AdminBookingDto dto)
        {
            return new AdminBookingRowViewModel
            {
                Id = dto.Id,
                CustomerName = dto.CustomerName,
                LotTitle = dto.LotTitle,
                FoodPointName = dto.FoodPointName,
                Quantity = dto.Quantity,
                PriceAtReservation = dto.PriceAtReservation,
                TotalPrice = dto.TotalPrice,
                ReservedAt = dto.ReservedAt,
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
