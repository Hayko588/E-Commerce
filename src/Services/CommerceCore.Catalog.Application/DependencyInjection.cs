using CommerceCore.Catalog.Application.Behaviors;
using CommerceCore.Catalog.Application.Stock;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceCore.Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<StockReservationService>();

        return services;
    }
}
