using Avalonia.Data.Converters;
using Domain.Enums;
using System;
using System.Globalization;

namespace ReMealApp.Converters
{
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
                LotStatus.Cancelled => "Отменен",
                _ => status.ToString()
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
