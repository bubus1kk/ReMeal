using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Globalization;

namespace ReMealApp.Converters
{
    public sealed class ImagePathToBitmapConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var path = value as string;
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                if (path.StartsWith("/", StringComparison.Ordinal))
                {
                    var uri = new Uri($"avares://ReMealApp{path}");
                    using var stream = AssetLoader.Open(uri);
                    return new Bitmap(stream);
                }

                if (File.Exists(path))
                    return new Bitmap(path);
            }
            catch
            {
                return null;
            }

            return null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
