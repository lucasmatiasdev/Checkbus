using Checkbus.Application.Abstractions;
using Checkbus.Application.UseCases.Authentication.Login;
using Checkbus.Application.UseCases.Authentication.Register;
using Checkbus.Infrastructure.Context;
using Checkbus.Infrastructure.Repositories;
using Checkbus.Infrastructure.Security;
using Checkbus.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("CheckbusDb")
                ?? throw new InvalidOperationException(
                    "Connection string 'CheckbusDb' not configured. Set it via " +
                    "'dotnet user-secrets set ConnectionStrings:CheckbusDb ...' (Development) " +
                    "or the ConnectionStrings__CheckbusDb environment variable.");

            services.AddDbContextFactory<CheckbusDbContext>(options =>
                options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

            // D4: bind the tenant after construction (never via the constructor) so
            // AddDbContextFactory's options-only activation short-circuit stays intact.
            services.AddScoped(provider =>
            {
                var context = provider.GetRequiredService<IDbContextFactory<CheckbusDbContext>>().CreateDbContext();
                context.BindTenant(provider.GetRequiredService<ICurrentTenant>());
                return context;
            });

            services.AddSingleton(TimeProvider.System);
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IOrganizationRepository, OrganizationRepository>();
            services.AddScoped<IProfileRepository, ProfileRepository>();
            services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
            services.AddSingleton<IPasswordGenerator, CryptoPasswordGenerator>();

            // D8: scoped to match their scoped repository dependencies.
            services.AddScoped<LoginUseCase>();
            services.AddScoped<RegisterUseCase>();

            // Registered unconditionally; the only Development gate is the Program.cs call site
            // (design "single Development gate") — a registered-but-never-invoked service is not
            // a reachable path.
            services.AddScoped<ISeedDataStore, DbContextSeedDataStore>();
            services.AddScoped<DevelopmentDataSeeder>();

            return services;
        }
    }
}
