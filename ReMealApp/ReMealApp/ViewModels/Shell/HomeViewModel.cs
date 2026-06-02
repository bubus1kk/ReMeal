using Application.DTOs.Admin;
using Application.DTOs.Analytics;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using ReMealApp.ViewModels.Admin;
using ReMealApp.ViewModels.Analytics;
using ReMealApp.ViewModels.Booking;
using ReMealApp.ViewModels.Catalog;
using ReMealApp.ViewModels.Maps;
using ReMealApp.ViewModels.Partner;
using ReMealApp.ViewModels.Profile;

namespace ReMealApp.ViewModels.Shell
{
    public partial class HomeViewModel : ViewModelBase
    {
        public const string HomeSection = "home";
        public const string CatalogSection = "catalog";
        public const string BookingsSection = "bookings";
        public const string FavoritesSection = "favorites";
        public const string FoodPointsSection = "food-points";
        public const string AnalyticsSection = "analytics";
        public const string AdminSection = "admin";
        public const string ProfileSection = "profile";
        public const string SettingsSection = "settings";
        public const string PartnerLotsSection = "partner-lots";
        public const string PartnerBookingsSection = "partner-bookings";
        public const string CreateLotSection = "create-lot";

        private readonly IAuthService _authService;
        private readonly Action<string> _showLogin;
        private readonly Action _exitApplication;
        private readonly ILotService _lotService;
        private readonly IBookingService _bookingService;
        private readonly IAdminService _adminService;
        private readonly IPartnerAnalyticsService _partnerAnalyticsService;

        [ObservableProperty]
        private ViewModelBase _currentSectionViewModel;

        [ObservableProperty]
        private string _selectedSectionKey = ProfileSection;

        [ObservableProperty]
        private bool _isPartner;

        [ObservableProperty]
        private bool _isAdmin;

        [ObservableProperty]
        private bool _isCatalogVisible = true;

        [ObservableProperty]
        private bool _isLotsNavigationVisible = true;

        [ObservableProperty]
        private bool _isBookingsVisible = true;

        [ObservableProperty]
        private bool _isAdminVisible;

        [ObservableProperty]
        private bool _isSidebarExpanded = true;

        [ObservableProperty]
        private bool _isLogoutConfirmationOpen;

        [ObservableProperty]
        private bool _isApplicationExitConfirmationOpen;

        public HomeViewModel(
            IAuthService authService,
            IUserProfileService userProfileService,
            IFoodPointService foodPointService,
            ILotService lotService,
            IBookingService bookingService,
            IProfileStatisticsService profileStatisticsService,
            Action<string> showLogin,
            Action exitApplication)
            : this(
                authService,
                userProfileService,
                foodPointService,
                lotService,
                bookingService,
                new DeniedAdminService(),
                profileStatisticsService,
                new UnavailableGeocodingService(),
                new UnavailableMapService(),
                new UnavailablePartnerAnalyticsService(),
                showLogin,
                exitApplication)
        {
        }

        public HomeViewModel(
            IAuthService authService,
            IUserProfileService userProfileService,
            IFoodPointService foodPointService,
            ILotService lotService,
            IBookingService bookingService,
            IProfileStatisticsService profileStatisticsService,
            IPartnerAnalyticsService partnerAnalyticsService,
            Action<string> showLogin,
            Action exitApplication)
            : this(
                authService,
                userProfileService,
                foodPointService,
                lotService,
                bookingService,
                new DeniedAdminService(),
                profileStatisticsService,
                new UnavailableGeocodingService(),
                new UnavailableMapService(),
                partnerAnalyticsService,
                showLogin,
                exitApplication)
        {
        }

