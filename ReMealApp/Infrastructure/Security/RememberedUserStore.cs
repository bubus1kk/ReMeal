using Application.Interfaces;
using Infrastructure.Persistence;

namespace Infrastructure.Security
{
    public sealed class RememberedUserStore : IRememberedUserStore
    {
        private readonly string _filePath;

        public RememberedUserStore(string filePath)
        {
            _filePath = filePath;
        }

        public static RememberedUserStore CreateDefault()
        {
            return DataAccessGuard.Execute(() =>
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var directory = Path.Combine(appData, "ReMeal");

                Directory.CreateDirectory(directory);

                return new RememberedUserStore(Path.Combine(directory, "remembered-user.txt"));
            }, "подготовить хранилище автологина");
        }

        public Guid? GetRememberedUserId()
        {
            return DataAccessGuard.Execute<Guid?>(() =>
            {
                if (!File.Exists(_filePath))
                    return null;

                var text = File.ReadAllText(_filePath).Trim();
                return Guid.TryParse(text, out var userId) ? userId : null;
            }, "прочитать сохраненного пользователя");
        }

        public void RememberUser(Guid userId)
        {
            DataAccessGuard.Execute(() =>
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(_filePath, userId.ToString());
            }, "сохранить автологин");
        }

        public void ForgetUser()
        {
            DataAccessGuard.Execute(() =>
            {
                if (File.Exists(_filePath))
                    File.Delete(_filePath);
            }, "удалить автологин");
        }
    }
}
