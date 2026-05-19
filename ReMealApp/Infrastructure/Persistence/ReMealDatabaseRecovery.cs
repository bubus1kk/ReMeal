using Application.Exceptions;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO;

namespace Infrastructure.Persistence
{
    public static class ReMealDatabaseRecovery
    {
        private const int SqliteCorrupt = 11;
        private const int SqliteNotADatabase = 26;

        public static bool CanRecover(Exception exception)
        {
            var sqliteException = GetSqliteException(exception);
            if (sqliteException is null)
                return false;

            return sqliteException.SqliteErrorCode is SqliteCorrupt or SqliteNotADatabase;
        }

        public static string MoveDatabaseToBackup(string databasePath)
        {
            if (!File.Exists(databasePath))
                return string.Empty;

            var directory = Path.GetDirectoryName(databasePath)
                ?? throw new DataAccessException("Не удалось определить папку локальной базы данных.");
            var fileName = Path.GetFileNameWithoutExtension(databasePath);
            var extension = Path.GetExtension(databasePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var backupPath = Path.Combine(directory, $"{fileName}.backup-{timestamp}{extension}");

            SqliteConnection.ClearAllPools();
            File.Move(databasePath, backupPath);
            return backupPath;
        }

        private static SqliteException? GetSqliteException(Exception exception)
        {
            var current = exception;

            while (current is not null)
            {
                if (current is SqliteException sqliteException)
                    return sqliteException;

                current = current.InnerException;
            }

            return null;
        }
    }
}