        public HomeViewModel(
            IAuthService authService,
            IUserProfileService userProfileService,
            IFoodPointService foodPointService,
            ILotService lotService,
            IBookingService bookingService,
            IProfileStatisticsService profileStatisticsService,
            IGeocodingService geocodingService,
            Action<string> showLogin,
            Action exitApplication)
            : this(
                authService,
                userProfileService,
                foodPointService,
                lotService,
                bookingService,
                new DeniedAdminService(),
                profileStatisticsService,
                geocodingService,
                new UnavailableMapService(),
                new UnavailablePartnerAnalyticsService(),
                showLogin,
                exitApplication)
        {
        }

        public HomeViewModel(
            IAuthService authService,
            IUserProfileService userProfileService,
            IFoodPointService foodPointService,
            ILotService lotService,
            IBookingService bookingService,
            IAdminService adminService,
            IProfileStatisticsService profileStatisticsService,
            IPartnerAnalyticsService partnerAnalyticsService,
            Action<string> showLogin,
            Action exitApplication)
            : this(
                authService,
                userProfileService,
                foodPointService,
                lotService,
                bookingService,
                adminService,
                profileStatisticsService,
                new UnavailableGeocodingService(),
                new UnavailableMapService(),
                partnerAnalyticsService,
                showLogin,
                exitApplication)
        {
        }

        public HomeViewModel(
            IAuthService authService,
            IUserProfileService userProfileService,
            IFoodPointService foodPointService,
            ILotService lotService,
            IBookingService bookingService,
            IAdminService adminService,
            IProfileStatisticsService profileStatisticsService,
            IGeocodingService geocodingService,
            IMapService mapService,
            IPartnerAnalyticsService partnerAnalyticsService,
            Action<string> showLogin,
            Action exitApplication)
        {
            _authService = authService;
            _showLogin = showLogin;
            _exitApplication = exitApplication;

            _lotService = lotService;
            _bookingService = bookingService;
            _adminService = adminService;
            _partnerAnalyticsService = partnerAnalyticsService;

            Profile = new UserProfileViewModel(
                userProfileService,
                authService,
                profileStatisticsService,
                NavigateToSection,
                showLogin,
                bookingService);

            Catalog = new CatalogViewModel(
                lotService,
                bookingService,
                OpenCustomerLotDetailsAsync);
            CustomerHome = new CustomerHomeViewModel(
                authService,
                lotService,
                bookingService,
                NavigateToSection,
                OpenCustomerLotDetailsAsync);
            PartnerHome = new PartnerHomeViewModel(
                authService,
                foodPointService,
                lotService,
                bookingService,
                NavigateToSection,
                () =>
                {
                    OpenCreateLot();
                    return Task.CompletedTask;
                },
                OpenCreateFoodPointAsync,
                OpenPartnerLotDetailsAsync,
                OpenFoodPointDetailsAsync);
            AdminHome = new AdminHomeViewModel(
                authService,
                adminService,
                NavigateToSection);
            Map = new MapViewModel(mapService, OpenCatalogForFoodPoint);
            FoodPoint = new FoodPointViewModel(foodPointService, lotService, geocodingService, this);
            PartnerLots = new PartnerLotsViewModel(lotService, foodPointService, this);
            PartnerBookings = new PartnerBookingsViewModel(bookingService);
            CreateLot = new CreateLotViewModel(foodPointService, lotService, this);
            MyBookings = new MyBookingsViewModel(
                bookingService,
                OpenCustomerLotDetailsAsync,
                () => NavigateToSectionAsync(CatalogSection));
            AdminPanel = new AdminPanelViewModel(adminService);

            Analytics = new PartnerAnalyticsViewModel(
                _partnerAnalyticsService,
                foodPointService);

            _currentSectionViewModel = Profile;
        }

        public UserProfileViewModel Profile { get; }

        public CatalogViewModel Catalog { get; }

        public CustomerHomeViewModel CustomerHome { get; }

        public PartnerHomeViewModel PartnerHome { get; }

        public AdminHomeViewModel AdminHome { get; }

        public MapViewModel Map { get; }

        public FoodPointViewModel FoodPoint { get; }

        public PartnerLotsViewModel PartnerLots { get; }

