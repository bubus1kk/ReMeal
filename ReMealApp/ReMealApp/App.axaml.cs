using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Infrastructure;
using ReMealApp.ViewModels;
using ReMealApp.Views;
using System.Diagnostics;

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
            RegisterUnhandledExceptionHandlers();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                try
                {
                    var userModule = ReMealUserModule.CreateDefault();
                    var mainWindowViewModel = new MainWindowViewModel(
                        userModule.AuthService,
                        userModule.UserProfileService,
                        userModule.FoodPointService,
                        userModule.LotService);

                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = mainWindowViewModel,
                    };

                    _ = mainWindowViewModel.InitializeAsync();
                }
                catch (Exception ex)
                {
                    desktop.MainWindow = CreateStartupErrorWindow(ex);
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static void RegisterUnhandledExceptionHandlers()
        {
            Dispatcher.UIThread.UnhandledException += (_, args) =>
            {
                Trace.TraceError(args.Exception.ToString());
                args.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                Trace.TraceError(args.Exception.ToString());
                args.SetObserved();
            };
        }

        private static MainWindow CreateStartupErrorWindow(Exception exception)
        {
            return new MainWindow
            {
                Content = new Border
                {
                    Background = SolidColorBrush.Parse("#0D1513"),
                    Padding = new Thickness(36),
                    Child = new StackPanel
                    {
                        Width = 760,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Spacing = 12,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "ReMeal не смог открыть базу данных",
                                Foreground = SolidColorBrush.Parse("#F7FBF4"),
                                FontSize = 28,
                                FontWeight = FontWeight.SemiBold,
                                TextWrapping = TextWrapping.Wrap
                            },
                            new TextBlock
                            {
                                Text = ExceptionMessageFormatter.ToUserMessage(exception),
                                Foreground = SolidColorBrush.Parse("#FFB7B7"),
                                FontSize = 16,
                                LineHeight = 24,
                                TextWrapping = TextWrapping.Wrap
                            },
                            new TextBlock
                            {
                                Text = "Проверьте, что файл базы данных доступен для записи и не открыт эксклюзивно другим инструментом.",
                                Foreground = SolidColorBrush.Parse("#AEB9B1"),
                                FontSize = 14,
                                LineHeight = 22,
                                TextWrapping = TextWrapping.Wrap
                            }
                        }
                    }
                }
            };
        }
    }
}
