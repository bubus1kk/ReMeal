using Application.Exceptions;
using System.Diagnostics;

namespace ReMealApp.ViewModels
{
    internal static class ExceptionMessageFormatter
    {
        public static string ToUserMessage(Exception exception)
        {
            Trace.TraceError(exception.ToString());

            return exception switch
            {
                DataAccessException => exception.Message,
                OperationCanceledException => "Операция была отменена.",
                ArgumentException => exception.Message,
                InvalidOperationException => exception.Message,
                KeyNotFoundException => exception.Message,
                UnauthorizedAccessException => exception.Message,
                _ => "Произошла непредвиденная ошибка. Попробуйте повторить операцию."
            };
        }
    }
}
