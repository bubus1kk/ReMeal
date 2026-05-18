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

                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(userModule.AuthService, userModule.UserProfileService),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
