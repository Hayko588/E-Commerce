using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Behaviors;
using System.Reflection;

namespace OrderService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 1. Register MediatR handlers, pipeline behaviors, and ISender
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // 2. Register FluentValidation validators from the application assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}