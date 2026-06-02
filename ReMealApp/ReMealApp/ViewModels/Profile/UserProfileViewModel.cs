using Application.DTOs.Booking;
using Application.DTOs.Profile;
using Application.DTOs.Users;
using Application.Interfaces;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using ReMealApp.ViewModels;
using ReMealApp.ViewModels.Shell;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ReMealApp.ViewModels.Profile
{
    public partial class UserProfileViewModel : ViewModelBase
    {
        private const string DefaultAvatarPath = "/Assets/Profile/avatar-placeholder.png";
        private const double CustomerPieChartSize = 184;
        private const double CustomerPieChartOuterRadius = 86;
        private const double CustomerPieChartInnerRadius = 48;

        private readonly IUserProfileService _userProfileService;
        private readonly IAuthService _authService;
        private readonly IProfileStatisticsService _profileStatisticsService;
        private readonly IBookingService? _bookingService;
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

        [ObservableProperty]
        private int _customerTotalBookingsCount;

        public UserProfileViewModel(
            IUserProfileService userProfileService,
            IAuthService authService,
            IProfileStatisticsService profileStatisticsService,
            Action<string> navigateToSection,
            Action<string> showLogin,
            IBookingService? bookingService = null)
        {
            _userProfileService = userProfileService;
            _authService = authService;
            _profileStatisticsService = profileStatisticsService;
            _bookingService = bookingService;
            _navigateToSection = navigateToSection;
            _showLogin = showLogin;
        }

        public ObservableCollection<ProfileKpiItemViewModel> Kpis { get; } = new();

        public ObservableCollection<ProfileFieldItemViewModel> ProfileFields { get; } = new();

        public ObservableCollection<PartnerFoodPointItemViewModel> PartnerFoodPoints { get; } = new();

        public ObservableCollection<RoleDistributionItemViewModel> RoleDistribution { get; } = new();

        public ObservableCollection<CustomerProfileMetricItemViewModel> CustomerActivityMetrics { get; } = new();

        public ObservableCollection<CustomerBookingStatusItemViewModel> CustomerBookingStatuses { get; } = new();

        public ObservableCollection<CustomerRecentBookingItemViewModel> CustomerRecentBookings { get; } = new();

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
            UserRole.Administrator => "Открыть администрирование",
            _ => "Открыть раздел"
        };

        public bool IsCustomer => Role == UserRole.StudentCustomer;

        public bool IsPartner => Role == UserRole.FoodPointRepresentative;

        public bool IsAdmin => Role == UserRole.Administrator;

        public bool IsCustomerOverviewVisible => IsCustomer;

        public bool HasCustomerRecentBookings => CustomerRecentBookings.Count > 0;

        public bool HasNoCustomerRecentBookings => IsCustomer && !HasCustomerRecentBookings;

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
                CustomerActivityMetrics.Clear();
                CustomerBookingStatuses.Clear();
                CustomerRecentBookings.Clear();
                CustomerTotalBookingsCount = 0;
                SelectedPartnerFoodPoint = null;

                if (IsCustomer)
                {
                    await LoadCustomerBookingsAsync();
                }
                else if (IsPartner)
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
                NotifyCustomerBookingStateChanged();
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
                UserRole.Administrator => HomeViewModel.AdminSection,
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
            OnPropertyChanged(nameof(IsCustomerOverviewVisible));
            OnPropertyChanged(nameof(HasNoCustomerRecentBookings));
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

        private async Task LoadCustomerBookingsAsync()
        {
            if (_bookingService is null)
            {
                ApplyCustomerBookings(Array.Empty<BookingDto>());
                return;
            }

            var bookings = await _bookingService.GetCurrentUserBookingsAsync();
            ApplyCustomerBookings(bookings);
        }

        private void ApplyCustomerBookings(IReadOnlyCollection<BookingDto> bookings)
        {
            var orderedBookings = bookings
                .OrderByDescending(x => x.ReservedAt)
                .ToList();

            var totalCount = orderedBookings.Count;
            var activeBookings = orderedBookings
                .Where(x => x.Status == BookingStatus.Active)
                .ToList();
            var issuedBookings = orderedBookings
                .Where(x => x.Status == BookingStatus.Issued)
                .ToList();
            var cancelledBookings = orderedBookings
                .Where(x => x.Status == BookingStatus.Cancelled)
                .ToList();

            Kpis.Add(new ProfileKpiItemViewModel(
                "Всего броней",
                totalCount.ToString("N0", CultureInfo.CurrentCulture),
                "За всё время"));
            Kpis.Add(new ProfileKpiItemViewModel(
                "Активные",
                activeBookings.Count.ToString("N0", CultureInfo.CurrentCulture),
                "Ожидают выдачи"));
            Kpis.Add(new ProfileKpiItemViewModel(
                "Получено",
                issuedBookings.Count.ToString("N0", CultureInfo.CurrentCulture),
                "Выданные брони"));
            Kpis.Add(new ProfileKpiItemViewModel(
                "Отменено",
                cancelledBookings.Count.ToString("N0", CultureInfo.CurrentCulture),
                "Отмененные брони"));

            var totalReservedQuantity = orderedBookings.Sum(x => x.Quantity);
            var activeQuantity = activeBookings.Sum(x => x.Quantity);
            var lastBooking = orderedBookings.FirstOrDefault();
            CustomerTotalBookingsCount = totalCount;

            CustomerActivityMetrics.Add(new CustomerProfileMetricItemViewModel(
                "Бронирований",
                totalCount.ToString("N0", CultureInfo.CurrentCulture),
                "Всего записей в истории"));
            CustomerActivityMetrics.Add(new CustomerProfileMetricItemViewModel(
                "Наборов забронировано",
                FormatQuantity(totalReservedQuantity),
                "Суммарное количество"));
            CustomerActivityMetrics.Add(new CustomerProfileMetricItemViewModel(
                "Активно сейчас",
                FormatQuantity(activeQuantity),
                "Ожидает выдачи"));
            CustomerActivityMetrics.Add(new CustomerProfileMetricItemViewModel(
                "Последняя бронь",
                FormatBookingDate(lastBooking),
                "По дате создания"));

            var startAngle = -90d;
            startAngle = AddCustomerStatus(
                "Активные",
                activeBookings.Count,
                totalCount,
                BookingStatus.Active,
                "#FFB84D",
                startAngle);
            startAngle = AddCustomerStatus(
                "Выданы",
                issuedBookings.Count,
                totalCount,
                BookingStatus.Issued,
                "#65D97A",
                startAngle);
            AddCustomerStatus(
                "Отменены",
                cancelledBookings.Count,
                totalCount,
                BookingStatus.Cancelled,
                "#FF7387",
                startAngle);

            foreach (var booking in orderedBookings.Take(4))
                CustomerRecentBookings.Add(CustomerRecentBookingItemViewModel.FromDto(booking));
        }

        private double AddCustomerStatus(
            string title,
            int count,
            int total,
            BookingStatus status,
            string color,
            double startAngle)
        {
            var percentage = total == 0
                ? 0
                : Math.Round(count * 100d / total, 1);
            var sweepAngle = total == 0 ? 0 : count / (double)total * 360;

            CustomerBookingStatuses.Add(new CustomerBookingStatusItemViewModel(
                title,
                count,
                percentage,
                status,
                color,
                CreateCustomerDonutSliceGeometry(startAngle, sweepAngle)));

            return startAngle + sweepAngle;
        }

        private void NotifyCustomerBookingStateChanged()
        {
            OnPropertyChanged(nameof(HasCustomerRecentBookings));
            OnPropertyChanged(nameof(HasNoCustomerRecentBookings));
        }

        private void RefreshProfileFields()
        {
            ProfileFields.Clear();
            ProfileFields.Add(new ProfileFieldItemViewModel("Логин", EmptyFallback(Login)));
            ProfileFields.Add(new ProfileFieldItemViewModel("Имя / ФИО", EmptyFallback(FullName)));
            ProfileFields.Add(new ProfileFieldItemViewModel("Email", EmptyFallback(Email)));
            ProfileFields.Add(new ProfileFieldItemViewModel("Телефон", PhoneText));
            ProfileFields.Add(new ProfileFieldItemViewModel("Роль", RoleText));
        }

        private static string EmptyFallback(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Не указано" : value;
        }

        private static string FormatBookingDate(BookingDto? booking)
        {
            return booking is null || booking.ReservedAt == default
                ? "Нет броней"
                : booking.ReservedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture);
        }

        private static string FormatQuantity(int quantity)
        {
            return $"{quantity.ToString("N0", CultureInfo.CurrentCulture)} шт.";
        }

        private static Geometry CreateCustomerDonutSliceGeometry(
            double startAngle,
            double sweepAngle)
        {
            if (sweepAngle <= 0)
                return new StreamGeometry();

            var center = new Point(
                CustomerPieChartSize / 2,
                CustomerPieChartSize / 2);

            var visibleSweepAngle = Math.Clamp(
                sweepAngle,
                0.1,
                359.99);
            var endAngle = startAngle + visibleSweepAngle;

            var outerStart = GetCustomerArcPoint(
                center,
                CustomerPieChartOuterRadius,
                startAngle);
            var outerEnd = GetCustomerArcPoint(
                center,
                CustomerPieChartOuterRadius,
                endAngle);
            var innerStart = GetCustomerArcPoint(
                center,
                CustomerPieChartInnerRadius,
                startAngle);
            var innerEnd = GetCustomerArcPoint(
                center,
                CustomerPieChartInnerRadius,
                endAngle);

            var geometry = new StreamGeometry();
            using var context = geometry.Open();

            var isLargeArc = visibleSweepAngle > 180;

            context.BeginFigure(outerStart, true);
            context.ArcTo(
                outerEnd,
                new Size(
                    CustomerPieChartOuterRadius,
                    CustomerPieChartOuterRadius),
                0,
                isLargeArc,
                SweepDirection.Clockwise);
            context.LineTo(innerEnd);
            context.ArcTo(
                innerStart,
                new Size(
                    CustomerPieChartInnerRadius,
                    CustomerPieChartInnerRadius),
                0,
                isLargeArc,
                SweepDirection.CounterClockwise);
            context.EndFigure(true);

            return geometry;
        }

        private static Point GetCustomerArcPoint(
            Point center,
            double radius,
            double angle)
        {
            var radians = angle * Math.PI / 180;

            return new Point(
                center.X + Math.Cos(radians) * radius,
                center.Y + Math.Sin(radians) * radius);
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

    public sealed class CustomerProfileMetricItemViewModel
    {
        public CustomerProfileMetricItemViewModel(string title, string value, string caption)
        {
            Title = title;
            Value = value;
            Caption = caption;
        }

        public string Title { get; }

        public string Value { get; }

        public string Caption { get; }
    }

    public sealed class CustomerBookingStatusItemViewModel
    {
        public CustomerBookingStatusItemViewModel(
            string title,
            int count,
            double percentage,
            BookingStatus status,
            string color,
            Geometry geometry)
        {
            Title = title;
            Count = count;
            Percentage = percentage;
            Status = status;
            CountText = count.ToString("N0", CultureInfo.CurrentCulture);
            PercentageText = $"{percentage.ToString("N1", CultureInfo.CurrentCulture)}%";
            Brush = new SolidColorBrush(Color.Parse(color));
            Geometry = geometry;
        }

        public string Title { get; }

        public int Count { get; }

        public double Percentage { get; }

        public BookingStatus Status { get; }

        public string CountText { get; }

        public string PercentageText { get; }

        public IBrush Brush { get; }

        public Geometry Geometry { get; }

        public bool IsActive => Status == BookingStatus.Active;

        public bool IsIssued => Status == BookingStatus.Issued;

        public bool IsCancelled => Status == BookingStatus.Cancelled;
    }

    public sealed class CustomerRecentBookingItemViewModel
    {
        private CustomerRecentBookingItemViewModel()
        {
        }

        public string LotTitle { get; init; } = string.Empty;

        public string FoodPointName { get; init; } = string.Empty;

        public string DateText { get; init; } = string.Empty;

        public string QuantityText { get; init; } = string.Empty;

        public string TotalText { get; init; } = string.Empty;

        public string StatusText { get; init; } = string.Empty;

        public BookingStatus Status { get; init; }

        public bool IsActive => Status == BookingStatus.Active;

        public bool IsIssued => Status == BookingStatus.Issued;

        public bool IsCancelled => Status == BookingStatus.Cancelled;

        public static CustomerRecentBookingItemViewModel FromDto(BookingDto dto)
        {
            return new CustomerRecentBookingItemViewModel
            {
                LotTitle = dto.DisplayLotTitle,
                FoodPointName = dto.DisplayFoodPointName,
                DateText = string.IsNullOrWhiteSpace(dto.DisplayReservedTime)
                    ? dto.DisplayReservedDate
                    : $"{dto.DisplayReservedDate} {dto.DisplayReservedTime}",
                QuantityText = dto.DisplayQuantityWithUnit,
                TotalText = dto.DisplayReservationTotal,
                StatusText = dto.StatusText,
                Status = dto.Status
            };
        }
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
