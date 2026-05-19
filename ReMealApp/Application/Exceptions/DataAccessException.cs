namespace Application.Exceptions
{
    public sealed class DataAccessException : Exception
    {
        public DataAccessException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
