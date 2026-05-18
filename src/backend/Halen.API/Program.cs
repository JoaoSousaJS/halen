using System.Text;
using System.Threading.RateLimiting;
using Confluent.Kafka;
using FluentValidation;
using Halen.Application.Auth.Commands;
using Halen.Application.Interfaces;
using Halen.Application.MedicalRecords;
using Halen.Application.Pipeline;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.API.Hubs;
using Halen.API.Middleware;
using Halen.Infrastructure.Messaging;
using Halen.Infrastructure.Persistence;
using Halen.Infrastructure.Services;
using Halen.Infrastructure.Storage;
using Microsoft.AspNetCore.SignalR;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Fail fast: crash at startup rather than at first request if the JWT secret is absent.
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured. Set it via environment variable or user-secrets.");

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<HalenDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ── Identity ──────────────────────────────────────────────────────────────────
// Adds UserManager, SignInManager, role management, password hashing, etc.
builder.Services.AddIdentity<User, IdentityRole<Guid>>(opt =>
    {
        opt.Password.RequireDigit = true;
        opt.Password.RequiredLength = 8;
        opt.Lockout.MaxFailedAccessAttempts = 5;
        opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<HalenDbContext>()
    .AddDefaultTokenProviders();

// ── JWT Auth ──────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(opt =>
    {
        opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opt =>
    {
        opt.MapInboundClaims = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)),
            RoleClaimType = "role",
        };
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("PatientOnly", p => p.RequireRole("Patient"))
    .AddPolicy("DoctorOnly", p => p.RequireRole("Doctor"))
    .AddPolicy("PlatformAdmin", p => p.RequireRole("PlatformAdmin"))
    .AddPolicy("ClinicAdmin", p => p.RequireRole("ClinicAdmin", "PlatformAdmin"))
    .AddPolicy("AdminOnly", p => p.RequireRole("PlatformAdmin", "ClinicAdmin"));

// ── MediatR ───────────────────────────────────────────────────────────────────
// Scans the Application assembly and registers all IRequestHandler<> implementations.
// ValidationBehavior runs before every handler — returns 400 if a validator rejects the input.
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(RegisterCommand).Assembly);

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<HalenDbContext>());

// ── Infrastructure services ───────────────────────────────────────────────────
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, MockEmailService>();
builder.Services.AddScoped<IPaymentService, MockPaymentService>();
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<IRecordAccessChecker, RecordAccessChecker>();

// Kafka producer registered as singleton — one connection, reused across requests
var kafkaConfig = new ProducerConfig
{
    BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092"
};
builder.Services.AddSingleton<IProducer<string, string>>(_ =>
    new ProducerBuilder<string, string>(kafkaConfig).Build());
builder.Services.AddScoped<IEventBus, KafkaEventBus>();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, SubClaimUserIdProvider>();
builder.Services.AddSingleton<INotificationSender, SignalRNotificationSender>();
builder.Services.AddSingleton<NotificationMessageHandler>();
builder.Services.AddHostedService<NotificationConsumerService>();

// ── Rate limiting ────────────────────────────────────────────────────────────
var authRateLimit = int.TryParse(builder.Configuration["RateLimit:Auth"], out var parsed) ? parsed : 10;
builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opt.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

// ── API ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var frontendOrigin = builder.Configuration["Frontend:Origin"] ?? "http://localhost:5173";
builder.Services.AddCors(opt =>
    opt.AddPolicy("Frontend", policy =>
        policy.WithOrigins(frontendOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

var app = builder.Build();

// Global exception handler — prevents stack traces from leaking to clients.
// ValidationException → 400 with the validation errors.
// Everything else → logged + generic 500.
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        var feature = ctx.Features.Get<IExceptionHandlerFeature>();
        var error = feature?.Error;

        if (error is UnauthorizedAccessException)
        {
            ctx.Response.StatusCode = 401;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }

        if (error is ValidationException ve)
        {
            ctx.Response.StatusCode = 400;
            ctx.Response.ContentType = "application/json";
            var errors = ve.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            await ctx.Response.WriteAsJsonAsync(new { errors });
            return;
        }

        if (error is not null)
        {
            var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(error, "Unhandled exception");
        }

        ctx.Response.StatusCode = 500;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new { error = "An internal error occurred" });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<FeatureFlagMiddleware>();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<ConsultationHub>("/hubs/consultation");
app.MapHub<ChatHub>("/hubs/chat");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var role in new[] { "Patient", "Doctor", "ClinicAdmin", "PlatformAdmin" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }

    // Seed default clinic — all open registrations go here.
    var defaultSlug = app.Configuration["Seed:DefaultClinicSlug"] ?? "default";
    var defaultClinicName = app.Configuration["Seed:DefaultClinicName"] ?? "Default Clinic";
    var defaultClinic = await db.Clinics.FirstOrDefaultAsync(c => c.Slug == defaultSlug);
    if (defaultClinic is null)
    {
        defaultClinic = new Clinic { Name = defaultClinicName, Slug = defaultSlug };
        db.Clinics.Add(defaultClinic);
        await db.SaveChangesAsync();
    }

    // Ensure all feature flags exist for the default clinic (migration may have
    // created the clinic without flags).
    var existingKeys = await db.ClinicFeatureFlags
        .Where(f => f.ClinicId == defaultClinic.Id)
        .Select(f => f.FeatureKey)
        .ToListAsync();
    var missingKeys = Halen.Domain.Constants.FeatureKeys.All.Except(existingKeys);
    foreach (var key in missingKeys)
        db.ClinicFeatureFlags.Add(new ClinicFeatureFlag { ClinicId = defaultClinic.Id, FeatureKey = key, IsEnabled = true });
    if (missingKeys.Any())
        await db.SaveChangesAsync();

    // Seed platform admin user.
    var adminEmail = app.Configuration["Seed:AdminEmail"];
    var adminPassword = app.Configuration["Seed:AdminPassword"];
    if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new User
            {
                FirstName = app.Configuration["Seed:AdminFirstName"] ?? "Halen",
                LastName  = app.Configuration["Seed:AdminLastName"]  ?? "Admin",
                Email    = adminEmail,
                UserName = adminEmail,
                Role     = UserRole.PlatformAdmin,
                ClinicId = defaultClinic.Id,
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "PlatformAdmin");
        }
    }
}

app.Run();

public partial class Program { }
