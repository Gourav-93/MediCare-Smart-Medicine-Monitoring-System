using MediCare.Data;
using MediCare.Models;
using MediCare.Services.Interfaces;
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Medicine Reminder Service started.");

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

        var context =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var emailService =
            scope.ServiceProvider.GetRequiredService<IEmailService>();

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

                var scheduledTime =
                    today.Add(schedule.Time);

                if (scheduledTime > now)
                    continue;

                var log = await context.MedicineLogs
                    .FirstOrDefaultAsync(
                        x => x.MedicineId == medicine.Id &&
                             x.ScheduledTime == scheduledTime,
                        stoppingToken);

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

                    // Database notification for patient
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

                    // Email notification for patient
                    if (!string.IsNullOrWhiteSpace(
                            medicine.Patient.User.Email))
                    {
                        try
                        {
                            await emailService.SendEmailAsync(
                                medicine.Patient.User.Email,
                                "Medicine Reminder - MediCare",
                                $"Hello {medicine.Patient.User.Name},\n\n" +
                                $"It is time to take your medicine: {medicine.Name}.\n\n" +
                                "Please take your medicine on time.\n\n" +
                                "Regards,\n" +
                                "MediCare");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Failed to send reminder email to patient {Email}.",
                                medicine.Patient.User.Email);
                        }
                    }

                    _logger.LogInformation(
                        "Reminder created for {MedicineName}.",
                        medicine.Name);
                }

                // Mark medicine as missed after 30 minutes
                if (log.Status == "Pending" &&
                    now >= scheduledTime.AddMinutes(30))
                {
                    log.Status = "Missed";

                    // Patient database notification
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

                    // Patient email
                    if (!string.IsNullOrWhiteSpace(
                            medicine.Patient.User.Email))
                    {
                        try
                        {
                            await emailService.SendEmailAsync(
                                medicine.Patient.User.Email,
                                "Medicine Missed - MediCare",
                                $"Hello {medicine.Patient.User.Name},\n\n" +
                                $"You missed your {medicine.Name} dose.\n\n" +
                                "Please make sure to follow your medicine schedule.\n\n" +
                                "Regards,\n" +
                                "MediCare");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Failed to send missed medicine email to patient {Email}.",
                                medicine.Patient.User.Email);
                        }
                    }

                    // Get linked caregivers
                    var caregivers =
                        await context.CaregiverPatients
                            .Include(cp => cp.Caregiver)
                                .ThenInclude(c => c.User)
                            .Where(cp =>
                                cp.PatientId == medicine.PatientId)
                            .ToListAsync(stoppingToken);

                    foreach (var caregiverPatient in caregivers)
                    {
                        var caregiver =
                            caregiverPatient.Caregiver;

                        // Caregiver database notification
                        var caregiverNotification =
                            new Notification
                            {
                                UserId = caregiver.UserId,
                                Title = "Patient Missed Medicine",
                                Message =
                                    $"The patient missed {medicine.Name}.",
                                Type = "CaregiverMedicineMissed",
                                IsRead = false,
                                CreatedAt = DateTime.Now
                            };

                        context.Notifications.Add(
                            caregiverNotification);

                        // Caregiver email
                        if (!string.IsNullOrWhiteSpace(
                                caregiver.User.Email))
                        {
                            try
                            {
                                await emailService.SendEmailAsync(
                                    caregiver.User.Email,
                                    "Patient Missed Medicine - MediCare",
                                    $"Hello {caregiver.User.Name},\n\n" +
                                    $"The patient has missed the scheduled " +
                                    $"dose of {medicine.Name}.\n\n" +
                                    "Please check on the patient.\n\n" +
                                    "Regards,\n" +
                                    "MediCare");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(
                                    ex,
                                    "Failed to send missed medicine email to caregiver {Email}.",
                                    caregiver.User.Email);
                            }
                        }
                    }

                    _logger.LogInformation(
                        "Medicine {MedicineName} marked as missed. " +
                        "Patient and caregivers notified.",
                        medicine.Name);
                }
            }
        }

        await context.SaveChangesAsync(stoppingToken);
    }
}