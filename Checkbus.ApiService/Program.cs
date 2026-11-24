using Checkbus.ApiService.Application.Auth.Commands;
using Checkbus.ApiService.Application.Common.Behaviors;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Maps;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Reports;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Payments;
using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.ExceptionHandling;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.ApiService.Infrastructure.Implementations.Email;
using Checkbus.ApiService.Infrastructure.Implementations.Maps;
using Checkbus.ApiService.Infrastructure.Implementations.Payments;
using Checkbus.ApiService.Infrastructure.Implementations.Reports;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Implementations.Storage;
using Checkbus.ApiService.Infrastructure.Persistence;
using Checkbus.ApiService.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var argentineCulture = new CultureInfo("es-AR");
CultureInfo.DefaultThreadCurrentCulture = argentineCulture;
CultureInfo.DefaultThreadCurrentUICulture = argentineCulture;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing Jwt configuration section.");

builder.Services.AddSingleton(jwtOptions);

var fileStorageOptions = builder.Configuration.GetSection("FileStorage").Get<FileStorageOptions>()
    ?? throw new InvalidOperationException("Missing FileStorage configuration section.");
fileStorageOptions.LocalRootPath =
    Path.GetFullPath(fileStorageOptions.LocalRootPath, builder.Environment.ContentRootPath);

builder.Services.AddSingleton(fileStorageOptions);

var googleMapsOptions = builder.Configuration.GetSection("GoogleMaps").Get<GoogleMapsOptions>()
    ?? new GoogleMapsOptions();

builder.Services.AddSingleton(googleMapsOptions);
builder.Services.AddHttpClient<IDirectionsService, GoogleDirectionsService>(client =>
{
    client.BaseAddress = new Uri("https://maps.googleapis.com/");
});

var emailOptions = builder.Configuration.GetSection("Email").Get<EmailOptions>()
    ?? new EmailOptions();

builder.Services.AddSingleton(emailOptions);
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

var supportOptions = builder.Configuration.GetSection("Support").Get<SupportOptions>()
    ?? new SupportOptions();

builder.Services.AddSingleton(supportOptions);

var mercadoPagoOptions = builder.Configuration.GetSection("MercadoPago").Get<MercadoPagoOptions>()
    ?? new MercadoPagoOptions();

builder.Services.AddSingleton(mercadoPagoOptions);

var simulatedPaymentOptions = builder.Configuration.GetSection("Simulation").Get<SimulatedPaymentOptions>()
    ?? new SimulatedPaymentOptions();

builder.Services.AddSingleton(simulatedPaymentOptions);

builder.Services.AddHttpClient("SimulatedWebhook");

if (mercadoPagoOptions.Simulated)
{

    builder.Services.AddSingleton<SimulatedPaymentService>();
    builder.Services.AddSingleton<IPaymentService>(sp => sp.GetRequiredService<SimulatedPaymentService>());
    builder.Services.AddSingleton<ISimulatedOrderStore>(sp => sp.GetRequiredService<SimulatedPaymentService>());
}
else
{
    builder.Services.AddScoped<IPaymentService, MercadoPagoPaymentService>();
}

builder.Services.AddDataProtection();
builder.Services.AddScoped<IMercadoPagoOAuthService, MercadoPagoOAuthService>();
builder.Services.AddScoped<IOrganizationMercadoPagoConnectionRepository, OrganizationMercadoPagoConnectionRepository>();

if (mercadoPagoOptions.Simulated)
{
    builder.Services.AddSingleton<IMercadoPagoSellerTokenResolver, SimulatedMercadoPagoSellerTokenResolver>();
}
else
{
    builder.Services.AddScoped<IMercadoPagoSellerTokenResolver, MercadoPagoSellerTokenResolver>();
}

builder.Services.AddScoped<IMercadoPagoConnectionStateService, MercadoPagoConnectionStateService>();

builder.Services.AddSingleton(new PublicWebOptions
{
    BaseUrl = builder.Configuration["PublicWebBaseUrl"] ?? string.Empty
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
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IPassengerRepository, PassengerRepository>();
builder.Services.AddScoped<IBoardingRecordRepository, BoardingRecordRepository>();
builder.Services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IRefundRepository, RefundRepository>();
builder.Services.AddScoped<IProcessedWebhookEventRepository, ProcessedWebhookEventRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
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

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("public-reservations", httpContext =>
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetSlidingWindowLimiter(remoteIp, _ => new SlidingWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4,
            PermitLimit = 20,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });
});

builder.Services.AddControllers(options => options.Filters.Add(new AuthorizeFilter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AuthExceptionHandler>();
builder.Services.AddExceptionHandler<UserConflictExceptionHandler>();
builder.Services.AddExceptionHandler<DriverRequirementExceptionHandler>();
builder.Services.AddExceptionHandler<VehicleExceptionHandler>();
builder.Services.AddExceptionHandler<VehicleDocumentExceptionHandler>();
builder.Services.AddExceptionHandler<MaintenanceRecordExceptionHandler>();
builder.Services.AddExceptionHandler<TripExceptionHandler>();
builder.Services.AddExceptionHandler<ReservationExceptionHandler>();
builder.Services.AddExceptionHandler<BoardingExceptionHandler>();
builder.Services.AddExceptionHandler<SupportTicketExceptionHandler>();
builder.Services.AddExceptionHandler<OrganizationExceptionHandler>();

builder.Services.AddOpenApi();

builder.AddNpgsqlDbContext<CheckbusDbContext>("checkbusdb", configureDbContextOptions: options =>
{
    options.UseSeeding(CheckbusDbSeeder.Seed);
    options.UseAsyncSeeding(CheckbusDbSeeder.SeedAsync);
});

var app = builder.Build();

{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CheckbusDbContext>();
    var strategy = dbContext.Database.CreateExecutionStrategy();

    await strategy.ExecuteAsync(() => dbContext.Database.MigrateAsync());
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "API service is running.");

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();