using Application.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Infrastructure.Persistence
{
    internal static class DataAccessGuard
    {
        private const int SqliteError = 1;
        private const int SqliteBusy = 5;
        private const int SqliteLocked = 6;
        private const int SqliteReadonly = 8;
        private const int SqliteIoErr = 10;
        private const int SqliteCorrupt = 11;
        private const int SqliteCantOpen = 14;
        private const int SqliteConstraint = 19;
        private const int SqliteNotADatabase = 26;

        public static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (ShouldWrap(ex))
            {
                throw CreateException(operationName, ex);
            }
        }

        public static async Task ExecuteAsync(Func<Task> operation, string operationName)
        {
            try
            {
                await operation();
            }
            catch (Exception ex) when (ShouldWrap(ex))
            {
                throw CreateException(operationName, ex);
            }
        }

        public static void Execute(Action operation, string operationName)
        {
            try
            {
                operation();
            }
            catch (Exception ex) when (ShouldWrap(ex))
            {
                throw CreateException(operationName, ex);
            }
        }

        public static T Execute<T>(Func<T> operation, string operationName)
        {
            try
            {
                return operation();
            }
            catch (Exception ex) when (ShouldWrap(ex))
            {
                throw CreateException(operationName, ex);
            }
        }

        private static bool ShouldWrap(Exception ex)
        {
            if (ex is OperationCanceledException or DataAccessException)
                return false;

            return ex is DbUpdateException
                or SqliteException
                or InvalidOperationException
                or IOException
                or UnauthorizedAccessException;
        }

        private static DataAccessException CreateException(string operationName, Exception ex)
        {
            var message = ex switch
            {
                DbUpdateConcurrencyException => "Данные были изменены или удалены другим действием. Обновите экран и повторите операцию.",
                DbUpdateException dbUpdateException => GetSqliteException(dbUpdateException) is { } sqliteException
                    ? GetSqliteMessage(sqliteException)
                    : "Не удалось сохранить данные. Проверьте введенные значения и повторите операцию.",
                SqliteException sqliteException => GetSqliteMessage(sqliteException),
                IOException => "Не удалось получить доступ к файлу базы данных. Проверьте, что диск доступен и не переполнен.",
                UnauthorizedAccessException => "Нет прав доступа к файлу базы данных. Запустите приложение с доступом к папке пользователя.",
                InvalidOperationException => "Не удалось выполнить операцию с базой данных. Обновите экран и повторите действие.",
                _ => "Произошла ошибка при работе с базой данных."
            };

            return new DataAccessException($"{message} Операция: {operationName}.", ex);
        }

        private static SqliteException? GetSqliteException(Exception exception)
        {
            var current = exception;

            while (current.InnerException is not null)
            {
                if (current.InnerException is SqliteException sqliteException)
                    return sqliteException;

                current = current.InnerException;
            }

            return null;
        }

        private static string GetSqliteMessage(SqliteException exception)
        {
            return exception.SqliteErrorCode switch
            {
                SqliteConstraint => "Нарушено ограничение данных: возможно, такая запись уже существует или связанная запись не найдена.",
                SqliteBusy or SqliteLocked => "База данных временно занята или заблокирована. Закройте другие окна/инструменты с этой БД и повторите операцию.",
                SqliteReadonly => "База данных открыта только для чтения. Проверьте права на файл БД.",
                SqliteIoErr or SqliteCantOpen => "Не удалось открыть или записать файл базы данных. Проверьте путь, диск и права доступа.",
                SqliteCorrupt or SqliteNotADatabase => "Файл базы данных поврежден или имеет неверный формат.",
                SqliteError => "SQLite не смог выполнить запрос к базе данных.",
                _ => "Произошла ошибка SQLite при работе с базой данных."
            };
        }
    }
}
