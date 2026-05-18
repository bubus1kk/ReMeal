using Avalonia.Data.Converters;
using ReMeal.Domain.Enums;
using System.Globalization;

namespace ReMeal.Desktop.Converters;

public class LotStatusToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LotStatus status)
            return string.Empty;

        return status switch
        {
            LotStatus.Active => "Активен",
            LotStatus.SoldOut => "Распродан",
            LotStatus.Expired => "Просрочен",
            LotStatus.Cancelled => "Отменён",
            _ => status.ToString()
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
            return null;

        return text.Trim() switch
        {
            "Активен" => LotStatus.Active,
            "Распродан" => LotStatus.SoldOut,
            "Просрочен" => LotStatus.Expired,
            "Отменён" => LotStatus.Cancelled,
            _ => Enum.TryParse<LotStatus>(text, ignoreCase: true, out var parsed) ? parsed : null
        };
    }
}
