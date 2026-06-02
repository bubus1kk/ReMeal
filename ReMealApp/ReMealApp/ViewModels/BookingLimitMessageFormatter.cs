namespace ReMealApp.ViewModels;

internal static class BookingLimitMessageFormatter
{
    private const string ActiveBookingLimitMarker = "5 активных бронирований";

    public static bool TryFormat(string message, out string dialogMessage)
    {
        if (message.Contains(ActiveBookingLimitMarker, StringComparison.CurrentCultureIgnoreCase))
        {
            dialogMessage = "У вас уже есть 5 активных бронирований.";
            return true;
        }

        dialogMessage = string.Empty;
        return false;
    }
}
