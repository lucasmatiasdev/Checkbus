using Checkbus.Application.Abstractions;
using Checkbus.Infrastructure.Context;
using Checkbus.Infrastructure.Repositories;
using Checkbus.Infrastructure.Security;
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

            services.AddScoped(provider =>
                provider.GetRequiredService<IDbContextFactory<CheckbusDbContext>>().CreateDbContext());

            services.AddSingleton(TimeProvider.System);
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IOrganizationRepository, OrganizationRepository>();
            services.AddScoped<IProfileRepository, ProfileRepository>();
            services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
            services.AddSingleton<IPasswordGenerator, CryptoPasswordGenerator>();

            return services;
        }
    }
}
