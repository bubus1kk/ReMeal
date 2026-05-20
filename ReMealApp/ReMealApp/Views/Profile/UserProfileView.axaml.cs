using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ReMealApp.ViewModels.Profile;

namespace ReMealApp.Views.Profile
{
    public partial class UserProfileView : UserControl
    {
        public UserProfileView()
        {
            InitializeComponent();
        }

        private async void ChangeAvatarButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not UserProfileViewModel viewModel)
                return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
                return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите фото профиля",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Изображения")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp" }
                    }
                }
            });

            var sourcePath = files.Count == 0 ? null : files[0].TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(sourcePath))
                await viewModel.ChangeAvatarAsync(sourcePath);
        }
    }
}