        public PartnerBookingsViewModel PartnerBookings { get; }

        public CreateLotViewModel CreateLot { get; }

        public MyBookingsViewModel MyBookings { get; }

        public AdminPanelViewModel AdminPanel { get; }

        public PartnerAnalyticsViewModel Analytics { get; }

        public bool IsHomeSelected => SelectedSectionKey == HomeSection;

        public bool IsCatalogSelected =>
            SelectedSectionKey == CatalogSection ||
            SelectedSectionKey == PartnerLotsSection;

        public bool IsBookingsSelected =>
            SelectedSectionKey == BookingsSection ||
            SelectedSectionKey == PartnerBookingsSection;

        public bool IsFoodPointsSelected =>
            SelectedSectionKey == FoodPointsSection;

        public bool IsAnalyticsSelected =>
            SelectedSectionKey == AnalyticsSection;

        public bool IsAdminSelected =>
            SelectedSectionKey == AdminSection;

        public bool IsProfileSelected =>
            SelectedSectionKey == ProfileSection;

        public bool IsSettingsSelected =>
            SelectedSectionKey == SettingsSection;

        public double SidebarWidth => IsSidebarExpanded ? 196 : 72;

        public double SidebarLogoWidth => IsSidebarExpanded ? 138 : 0;

        public double SidebarLogoHeight => IsSidebarExpanded ? 58 : 0;

        public string SidebarToggleText => IsSidebarExpanded ? "<" : ">";

        public string BookingsNavigationText =>
            IsPartner ? "Брони клиентов" : "Мои брони";

        public string FoodPointsNavigationText =>
            IsPartner ? "Мои точки" : "Карта";

        public string FoodPointsNavigationIconPath => "/Assets/Icons/location.png";

        public async Task InitializeAsync()
        {
            try
            {
                await Profile.LoadAsync();

                var currentUser = await _authService.GetCurrentUserAsync();

                var role = currentUser?.Role;

                IsPartner = role == UserRole.FoodPointRepresentative;
                IsAdmin = role == UserRole.Administrator;

                IsCatalogVisible = role == UserRole.StudentCustomer;

                IsLotsNavigationVisible =
                    role is UserRole.StudentCustomer or UserRole.FoodPointRepresentative;

                IsBookingsVisible =
                    role is UserRole.StudentCustomer or UserRole.FoodPointRepresentative;

                IsAdminVisible = IsAdmin;

                if (IsPartner)
                {
                    await FoodPoint.LoadAsync();
                    await PartnerLots.LoadAsync();
                    await CreateLot.RefreshAsync();
                }
                else if (role == UserRole.StudentCustomer)
                {
                    await Catalog.LoadAllAsync();
                }
                await NavigateToSectionAsync(
                    role is UserRole.StudentCustomer or UserRole.FoodPointRepresentative or UserRole.Administrator
                        ? HomeSection
                        : ProfileSection);
            }
            catch (Exception ex)
            {
                Profile.StatusMessage =
                    ExceptionMessageFormatter.ToUserMessage(ex);

                CurrentSectionViewModel = Profile;
            }
        }

        [RelayCommand]
        private Task ShowHomeAsync() =>
            NavigateToSectionAsync(HomeSection);

        [RelayCommand]
        private Task ShowCatalogAsync() =>
            NavigateToSectionAsync(CatalogSection);

        [RelayCommand]
        private Task ShowBookingsAsync() =>
            NavigateToSectionAsync(
                IsPartner
                    ? PartnerBookingsSection
                    : BookingsSection);

        [RelayCommand]
        private Task ShowPartnerBookingsAsync() =>
            NavigateToSectionAsync(PartnerBookingsSection);

        [RelayCommand]
        private Task ShowFavoritesAsync() => Task.CompletedTask;

        [RelayCommand]
        private Task ShowFoodPointsAsync() =>
            NavigateToSectionAsync(FoodPointsSection);

        [RelayCommand]
        private Task ShowAnalyticsAsync() =>
            NavigateToSectionAsync(AnalyticsSection);

