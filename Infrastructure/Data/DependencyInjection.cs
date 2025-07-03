using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.Common.Interfaces.IScraping;
using OddScout.Infrastructure.Data;
using OddScout.Infrastructure.Services;
using OddScout.Infrastructure.Services.Scraping;
using System.IO.Compression;
using System.Net;

namespace OddScout.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Services
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IGotoConversionService, GotoConversionService>();

        // Scraping Services
        services.AddHttpClient<IPinnacleScrapingService, PinnacleScrapingService>(client =>
        {
            // Configurar timeout e outras opções
            client.Timeout = TimeSpan.FromMinutes(5);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
        });

        services.AddScoped<IBetbyScrapingService, BetbyScrapingService>();
        services.AddScoped<IValueBetCalculationService, ValueBetCalculationService>();

        return services;
    }
}