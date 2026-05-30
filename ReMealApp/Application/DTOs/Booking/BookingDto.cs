using System.Globalization;
using Domain.Enums;

namespace Application.DTOs.Booking;

public class BookingDto
{
    private static readonly CultureInfo RussianCulture =
        CultureInfo.GetCultureInfo("ru-RU");

    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string UserLogin { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public string LotTitle { get; set; } = string.Empty;

    public string FoodPointName { get; set; } = string.Empty;

    public string FoodPointAddress { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal PriceAtReservation { get; set; }

    public decimal TotalPrice => PriceAtReservation * Quantity;

    public DateTime ReservedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? IssuedAt { get; set; }

    public BookingStatus Status { get; set; }

    public bool IsIssued { get; set; }

    public bool IsPending { get; set; }

    public bool IsCancelled { get; set; }

    public bool CanCancel => Status == BookingStatus.Active;

    public bool CanConfirmIssue => Status == BookingStatus.Active;

    public bool CannotConfirmIssue => !CanConfirmIssue;

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
            : "Точка не указана";

    public string DisplayFoodPointAddress =>
        !string.IsNullOrWhiteSpace(FoodPointAddress)
            ? FoodPointAddress
            : "Адрес не указан";

    public string DisplayQuantity =>
        Quantity > 0
            ? Quantity.ToString(CultureInfo.InvariantCulture)
            : "Не указано";

    public string DisplayTotalPrice =>
        TotalPrice > 0
            ? $"{TotalPrice.ToString("N2", RussianCulture)} ₽"
            : "Цена недоступна";

    public string DisplayReservedDate =>
        ReservedAt == default
            ? "Дата не указана"
            : ReservedAt.ToLocalTime().ToString("dd.MM.yyyy", RussianCulture);

    public string DisplayReservedTime =>
        ReservedAt == default
            ? string.Empty
            : ReservedAt.ToLocalTime().ToString("HH:mm", RussianCulture);

    public string StatusText => Status switch
    {
        BookingStatus.Active => "Активна",
        BookingStatus.Cancelled => "Отменена",
        BookingStatus.Issued => "Выдана",
        _ => Status.ToString()
    };
}
