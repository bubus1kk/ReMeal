using Application.DTOs.Admin;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using ReMealApp.ViewModels.Admin;
using ReMealApp.ViewModels.Booking;
using ReMealApp.ViewModels.Catalog;
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
            Action<string> showLogin,
            Action exitApplication)
        {
            _authService = authService;
            _showLogin = showLogin;
            _exitApplication = exitApplication;

            Profile = new UserProfileViewModel(
                userProfileService,
                authService,
                profileStatisticsService,
                NavigateToSection,
                showLogin);

            Catalog = new CatalogViewModel(lotService, bookingService);
            FoodPoint = new FoodPointViewModel(foodPointService, lotService, this);
            PartnerLots = new PartnerLotsViewModel(lotService, foodPointService, this);
            PartnerBookings = new PartnerBookingsViewModel(bookingService);
            CreateLot = new CreateLotViewModel(foodPointService, lotService, this);
            MyBookings = new MyBookingsViewModel(bookingService);
            AdminPanel = new AdminPanelViewModel(adminService);
            _currentSectionViewModel = Profile;
        }

        public UserProfileViewModel Profile { get; }

        public CatalogViewModel Catalog { get; }

        public FoodPointViewModel FoodPoint { get; }

        public PartnerLotsViewModel PartnerLots { get; }

        public PartnerBookingsViewModel PartnerBookings { get; }

        public CreateLotViewModel CreateLot { get; }

        public MyBookingsViewModel MyBookings { get; }

        public AdminPanelViewModel AdminPanel { get; }

        public bool IsHomeSelected => SelectedSectionKey == HomeSection;

        public bool IsCatalogSelected => SelectedSectionKey == CatalogSection || SelectedSectionKey == PartnerLotsSection;

        public bool IsBookingsSelected => SelectedSectionKey == BookingsSection || SelectedSectionKey == PartnerBookingsSection;

        public bool IsFoodPointsSelected => SelectedSectionKey == FoodPointsSection;

        public bool IsAnalyticsSelected => SelectedSectionKey == AnalyticsSection;

        public bool IsAdminSelected => SelectedSectionKey == AdminSection;

        public bool IsProfileSelected => SelectedSectionKey == ProfileSection;

        public bool IsSettingsSelected => SelectedSectionKey == SettingsSection;

        public double SidebarWidth => IsSidebarExpanded ? 196 : 72;

        public double SidebarLogoWidth => IsSidebarExpanded ? 138 : 0;

        public double SidebarLogoHeight => IsSidebarExpanded ? 58 : 0;

        public string SidebarToggleText => IsSidebarExpanded ? "<" : ">";

        public string BookingsNavigationText => IsPartner ? "Брони клиентов" : "Мои брони";

        public string FoodPointsNavigationText => IsPartner ? "Мои точки" : "Партнёры";

        public string FoodPointsNavigationIconPath => IsPartner ? "/Assets/Icons/location.png" : "/Assets/Icons/partners.png";

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
                IsLotsNavigationVisible = role is UserRole.StudentCustomer or UserRole.FoodPointRepresentative;
                IsBookingsVisible = role is UserRole.StudentCustomer or UserRole.FoodPointRepresentative;
                IsAdminVisible = IsAdmin;

                if (IsPartner)
                {
                    await FoodPoint.LoadAsync();
                    await PartnerLots.LoadAsync();
                    await CreateLot.RefreshAsync();
                }
                else if (role == UserRole.StudentCustomer)
                {
                    await Catalog.LoadAsync();
                }
                else if (IsAdmin)
                {
                    await AdminPanel.InitializeAsync();
                }

                await NavigateToSectionAsync(ProfileSection);
            }
            catch (Exception ex)
            {
                Profile.StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
                CurrentSectionViewModel = Profile;
            }
        }

        [RelayCommand]
        private Task ShowHomeAsync() => NavigateToSectionAsync(HomeSection);

        [RelayCommand]
        private Task ShowCatalogAsync() => NavigateToSectionAsync(CatalogSection);

        [RelayCommand]
        private Task ShowBookingsAsync() => NavigateToSectionAsync(IsPartner ? PartnerBookingsSection : BookingsSection);

        [RelayCommand]
        private Task ShowPartnerBookingsAsync() => NavigateToSectionAsync(PartnerBookingsSection);

        [RelayCommand]
        private Task ShowFavoritesAsync() => NavigateToSectionAsync(FavoritesSection);

        [RelayCommand]
        private Task ShowFoodPointsAsync() => NavigateToSectionAsync(FoodPointsSection);

        [RelayCommand]
        private Task ShowAnalyticsAsync() => NavigateToSectionAsync(AnalyticsSection);

        [RelayCommand]
        private Task ShowAdminAsync() => NavigateToSectionAsync(AdminSection);

        [RelayCommand]
        private Task ShowProfileAsync() => NavigateToSectionAsync(ProfileSection);

        [RelayCommand]
        private Task ShowSettingsAsync() => NavigateToSectionAsync(SettingsSection);

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
                Profile.StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
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
                Profile.StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }

        public Task OpenPartnerLotsAsync()
        {
            return NavigateToSectionAsync(PartnerLotsSection);
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
                        SetSection(
                            HomeSection,
                            new ModulePlaceholderViewModel(
                                "Главная",
                                "Основной экран будет развиваться отдельно. Сейчас доступные рабочие разделы открываются из боковой навигации.",
                                "/Assets/Icons/home.png"));
                        break;

                    case CatalogSection when IsCatalogVisible:
                        await Catalog.LoadAsync();
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
                        SetSection(
                            FavoritesSection,
                            new ModulePlaceholderViewModel(
                                "Избранное",
                                "Избранные наборы появятся после добавления соответствующей модели и сервиса.",
                                "/Assets/Icons/favorites.png"));
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
                            SetSection(FoodPointsSection, FoodPoint);
                        }
                        else
                        {
                            SetSection(
                                FoodPointsSection,
                                new ModulePlaceholderViewModel(
                                    "Партнеры",
                                    "Раздел партнеров пока не имеет отдельного пользовательского экрана.",
                                    "/Assets/Icons/partners.png"));
                        }

                        break;

                    case AnalyticsSection:
                        SetSection(
                            AnalyticsSection,
                            new ModulePlaceholderViewModel(
                                "Аналитика",
                                "Расширенная аналитика будет добавлена отдельным модулем. В профиле отображаются только показатели, которые уже можно посчитать по текущей базе.",
                                "/Assets/Icons/analytics.png"));
                        break;

                    case AdminSection:
                        await AdminPanel.InitializeAsync();
                        SetSection(AdminSection, AdminPanel);
                        break;

                    case SettingsSection:
                        SetSection(
                            SettingsSection,
                            new ModulePlaceholderViewModel(
                                "Настройки",
                                "Настройки профиля сейчас доступны через модальное окно редактирования.",
                                "/Assets/Icons/settings.png"));
                        break;

                    default:
                        await Profile.LoadAsync();
                        SetSection(ProfileSection, Profile);
                        break;
                }
            }
            catch (Exception ex)
            {
                Profile.StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
                SetSection(ProfileSection, Profile);
            }
        }

        private void SetSection(string sectionKey, ViewModelBase viewModel)
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
            public Task EnsureAdministratorAccessAsync(CancellationToken cancellationToken = default)
            {
                throw new UnauthorizedAccessException("Административный модуль доступен только администратору.");
            }

            public Task<List<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
            {
                throw new UnauthorizedAccessException("Административный модуль доступен только администратору.");
            }

            public Task<List<AdminFoodPointDto>> GetFoodPointsAsync(CancellationToken cancellationToken = default)
            {
                throw new UnauthorizedAccessException("Административный модуль доступен только администратору.");
            }

            public Task ActivateFoodPointAsync(Guid foodPointId, CancellationToken cancellationToken = default)
            {
                throw new UnauthorizedAccessException("Административный модуль доступен только администратору.");
            }

            public Task DeactivateFoodPointAsync(Guid foodPointId, CancellationToken cancellationToken = default)
            {
                throw new UnauthorizedAccessException("Административный модуль доступен только администратору.");
            }
        }
    }
}
