using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Entities;
using Domain.Enums;
using System.Globalization;

namespace ReMealApp.ViewModels.Partner
{
    public partial class PartnerLotListItemViewModel : ObservableObject
    {
        public PartnerLotListItemViewModel(FoodLot lot)
        {
            Lot = lot;
        }

        public FoodLot Lot { get; }

        public Guid Id => Lot.Id;

        public string Title => Lot.Title;

        public string Subtitle => LotDisplayFormatting.FirstFilled(
            Lot.Description,
            Lot.Composition,
            "Описание не добавлено");

        public string DescriptionPreviewSource => string.Join(" ", new[]
        {
            Lot.Description,
            Lot.Composition
        }.Where(x => !string.IsNullOrWhiteSpace(x)));

        public string FoodPointName => Lot.FoodPoint?.Name ?? "Не указано";

        public string FoodPointAddress => Lot.FoodPoint?.Address ?? "Не указано";

        public string? ImagePath => Lot.ImagePath;

        public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

        public string PickupDateText => LotDisplayFormatting.FormatPickupDate(Lot.PickupDeadline);

        public string PickupTimeText => LotDisplayFormatting.FormatPickupTime(Lot.PickupDeadline);

        public int AvailableQuantity => Lot.AvailableQuantity;

        public int TotalQuantity => Lot.TotalQuantity;

        public string PriceText => LotDisplayFormatting.FormatPrice(Lot.Price);

        public string StatusText => LotDisplayFormatting.GetVisualStatusText(Lot);

        public bool IsUnavailable => Lot.AvailableQuantity <= 0;

        public IBrush QuantityBrush => IsUnavailable
            ? LotDisplayFormatting.DangerBrush
            : LotDisplayFormatting.TextBrush;

        public IBrush StatusBadgeBackground => LotDisplayFormatting.GetStatusBackground(Lot);

        public IBrush StatusBadgeForeground => LotDisplayFormatting.GetStatusForeground(Lot);

        public IBrush StatusDotBrush => LotDisplayFormatting.GetStatusAccent(Lot);

        public bool CanEdit => Lot.Status is not LotStatus.Cancelled and not LotStatus.Expired;

        public bool CanCancel => Lot.Status is not LotStatus.Cancelled and not LotStatus.Expired;

        public bool IsAvailableForSale => LotDisplayFormatting.IsAvailableForSale(Lot);

        public bool IsEndingToday => LotDisplayFormatting.IsEndingToday(Lot);

        [ObservableProperty]
        private bool _isSelected;
    }

    public sealed class FoodPointFilterOption
    {
        public FoodPointFilterOption(Guid? id, string name)
        {
            Id = id;
            Name = name;
        }

        public Guid? Id { get; }

        public string Name { get; }
    }

    public sealed class LotStatusFilterOption
    {
        public LotStatusFilterOption(LotStatusFilterKind kind, string name)
        {
            Kind = kind;
            Name = name;
        }

        public LotStatusFilterKind Kind { get; }

        public string Name { get; }
    }

    public enum LotStatusFilterKind
    {
        All,
        Active,
        EndingToday,
        SoldOut,
        Expired,
        Cancelled
    }

    internal static class LotDisplayFormatting
    {
        private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

        public static readonly IBrush TextBrush = SolidColorBrush.Parse("#F4F7F3");
        public static readonly IBrush MutedTextBrush = SolidColorBrush.Parse("#A8B1AD");
        public static readonly IBrush GreenBrush = SolidColorBrush.Parse("#78D957");
        public static readonly IBrush GreenSoftBrush = SolidColorBrush.Parse("#1D3825");
        public static readonly IBrush WarningBrush = SolidColorBrush.Parse("#FFB238");
        public static readonly IBrush WarningSoftBrush = SolidColorBrush.Parse("#3B2D16");
        public static readonly IBrush DangerBrush = SolidColorBrush.Parse("#FF705F");
        public static readonly IBrush DangerSoftBrush = SolidColorBrush.Parse("#3A1E1C");
        public static readonly IBrush NeutralBrush = SolidColorBrush.Parse("#94A0AA");
        public static readonly IBrush NeutralSoftBrush = SolidColorBrush.Parse("#242C33");

