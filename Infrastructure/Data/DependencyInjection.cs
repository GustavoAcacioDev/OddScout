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
        // Database - Handle Railway DATABASE_URL format
        var connectionString = GetConnectionString(configuration);
        var databaseProvider = configuration["DatabaseProvider"] ?? "PostgreSQL";

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (databaseProvider == "PostgreSQL")
            {
                options.UseNpgsql(connectionString,
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            }
            else
            {
                options.UseSqlServer(connectionString,
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            }
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Services
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IGotoConversionService, GotoConversionService>();
        services.AddScoped<IEventManagementService, EventManagementService>(); // ESSA LINHA É CRÍTICA

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

    private static string GetConnectionString(IConfiguration configuration)
    {
        // First try to get DATABASE_URL (Railway format)
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrEmpty(databaseUrl))
        {
            return ConvertDatabaseUrlToConnectionString(databaseUrl);
        }

        // Fallback to DefaultConnection
        return configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("No database connection string found");
    }

    private static string ConvertDatabaseUrlToConnectionString(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};Username={uri.UserInfo.Split(':')[0]};Password={uri.UserInfo.Split(':')[1]};SSL Mode=Require;Trust Server Certificate=true";
        return connectionString;
    }
}