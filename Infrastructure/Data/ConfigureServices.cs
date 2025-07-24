using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OddScout.Application.Common.Interfaces;
using OddScout.Infrastructure.Data;

namespace OddScout.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var databaseProvider = configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";

        // Configuração condicional do banco de dados
        if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString, b =>
                {
                    b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                }));
        }
        else
        {
            // SQL Server (desenvolvimento local)
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
        }

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        return services;
    }
}