        public static string FirstFilled(string? first, string? second, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(first))
                return TrimPreview(first);

            if (!string.IsNullOrWhiteSpace(second))
                return TrimPreview(second);

            return fallback;
        }

        public static string FormatPrice(decimal price)
        {
            var format = decimal.Truncate(price) == price ? "0" : "0.00";
            return $"{price.ToString(format, RussianCulture)} ₽";
        }

        public static string FormatPickupDate(DateTime deadlineUtc)
        {
            var local = deadlineUtc.ToLocalTime();
            var today = DateTime.Today;

            if (local.Date == today)
                return "Сегодня";

            if (local.Date == today.AddDays(1))
                return "Завтра";

            return local.ToString("d MMMM", RussianCulture);
        }

        public static string FormatPickupTime(DateTime deadlineUtc)
        {
            var local = deadlineUtc.ToLocalTime();
            return $"до {local:HH:mm}";
        }

        public static string FormatFullDateTime(DateTime dateTimeUtc)
        {
            if (dateTimeUtc == default)
                return "Не указано";

            return dateTimeUtc.ToLocalTime().ToString("dd.MM.yyyy, HH:mm", RussianCulture);
        }

        public static string GetStatusText(LotStatus status)
        {
            return status switch
            {
                LotStatus.Active => "Активен",
                LotStatus.SoldOut => "Распродан",
                LotStatus.Expired => "Просрочен",
                LotStatus.Cancelled => "Снят с публикации",
                _ => status.ToString()
            };
        }

        public static string GetVisualStatusText(FoodLot lot)
        {
            return IsEndingToday(lot)
                ? "Скоро завершится"
                : GetStatusText(lot.Status);
        }

        public static bool IsAvailableForSale(FoodLot lot)
        {
            return lot.Status == LotStatus.Active &&
                lot.AvailableQuantity > 0 &&
                lot.PickupDeadline > DateTime.UtcNow &&
                lot.FoodPoint is null or { IsActive: true };
        }

        public static bool IsEndingToday(FoodLot lot)
        {
            if (lot.Status != LotStatus.Active || lot.PickupDeadline <= DateTime.UtcNow)
                return false;

            return lot.PickupDeadline.ToLocalTime().Date == DateTime.Today;
        }

        public static IBrush GetStatusAccent(FoodLot lot)
        {
            if (IsEndingToday(lot))
                return WarningBrush;

            return lot.Status switch
            {
                LotStatus.Active => GreenBrush,
                LotStatus.SoldOut => WarningBrush,
                LotStatus.Expired => NeutralBrush,
                LotStatus.Cancelled => NeutralBrush,
                _ => NeutralBrush
            };
        }

        public static IBrush GetStatusBackground(FoodLot lot)
        {
            if (IsEndingToday(lot))
                return WarningSoftBrush;

            return lot.Status switch
            {
                LotStatus.Active => GreenSoftBrush,
                LotStatus.SoldOut => WarningSoftBrush,
                LotStatus.Expired => NeutralSoftBrush,
                LotStatus.Cancelled => NeutralSoftBrush,
                _ => NeutralSoftBrush
            };
        }

        public static IBrush GetStatusForeground(FoodLot lot)
        {
            if (IsEndingToday(lot))
                return WarningBrush;

            return lot.Status switch
            {
                LotStatus.Active => GreenBrush,
                LotStatus.SoldOut => WarningBrush,
                LotStatus.Expired => NeutralBrush,
                LotStatus.Cancelled => NeutralBrush,
                _ => NeutralBrush
            };
        }

        private static string TrimPreview(string value)
        {
            var normalized = value.ReplaceLineEndings(" ").Trim();
            return normalized.Length <= 80
                ? normalized
                : normalized[..77] + "...";
        }
    }
}
