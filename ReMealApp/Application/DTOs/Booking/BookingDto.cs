using System.Globalization;
using Domain.Enums;

namespace Application.DTOs.Booking;

public class BookingDto
{
    private static readonly CultureInfo RussianCulture =
        CultureInfo.GetCultureInfo("ru-RU");

    public Guid Id { get; set; }

    public Guid FoodLotId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string UserLogin { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public string LotTitle { get; set; } = string.Empty;

    public string FoodPointName { get; set; } = string.Empty;

    public string FoodPointAddress { get; set; } = string.Empty;

    public string FoodLotImagePath { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal PriceAtReservation { get; set; }

    public decimal TotalPrice => PriceAtReservation * Quantity;

    public DateTime ReservedAt { get; set; }

    public DateTime PickupDeadline { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? IssuedAt { get; set; }

    public BookingStatus Status { get; set; }

    public bool IsIssued { get; set; }

    public bool IsPending { get; set; }

    public bool IsCancelled { get; set; }

    public bool CanCancel => Status == BookingStatus.Active;

    public bool CanConfirmIssue => Status == BookingStatus.Active;

    public bool CannotConfirmIssue => !CanConfirmIssue;

    public bool HasFoodLotImage =>
        !string.IsNullOrWhiteSpace(FoodLotImagePath);

    public string DisplayUserName =>
        !string.IsNullOrWhiteSpace(UserName)
            ? UserName
            : !string.IsNullOrWhiteSpace(UserLogin)
                ? UserLogin
                : "Имя не указано";

    public string DisplayUserEmail =>
        !string.IsNullOrWhiteSpace(UserEmail)
            ? UserEmail
            : "Email не указан";

    public string UserInitials
    {
        get
        {
            var parts = DisplayUserName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0 || DisplayUserName == "Имя не указано")
                return "?";

            return string.Concat(
                    parts
                        .Take(2)
                        .Select(x => char.ToUpperInvariant(x[0])))
                .PadRight(2, ' ')
                .Trim();
        }
    }

    public string DisplayLotTitle =>
        !string.IsNullOrWhiteSpace(LotTitle)
            ? LotTitle
            : "Название не указано";

    public string DisplayFoodPointName =>
        !string.IsNullOrWhiteSpace(FoodPointName)
            ? FoodPointName
            : "Точка питания не указана";

    public string DisplayFoodPointAddress =>
        !string.IsNullOrWhiteSpace(FoodPointAddress)
            ? FoodPointAddress
            : "Адрес не указан";

    public string DisplayQuantity =>
        Quantity > 0
            ? Quantity.ToString(CultureInfo.InvariantCulture)
            : "Не указано";

    public string DisplayQuantityWithUnit =>
        Quantity > 0
            ? $"{Quantity.ToString(CultureInfo.InvariantCulture)} шт."
            : "Не указано";

    public string DisplayTotalPrice =>
        TotalPrice > 0
            ? $"{TotalPrice.ToString("N2", RussianCulture)} ₽"
            : "Цена недоступна";

    public string DisplayReservationTotal =>
        TotalPrice > 0
            ? $"{TotalPrice.ToString("N2", RussianCulture)} ₽"
            : "Сумма недоступна";

    public string DisplayReservedDate =>
        ReservedAt == default
            ? "Дата брони не указана"
            : ReservedAt.ToLocalTime().ToString("dd.MM.yyyy", RussianCulture);

    public string DisplayReservedTime =>
        ReservedAt == default
            ? string.Empty
            : ReservedAt.ToLocalTime().ToString("HH:mm", RussianCulture);

    public string DisplayPickupDeadlineDate =>
        PickupDeadline == default
            ? "Дедлайн не указан"
            : PickupDeadline.ToLocalTime().ToString("dd.MM.yyyy", RussianCulture);

    public string DisplayPickupDeadlineTime =>
        PickupDeadline == default
            ? string.Empty
            : PickupDeadline.ToLocalTime().ToString("HH:mm", RussianCulture);

    public bool HasPickupDeadlineTime => PickupDeadline != default;

    public string ActiveDeadlineText
    {
        get
        {
            if (Status != BookingStatus.Active || PickupDeadline == default)
                return string.Empty;

            var remaining = PickupDeadline.ToUniversalTime() - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
                return "Срок истёк";

            if (remaining.TotalMinutes < 1)
                return "Осталось меньше минуты";

            if (remaining.TotalDays >= 1)
            {
                var days = (int)Math.Floor(remaining.TotalDays);
                var hours = remaining.Hours;
                return hours > 0
                    ? $"Осталось {days} {Pluralize(days, "день", "дня", "дней")} {hours} ч"
                    : $"Осталось {days} {Pluralize(days, "день", "дня", "дней")}";
            }

            if (remaining.TotalHours >= 1)
            {
                var hours = (int)Math.Floor(remaining.TotalHours);
                var minutes = remaining.Minutes;
                return minutes > 0
                    ? $"Осталось {hours} ч {minutes} мин"
                    : $"Осталось {hours} ч";
            }

            return $"Осталось {Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))} мин";
        }
    }

    public bool HasActiveDeadlineText =>
        !string.IsNullOrWhiteSpace(ActiveDeadlineText);

    public string StatusText => Status switch
    {
        BookingStatus.Active => "Активное",
        BookingStatus.Cancelled => "Отменено",
        BookingStatus.Issued => "Выдано",
        _ => Status.ToString()
    };

    private static string Pluralize(
        int count,
        string singular,
        string few,
        string many)
    {
        var lastTwo = count % 100;
        if (lastTwo is >= 11 and <= 14)
            return many;

        return (count % 10) switch
        {
            1 => singular,
            2 or 3 or 4 => few,
            _ => many
        };
    }
}