        [RelayCommand]
        private Task ShowAdminAsync() =>
            NavigateToSectionAsync(AdminSection);

        [RelayCommand]
        private Task ShowProfileAsync() =>
            NavigateToSectionAsync(ProfileSection);

        [RelayCommand]
        private Task ShowSettingsAsync() => Task.CompletedTask;

        [RelayCommand]
        private void ToggleSidebar()
        {
            IsSidebarExpanded = !IsSidebarExpanded;
        }

        [RelayCommand]
        private void Logout()
        {
            IsApplicationExitConfirmationOpen = true;
        }

        [RelayCommand]
        private void CancelApplicationExit()
        {
            IsApplicationExitConfirmationOpen = false;
        }

        [RelayCommand]
        private void ConfirmApplicationExit()
        {
            IsApplicationExitConfirmationOpen = false;
            _exitApplication();
        }

        public async Task RefreshPartnerAsync()
        {
            await FoodPoint.LoadAsync();
            await PartnerLots.LoadAsync();
            await CreateLot.RefreshAsync();
            await PartnerHome.LoadAsync();
            await Profile.LoadStatisticsAsync();
        }

        public async void OpenLotEditor(Guid lotId)
        {
            try
            {
                await CreateLot.LoadForEditAsync(lotId);

                SetSection(CreateLotSection, CreateLot);
            }
            catch (Exception ex)
            {
                Profile.StatusMessage =
                    ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }

        public async void OpenCreateLot(Guid? foodPointId = null)
        {
            try
            {
                await CreateLot.PrepareCreateAsync(foodPointId);

                SetSection(CreateLotSection, CreateLot);
            }
            catch (Exception ex)
            {
                Profile.StatusMessage =
                    ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }

        public Task OpenPartnerLotsAsync()
        {
            return NavigateToSectionAsync(PartnerLotsSection);
        }

        public async Task OpenCreateFoodPointAsync()
        {
            try
            {
                await FoodPoint.LoadAsync();
                FoodPoint.NewFoodPointCommand.Execute(null);
                SetSection(FoodPointsSection, FoodPoint);
            }
            catch (Exception ex)
            {
                Profile.StatusMessage =
                    ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }

        public async Task OpenPartnerLotDetailsAsync(Guid lotId)
        {
            try
            {
                await PartnerLots.LoadAsync();
                await PartnerLots.OpenDetailsByIdAsync(lotId);
                SetSection(PartnerLotsSection, PartnerLots);
            }
            catch (Exception ex)
            {
                Profile.StatusMessage =
                    ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }

        public async Task OpenFoodPointDetailsAsync(Guid foodPointId)
        {
            try
            {
                await FoodPoint.OpenDetailsByIdAsync(foodPointId);
                SetSection(FoodPointsSection, FoodPoint);
            }
            catch (Exception ex)
            {
                Profile.StatusMessage =
                    ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }

        private async Task OpenCustomerLotDetailsAsync(Guid lotId)
        {
            try
            {
                var details = new CustomerLotDetailsViewModel(
                    lotId,
                    _lotService,
                    _bookingService,
                    async () =>
                    {
                        await Catalog.LoadAsync();
                        SetSection(CatalogSection, Catalog);
                    });

                await details.LoadAsync();
                SetSection(CatalogSection, details);
            }
            catch (Exception ex)
            {
                CustomerHome.StatusMessage =
                    ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }

        private async void OpenCatalogForFoodPoint(Guid foodPointId)
        {
            try
            {
                await Catalog.LoadForFoodPointAsync(foodPointId);
                SetSection(CatalogSection, Catalog);
            }
            catch (Exception ex)
            {
                Map.StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }

        private void NavigateToSection(string sectionKey)
        {
            _ = NavigateToSectionAsync(sectionKey);
        }

        private async Task NavigateToSectionAsync(string sectionKey)
        {
            try
            {
                switch (sectionKey)
                {
                    case HomeSection:

                        if (IsCatalogVisible)
                        {
                            await CustomerHome.LoadAsync();
                            SetSection(HomeSection, CustomerHome);
                        }
                        else if (IsPartner)
                        {
                            await PartnerHome.LoadAsync();
                            SetSection(HomeSection, PartnerHome);
                        }
                        else if (IsAdmin)
                        {
                            await AdminHome.LoadAsync();
                            SetSection(HomeSection, AdminHome);
                        }
                        else
                        {
                            await Profile.LoadAsync();
                            SetSection(ProfileSection, Profile);
                        }

                        break;

                    case CatalogSection when IsCatalogVisible:
                        await Catalog.LoadAllAsync();
                        SetSection(CatalogSection, Catalog);

                        break;

                    case CatalogSection:
                    case PartnerLotsSection:

                        await PartnerLots.LoadAsync();

                        SetSection(PartnerLotsSection, PartnerLots);

                        break;

                    case BookingsSection:

                        await MyBookings.LoadAsync();

                        SetSection(BookingsSection, MyBookings);

                        break;

                    case FavoritesSection:
                        break;

                    case PartnerBookingsSection:

                        await PartnerBookings.LoadAsync();

                        SetSection(
                            PartnerBookingsSection,
                            PartnerBookings);

                        break;

                    case FoodPointsSection:

                        if (IsPartner)
                        {
                            await FoodPoint.LoadAsync();

                            SetSection(
                                FoodPointsSection,
                                FoodPoint);
                        }
                        else
                        {
                            await Map.LoadAsync();
                            SetSection(FoodPointsSection, Map);
                        }

                        break;

                    case AnalyticsSection when IsPartner:

                        await Analytics.LoadAsync();

                        SetSection(
                            AnalyticsSection,
                            Analytics);

                        break;

                    case AnalyticsSection:

                        Profile.StatusMessage =
                            "Аналитика доступна только партнеру.";

                        await Profile.LoadAsync();

                        SetSection(
                            ProfileSection,
                            Profile);

                        break;

                    case AdminSection:

                        await AdminPanel.InitializeAsync();

                        SetSection(
                            AdminSection,
                            AdminPanel);

                        break;

                    case SettingsSection:
                        break;

                    default:

                        await Profile.LoadAsync();

                        SetSection(ProfileSection, Profile);

                        break;
                }
            }
            catch (Exception ex)
            {
                Profile.StatusMessage =
                    ExceptionMessageFormatter.ToUserMessage(ex);

                SetSection(ProfileSection, Profile);
            }
        }

        private void SetSection(
            string sectionKey,
            ViewModelBase viewModel)
        {
            SelectedSectionKey = sectionKey;
            CurrentSectionViewModel = viewModel;
        }

        partial void OnSelectedSectionKeyChanged(string value)
        {
            OnPropertyChanged(nameof(IsHomeSelected));
            OnPropertyChanged(nameof(IsCatalogSelected));
            OnPropertyChanged(nameof(IsBookingsSelected));
            OnPropertyChanged(nameof(IsFoodPointsSelected));
            OnPropertyChanged(nameof(IsAnalyticsSelected));
            OnPropertyChanged(nameof(IsAdminSelected));
            OnPropertyChanged(nameof(IsProfileSelected));
            OnPropertyChanged(nameof(IsSettingsSelected));
        }

        partial void OnIsSidebarExpandedChanged(bool value)
        {
            OnPropertyChanged(nameof(SidebarWidth));
            OnPropertyChanged(nameof(SidebarLogoWidth));
            OnPropertyChanged(nameof(SidebarLogoHeight));
            OnPropertyChanged(nameof(SidebarToggleText));
        }

        partial void OnIsPartnerChanged(bool value)
        {
            OnPropertyChanged(nameof(BookingsNavigationText));
            OnPropertyChanged(nameof(FoodPointsNavigationText));
            OnPropertyChanged(nameof(FoodPointsNavigationIconPath));
        }

        private sealed class DeniedAdminService : IAdminService
        {
            public Task EnsureAdministratorAccessAsync(
                CancellationToken cancellationToken = default)
            {
                throw new UnauthorizedAccessException(
                    "Административный модуль доступен только администратору.");
            }

            public Task<List<AdminUserDto>> GetUsersAsync(
                CancellationToken cancellationToken = default)
            {
                throw new UnauthorizedAccessException(
                    "Административный модуль доступен только администратору.");
            }

            public Task ActivateUserAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
            {
                throw new UnauthorizedAccessException(
                    "Административный модуль доступен только администратору.");
            }

            public Task DeactivateUserAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
            {
                throw new UnauthorizedAccessException(
                    "Административный модуль доступен только администратору.");
            }

            public Task<List<AdminFoodPointDto>> GetFoodPointsAsync(
                CancellationToken cancellationToken = default)
            {
                throw new UnauthorizedAccessException(
                    "Административный модуль доступен только администратору.");
            }

            public Task ActivateFoodPointAsync(
                Guid foodPointId,
                CancellationToken cancellationToken = default)
            {
                throw new UnauthorizedAccessException(
                    "Административный модуль доступен только администратору.");
            }

            public Task DeactivateFoodPointAsync(
                Guid foodPointId,
                CancellationToken cancellationToken = default)
            {
                throw new UnauthorizedAccessException(
                    "Административный модуль доступен только администратору.");
            }

            public Task<List<AdminLotDto>> GetLotsAsync(
                CancellationToken cancellationToken = default)
            {
                throw new UnauthorizedAccessException(
                    "Административный модуль доступен только администратору.");
            }

            public Task CancelLotAsync(
                Guid lotId,
                CancellationToken cancellationToken = default)
            {
                throw new UnauthorizedAccessException(
                    "Административный модуль доступен только администратору.");
            }

            public Task<List<AdminBookingDto>> GetBookingsAsync(
                CancellationToken cancellationToken = default)
            {
                throw new UnauthorizedAccessException(
                    "Административный модуль доступен только администратору.");
            }
        }

        private sealed class UnavailableGeocodingService : IGeocodingService
        {
            public Task<Application.DTOs.Maps.GeocodingResultDto?> GeocodeAddressAsync(
                string address,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<Application.DTOs.Maps.GeocodingResultDto?>(null);
            }

            public Task<Application.DTOs.Maps.GeocodingResultDto?> ReverseGeocodeAsync(
                Application.DTOs.Maps.CoordinatesDto coordinates,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<Application.DTOs.Maps.GeocodingResultDto?>(null);
            }
        }

        private sealed class UnavailableMapService : IMapService
        {
            public Task<IReadOnlyList<Application.DTOs.Maps.MapFoodPointDto>> GetFoodPointsForMapAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<Application.DTOs.Maps.MapFoodPointDto>>(
                    Array.Empty<Application.DTOs.Maps.MapFoodPointDto>());
            }

            public Task<IReadOnlyList<Application.DTOs.Maps.NearestFoodPointDto>> FindNearestFoodPointsAsync(
                string address,
                int maxResults = 5,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<Application.DTOs.Maps.NearestFoodPointDto>>(
                    Array.Empty<Application.DTOs.Maps.NearestFoodPointDto>());
            }

            public double CalculateDistanceKm(
                Application.DTOs.Maps.CoordinatesDto first,
                Application.DTOs.Maps.CoordinatesDto second)
            {
                return 0;
            }
        }

        private sealed class UnavailablePartnerAnalyticsService : IPartnerAnalyticsService
        {
            public Task<PartnerAnalyticsDashboardDto> GetDashboardAsync(
                AnalyticsPeriod period,
                Guid? foodPointId = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromException<PartnerAnalyticsDashboardDto>(
                    new InvalidOperationException(
                        "Сервис аналитики не зарегистрирован."));
            }
        }
    }
}
