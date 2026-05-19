namespace Tests.TestSupport
{
    internal static class AssertEx
    {
        public static async Task<TException> ThrowsAsync<TException>(Func<Task> action)
            where TException : Exception
        {
            try
            {
                await action();
            }
            catch (TException exception)
            {
                return exception;
            }

            Assert.Fail($"Ожидалось исключение {typeof(TException).Name}.");
            throw new InvalidOperationException("Недостижимая ветка после Assert.Fail.");
        }
    }
}
