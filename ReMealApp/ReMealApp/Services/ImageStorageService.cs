namespace ReMealApp.Services
{
    public static class ImageStorageService
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png"
        };

        public static async Task<string> SaveImageAsync(
            string sourcePath,
            string category,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentException("Не выбран файл изображения.", nameof(sourcePath));

            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Файл изображения не найден.", sourcePath);

            var extension = Path.GetExtension(sourcePath);
            if (!AllowedExtensions.Contains(extension))
                throw new InvalidOperationException("Поддерживаются только изображения JPG и PNG.");

            var uploadsDirectory = GetUploadsDirectory(category);
            Directory.CreateDirectory(uploadsDirectory);

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var targetPath = Path.Combine(uploadsDirectory, fileName);

            await using var source = File.OpenRead(sourcePath);
            await using var target = File.Create(targetPath);
            await source.CopyToAsync(target, cancellationToken);

            return targetPath;
        }

        private static string GetUploadsDirectory(string category)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appData, "ReMeal", "Uploads", category);
        }
    }
}
