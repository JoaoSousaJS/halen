using System.Text;
using Confluent.Kafka;
using Halen.Application.Auth.Commands;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Infrastructure.Messaging;
using Halen.Infrastructure.Persistence;
using Halen.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("PatientOnly", p => p.RequireRole("Patient"))
    .AddPolicy("DoctorOnly", p => p.RequireRole("Doctor"))
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"));

// ── MediatR ───────────────────────────────────────────────────────────────────
// Scans the Application assembly and registers all IRequestHandler<> implementations
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly));

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

builder.Services.AddCors(opt =>
    opt.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();  // order matters — must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

// Auto-apply migrations on startup in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
