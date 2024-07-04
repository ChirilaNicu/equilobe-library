using Equilobe.Core.Shared;
using Equilobe.Core.Features.Loans;
using Equilobe.Infrastructure.Data;
using Equilobe.Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Equilobe.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LibraryDatabase")
            ?? throw new ArgumentException("LibraryDatabase Connection String");

        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        services.AddDbContext<LibraryDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

            options.UseSqlite(connectionString);
        });

        services.AddScoped<ILibraryDbContext>(provider => provider.GetRequiredService<LibraryDbContext>());
        services.AddScoped<LibraryDbContextInitializer>();
        services.AddScoped<IPenaltyCalculator, DefaultPenaltyCalculator>();

    }
}
