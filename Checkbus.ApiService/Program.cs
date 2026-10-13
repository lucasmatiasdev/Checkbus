using Checkbus.ApiService.Application.Auth.Commands;
using Checkbus.ApiService.Application.Common.Behaviors;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Maps;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Payments;
using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.ExceptionHandling;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.ApiService.Infrastructure.Implementations.Email;
using Checkbus.ApiService.Infrastructure.Implementations.Maps;
using Checkbus.ApiService.Infrastructure.Implementations.Payments;
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
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

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

// Email binds permissively too: the real Brevo SMTP credentials have not been provisioned yet
// (added later via dotnet user-secrets), so a missing section/values must not fail startup.
var emailOptions = builder.Configuration.GetSection("Email").Get<EmailOptions>()
    ?? new EmailOptions();

builder.Services.AddSingleton(emailOptions);
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Mercado Pago binds permissively too, same reason as Email above: real sandbox credentials
// have not been provisioned yet (G1, odd/tasks/pagos-mercadopago.md) so a missing section/
// values must not fail startup.
var mercadoPagoOptions = builder.Configuration.GetSection("MercadoPago").Get<MercadoPagoOptions>()
    ?? new MercadoPagoOptions();

builder.Services.AddSingleton(mercadoPagoOptions);
builder.Services.AddScoped<IPaymentService, MercadoPagoPaymentService>();

// Fase I (odd/tasks/pagos-mercadopago.md, Marketplace/Split Payments): encrypts/decrypts
// MercadoPagoConnection.AccessToken/RefreshToken at rest (OrganizationMercadoPagoConnectionRepository).
// AddDataProtection() registers IDataProtectionProvider explicitly rather than relying on it
// being pulled in incidentally by some other ASP.NET Core feature (Antiforgery/cookie auth)
// that might not always be present.
builder.Services.AddDataProtection();
builder.Services.AddScoped<IMercadoPagoOAuthService, MercadoPagoOAuthService>();
builder.Services.AddScoped<IOrganizationMercadoPagoConnectionRepository, OrganizationMercadoPagoConnectionRepository>();
builder.Services.AddScoped<IMercadoPagoSellerTokenResolver, MercadoPagoSellerTokenResolver>();

// The Web app's externally-reachable base URL, used to build links embedded in outgoing
// emails (odd/tasks/reservas-publicas.md Fase 2) — the API can't infer the frontend's address
// on its own. Same permissive/blank-until-provisioned shape as GoogleMaps.
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
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IRefundRepository, RefundRepository>();
builder.Services.AddScoped<IProcessedWebhookEventRepository, ProcessedWebhookEventRepository>();
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

// Rate limiting (odd/tasks/reservas-publicas.md R6). Scoped tightly to the project's public,
// unauthenticated surface only (PublicTripsController/PublicReservationsController, via
// [EnableRateLimiting] on those controllers) — deliberately NOT a global DefaultPolicy, the
// same "not a blanket opt-out" principle R3-R5 applied to [AllowAnonymous]. Every authenticated
// staff endpoint is unaffected: no policy is attached to it, so no limiter ever runs for it.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Partitioned per caller IP, sliding window. 20 requests/minute/IP: generous enough for a
    // genuine family-of-3 visitor (a few seat-map loads/refreshes while picking seats, plus one
    // batch submit, well under 20 in a minute) while still shutting down a seat-scraping or
    // reservation-spam script long before it can meaningfully exhaust seats or data. Not a hard
    // number from measured traffic (none exists yet for a brand-new public surface) — a
    // deliberately conservative starting point, easy to retune later from real usage.
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

// Add services to the container.
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
    // Any future entity/schema change needs its own `dotnet ef migrations add <Name>
    // --project Checkbus.ApiService.Infrastructure --startup-project Checkbus.ApiService` —
    // MigrateAsync() only applies migrations that already exist as files, it does not infer them.
    await strategy.ExecuteAsync(() => dbContext.Database.MigrateAsync());
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
app.UseRateLimiter();

app.MapControllers();

app.Run();