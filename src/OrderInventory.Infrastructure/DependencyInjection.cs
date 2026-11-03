using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Identity;
using OrderInventory.Application.Seeding;
using OrderInventory.Infrastructure.Configuration;
using OrderInventory.Infrastructure.Identity;
using OrderInventory.Infrastructure.Persistence;

namespace OrderInventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("ConnectionStrings:Database is required.");
        services.AddDbContext<OrderInventoryDbContext>(options => options.UseSqlServer(connection));
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(JwtOptions.IsValid, "JWT configuration is invalid.").ValidateOnStart();
        services.AddOptions<BootstrapOptions>().Bind(configuration.GetSection(BootstrapOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IUserStore, EfUserStore>();
        services.AddScoped<ISeedHistoryStore, EfSeedHistoryStore>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<AuthenticationService>();
        services.AddScoped<UserAdministrationService>();
        services.AddScoped<BootstrapSeeder>();
        services.AddScoped<BootstrapSeedService>();
        return services;
    }
}
