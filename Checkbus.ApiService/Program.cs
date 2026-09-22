using Checkbus.ApiService.Application.Auth.Commands;
using Checkbus.ApiService.Application.Common;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() 
    ?? throw new InvalidOperationException("Missing Jwt configuration section.");

builder.Services.AddSingleton(jwtOptions);

builder.Services.AddScoped<IPasswordHasher, IdentityPasswordHasher>();
builder.Services.AddScoped<IJwtGenerator, JwtGenerator>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICommandHandler<LoginCommand, LoginCommandResult>, LoginCommandHandler>();

builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddProblemDetails();

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

app.MapControllers();

app.Run();