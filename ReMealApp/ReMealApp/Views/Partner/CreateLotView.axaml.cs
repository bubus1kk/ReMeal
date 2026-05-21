using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ReMealApp.ViewModels;
using ReMealApp.ViewModels.Partner;

namespace ReMealApp.Views.Partner
{
    public partial class CreateLotView : UserControl
    {
        public CreateLotView()
        {
            InitializeComponent();
        }

        private async void SelectLotImage_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CreateLotViewModel viewModel)
                return;

            var path = await PickImagePathAsync();
            if (path is null)
                return;

            try
            {
                await viewModel.SetLotImageFromSourceAsync(path);
            }
            catch (Exception ex)
            {
                viewModel.StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }

        private async void SelectComponentImage_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CreateLotViewModel viewModel ||
                sender is not Control { DataContext: LotComponentEditorViewModel component })
            {
                return;
            }

            var path = await PickImagePathAsync();
            if (path is null)
                return;

            try
            {
                await viewModel.SetComponentImageFromSourceAsync(component, path);
            }
            catch (Exception ex)
            {
                viewModel.StatusMessage = ExceptionMessageFormatter.ToUserMessage(ex);
            }
        }

        private async Task<string?> PickImagePathAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
                return null;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Изображения")
                    {
                        Patterns = ["*.jpg", "*.jpeg", "*.png"]
                    }
                ]
            });

            return files.Count == 0 ? null : files[0].Path.LocalPath;
        }
    }
}
