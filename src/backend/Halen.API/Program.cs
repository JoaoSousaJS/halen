using System.Text;
using Confluent.Kafka;
using FluentValidation;
using Halen.Application.Auth.Commands;
using Halen.Application.Interfaces;
using Halen.Application.Pipeline;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Messaging;
using Halen.Infrastructure.Persistence;
using Halen.Infrastructure.Services;
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
            // Match the short "role" claim name written by JwtService.
            RoleClaimType = "role",
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("PatientOnly", p => p.RequireRole("Patient"))
    .AddPolicy("DoctorOnly", p => p.RequireRole("Doctor"))
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"));

// ── MediatR ───────────────────────────────────────────────────────────────────
// Scans the Application assembly and registers all IRequestHandler<> implementations.
// ValidationBehavior runs before every handler — returns 400 if a validator rejects the input.
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(RegisterCommand).Assembly);

// ── Infrastructure services ───────────────────────────────────────────────────
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, MockEmailService>();
builder.Services.AddScoped<IPaymentService, MockPaymentService>();

// Kafka producer registered as singleton — one connection, reused across requests
var kafkaConfig = new ProducerConfig
{
    BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092"
};
builder.Services.AddSingleton<IProducer<string, string>>(_ =>
    new ProducerBuilder<string, string>(kafkaConfig).Build());
builder.Services.AddScoped<IEventBus, KafkaEventBus>();

// ── API ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var frontendOrigin = builder.Configuration["Frontend:Origin"] ?? "http://localhost:5173";
builder.Services.AddCors(opt =>
    opt.AddPolicy("Frontend", policy =>
        policy.WithOrigins(frontendOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()));

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
app.UseAuthentication();  // order matters — must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

// Auto-apply migrations and seed roles on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
    await db.Database.MigrateAsync();

    // Roles must exist before any user can be assigned one.
    // RoleManager.CreateAsync is idempotent — safe to call on every startup.
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var role in new[] { "Patient", "Doctor", "Admin" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }

    // Seed a default admin user when credentials are present in configuration.
    // Skipped if the email already exists — safe to run on every startup.
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
                Role     = UserRole.Admin,
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}

app.Run();

public partial class Program { }
