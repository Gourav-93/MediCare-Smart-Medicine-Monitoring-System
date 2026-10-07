using MediCare.Data;
using MediCare.Models;
using Microsoft.EntityFrameworkCore;

namespace MediCare.BackgroundServices;

public class MedicineReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MedicineReminderBackgroundService> _logger;

    public MedicineReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<MedicineReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Medicine Reminder Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckMedicineSchedulesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while checking medicine schedules.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task CheckMedicineSchedulesAsync(
        CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var now = DateTime.Now;
        var today = now.Date;

        var medicines = await context.Medicines
            .Include(m => m.MedicineSchedules)
            .Include(m => m.Patient)
            .ThenInclude(p => p.User)
            .ToListAsync(stoppingToken);

        foreach (var medicine in medicines)
        {
            if (medicine.StartDate.Date > today ||
                medicine.EndDate.Date < today)
            {
                continue;
            }

            foreach (var schedule in medicine.MedicineSchedules)
            {
                if (!string.Equals(
                    schedule.Frequency,
                    "Daily",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var scheduledTime = today.Add(schedule.Time);

                if (scheduledTime > now)
                {
                    continue;
                }

                var log = await context.MedicineLogs
                    .FirstOrDefaultAsync(
                        x => x.MedicineId == medicine.Id &&
                             x.ScheduledTime == scheduledTime,
                        stoppingToken);

                // Create pending log and patient notification.
                if (log == null)
                {
                    log = new MedicineLog
                    {
                        MedicineId = medicine.Id,
                        PatientId = medicine.PatientId,
                        ScheduledTime = scheduledTime,
                        Status = "Pending"
                    };

                    context.MedicineLogs.Add(log);

                    var patientNotification = new Notification
                    {
                        UserId = medicine.Patient.UserId,
                        Title = "Medicine Reminder",
                        Message = $"Time to take {medicine.Name}.",
                        Type = "MedicineReminder",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };

                    context.Notifications.Add(patientNotification);

                    _logger.LogInformation(
                        "Reminder created for {MedicineName}.",
                        medicine.Name);
                }

                // Mark medicine as missed after 30 minutes.
                if (log.Status == "Pending" &&
                    now >= scheduledTime.AddMinutes(30))
                {
                    log.Status = "Missed";

                    // Notify patient.
                    var patientNotification = new Notification
                    {
                        UserId = medicine.Patient.UserId,
                        Title = "Medicine Missed",
                        Message =
                            $"You missed your {medicine.Name} dose.",
                        Type = "MedicineMissed",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };

                    context.Notifications.Add(patientNotification);

                    // Find linked caregivers.
                    var caregivers = await context.CaregiverPatients
                        .Include(cp => cp.Caregiver)
                        .Where(cp => cp.PatientId == medicine.PatientId)
                        .ToListAsync(stoppingToken);

                    foreach (var caregiverPatient in caregivers)
                    {
                        var caregiverNotification = new Notification
                        {
                            UserId = caregiverPatient.Caregiver.UserId,
                            Title = "Patient Missed Medicine",
                            Message =
                                $"The patient missed {medicine.Name}.",
                            Type = "CaregiverMedicineMissed",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        };

                        context.Notifications.Add(
                            caregiverNotification);
                    }

                    _logger.LogInformation(
                        "Medicine {MedicineName} marked as missed. " +
                        "Caregivers notified.",
                        medicine.Name);
                }
            }
        }

        await context.SaveChangesAsync(stoppingToken);
    }
}