using CommerceCore.Notifications.Application;
using CommerceCore.Notifications.Infrastructure.Persistence;
using CommerceCore.Notifications.Infrastructure.Sending;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceCore.Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(NotificationsDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
                sql.CommandTimeout(120);
            }));

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationReadService, NotificationReadService>();
        services.AddSingleton<INotificationSender, LoggingNotificationSender>();

        return services;
    }
}
