using Avalonia.Controls;
using ReMeal.Desktop.ViewModels;

namespace ReMeal.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
            await viewModel.InitializeAsync();
    }
}
