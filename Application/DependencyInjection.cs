using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OddScout.Application.Common.Behaviors;
using OddScout.Application.Users.Commands.SignUp;
using System.Reflection;

namespace OddScout.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // Add FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}