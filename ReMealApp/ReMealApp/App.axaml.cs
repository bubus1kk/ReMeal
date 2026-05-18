using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Infrastructure;
using ReMealApp.ViewModels;
using ReMealApp.Views;

namespace ReMealApp
{
    public partial class App : Avalonia.Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var userModule = ReMealUserModule.CreateDefault();
                var mainWindowViewModel = new MainWindowViewModel(userModule.AuthService, userModule.UserProfileService);

                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainWindowViewModel,
                };

                _ = mainWindowViewModel.InitializeAsync();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
