using Microsoft.EntityFrameworkCore;

namespace CommerceCore.TestSupport;

public static class DatabaseMigrator
{
    // Applies a service's EF migrations the way a deployment does, before the application starts.
    public static async Task MigrateAsync<TContext>(string connectionString) where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(TContext).Assembly.FullName))
            .Options;

        await using var context = (TContext)Activator.CreateInstance(typeof(TContext), options)!;
        await context.Database.MigrateAsync();
    }
}
