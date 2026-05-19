using Application.Interfaces;

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
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var directory = Path.Combine(appData, "ReMeal");

            Directory.CreateDirectory(directory);

            return new RememberedUserStore(Path.Combine(directory, "remembered-user.txt"));
        }

        public Guid? GetRememberedUserId()
        {
            if (!File.Exists(_filePath))
                return null;

            var text = File.ReadAllText(_filePath).Trim();
            return Guid.TryParse(text, out var userId) ? userId : null;
        }

        public void RememberUser(Guid userId)
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(_filePath, userId.ToString());
        }

        public void ForgetUser()
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }
    }
}
