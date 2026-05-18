namespace Infrastructure.Persistence
{
    public static class ReMealDatabasePath
    {
        public static string GetDefaultPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var directory = Path.Combine(appData, "ReMeal");

            Directory.CreateDirectory(directory);

            return Path.Combine(directory, "remeal.db");
        }
    }
}
