using MediCare.Data;
using MediCare.Repositories;
using MediCare.Repositories.Interfaces;
using MediCare.Service;
using MediCare.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using MediCare.BackgroundServices;
using System.Text.Json.Serialization;
using MediCare.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
    )
);

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"]!
                )
            ),

            ClockSkew = TimeSpan.Zero
        };
    });

// Repository
builder.Services.AddScoped<IMedicineRepository, MedicineRepository>();
builder.Services.AddScoped<IMedicineScheduleRepository, MedicineScheduleRepository>();
builder.Services.AddScoped<IMedicineLogRepository, MedicineLogRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ICaregiverPatientRepository, CaregiverPatientRepository>();

// Service
builder.Services.AddScoped<IMedicineService, MedicineService>();
builder.Services.AddScoped<IMedicineScheduleService, MedicineScheduleService>();
builder.Services.AddScoped<IMedicineLogService, MedicineLogService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICaregiverPatientService, CaregiverPatientService>();
builder.Services.AddScoped<IAdherenceService, AdherenceService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Background Service
builder.Services.AddHostedService<MedicineReminderBackgroundService>();


var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();