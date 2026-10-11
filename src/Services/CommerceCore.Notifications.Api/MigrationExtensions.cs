using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CommerceCore.Notifications.Api;

public static class MigrationExtensions
{
    // Development helper. If SQL Server is still recovering its databases when the API starts,
    // EF thinks the database is missing, runs CREATE DATABASE and gets error 1801 (already exists).
    // Retrying lets EF see the database on the next attempt.
    public static async Task MigrateWithRetryAsync(this DbContext db, ILogger logger, int maxAttempts = 8)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsStartupRace(ex))
            {
                logger.LogWarning("Migration attempt {Attempt}/{Max} failed ({Message}); retrying in 5s",
                    attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }

    // 1801 = database already exists, 4060 = cannot open database (still recovering)
    private static bool IsStartupRace(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
            if (e is SqlException { Number: 1801 or 4060 }) return true;
        return false;
    }
}
