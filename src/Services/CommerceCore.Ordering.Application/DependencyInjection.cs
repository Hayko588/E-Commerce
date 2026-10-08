using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using CommerceCore.Ordering.Application.Behaviours;

namespace CommerceCore.Ordering.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
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