using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ReMealApp.ViewModels.Admin
{
    public partial class AdminPanelViewModel : ViewModelBase
    {
        private readonly IAdminService _adminService;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _hasAccess;

        [ObservableProperty]
        private bool _isBusy;

        public AdminPanelViewModel(IAdminService adminService)
        {
            _adminService = adminService;
        }

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
                await _adminService.EnsureAdministratorAccessAsync();
                HasAccess = true;
                StatusMessage = "Доступ администратора подтвержден. Разделы управления будут добавлены следующими этапами.";
            }
            catch (Exception ex)
            {
                HasAccess = false;
                StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
