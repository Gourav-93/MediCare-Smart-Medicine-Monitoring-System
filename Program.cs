using MediCare.Data;
using MediCare.Repositories;
using MediCare.Repositories.Interfaces;
using MediCare.Service;
using MediCare.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
    )
);
// Repository
builder.Services.AddScoped<IMedicineRepository, MedicineRepository>();
builder.Services.AddScoped<IMedicineScheduleRepository, MedicineScheduleRepository>();
builder.Services.AddScoped<IMedicineLogRepository, MedicineLogRepository>();

// Service
builder.Services.AddScoped<IMedicineService, MedicineService>();
builder.Services.AddScoped<IMedicineScheduleService, MedicineScheduleService>();
builder.Services.AddScoped<IMedicineLogService, MedicineLogService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();