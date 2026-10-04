using Checkbus.ApiService.Application.Auth.Commands;
using Checkbus.ApiService.Application.Common.Behaviors;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Maps;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.ExceptionHandling;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.ApiService.Infrastructure.Implementations.Maps;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Implementations.Storage;
using Checkbus.ApiService.Infrastructure.Persistence;
using Checkbus.ApiService.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() 
    ?? throw new InvalidOperationException("Missing Jwt configuration section.");

builder.Services.AddSingleton(jwtOptions);

var fileStorageOptions = builder.Configuration.GetSection("FileStorage").Get<FileStorageOptions>()
    ?? throw new InvalidOperationException("Missing FileStorage configuration section.");
fileStorageOptions.LocalRootPath =
    Path.GetFullPath(fileStorageOptions.LocalRootPath, builder.Environment.ContentRootPath);

builder.Services.AddSingleton(fileStorageOptions);

// Unlike Jwt/FileStorage, GoogleMaps binds permissively: no API key has been provisioned yet
// (added later via dotnet user-secrets), so a missing section/key must not fail startup.
var googleMapsOptions = builder.Configuration.GetSection("GoogleMaps").Get<GoogleMapsOptions>()
    ?? new GoogleMapsOptions();

builder.Services.AddSingleton(googleMapsOptions);
builder.Services.AddHttpClient<IDirectionsService, GoogleDirectionsService>(client =>
{
    client.BaseAddress = new Uri("https://maps.googleapis.com/");
});

builder.Services.AddScoped<IPasswordHasher, IdentityPasswordHasher>();
builder.Services.AddScoped<IJwtGenerator, JwtGenerator>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IDriverRequirementRepository, DriverRequirementRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IVehicleDocumentRepository, VehicleDocumentRepository>();
builder.Services.AddScoped<IMaintenanceRecordRepository, MaintenanceRecordRepository>();
builder.Services.AddScoped<IVehicleDiagnosticRepository, VehicleDiagnosticRepository>();
builder.Services.AddScoped<IUbicacionRepository, UbicacionRepository>();
builder.Services.AddScoped<IEventoRepository, EventoRepository>();
builder.Services.AddScoped<IViajeRepository, ViajeRepository>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<LoginCommand>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssemblyContaining<LoginCommand>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddControllers(options => options.Filters.Add(new AuthorizeFilter()));

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AuthExceptionHandler>();
builder.Services.AddExceptionHandler<UserConflictExceptionHandler>();
builder.Services.AddExceptionHandler<DriverRequirementExceptionHandler>();
builder.Services.AddExceptionHandler<VehicleExceptionHandler>();
builder.Services.AddExceptionHandler<VehicleDocumentExceptionHandler>();
builder.Services.AddExceptionHandler<MaintenanceRecordExceptionHandler>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddNpgsqlDbContext<CheckbusDbContext>("checkbusdb", configureDbContextOptions: options =>
{
    options.UseSeeding(CheckbusDbSeeder.Seed);
    options.UseAsyncSeeding(CheckbusDbSeeder.SeedAsync);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CheckbusDbContext>();
    var strategy = dbContext.Database.CreateExecutionStrategy();
    await strategy.ExecuteAsync(() => dbContext.Database.EnsureCreatedAsync());
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "API service is running.");

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();