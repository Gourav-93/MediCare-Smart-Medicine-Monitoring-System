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
                    TimeSpan.FromSeconds(15),
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

                var log = await context.MedicineLogs
                    .FirstOrDefaultAsync(
                        x => x.MedicineId == medicine.Id &&
                             x.ScheduledTime == scheduledTime,
                        stoppingToken);

                // Check Reminder (2 minutes before)
                if (now >= scheduledTime.AddMinutes(-2))
                {
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
                        await context.SaveChangesAsync(stoppingToken);
                    }

                    await TrySendReminderEmailAsync(context, emailService, schedule, medicine, scheduledTime, stoppingToken);
                }

                // Check Missed (5 minutes grace period)
                if (log != null && log.Status == "Pending" && now >= scheduledTime.AddMinutes(5))
                {
                    log.Status = "Missed";
                    await context.SaveChangesAsync(stoppingToken);

                    await TrySendMissedEmailAsync(context, emailService, schedule, medicine, scheduledTime, stoppingToken);
                }

                // Check Caregiver Alert (30 minutes grace period)
                if (log != null && log.Status == "Missed" && now >= scheduledTime.AddMinutes(30))
                {
                    await TrySendCaregiverAlertAsync(context, emailService, schedule, medicine, scheduledTime, stoppingToken);
                }
            }
        }
    }

    private async Task TrySendReminderEmailAsync(AppDbContext context, IEmailService emailService, MedicineSchedule schedule, Medicine medicine, DateTime scheduledTime, CancellationToken stoppingToken)
    {
        var recipientEmail = medicine.Patient.User.Email;
        if (string.IsNullOrWhiteSpace(recipientEmail)) return;

        var deliveryLog = await context.EmailDeliveryLogs.FirstOrDefaultAsync(
            l => l.MedicineScheduleId == schedule.Id && l.ScheduledOccurrence == scheduledTime && l.NotificationType == "Reminder" && l.RecipientEmail == recipientEmail, stoppingToken);
        
        if (deliveryLog != null && (deliveryLog.Status == "Sent" || deliveryLog.RetryCount >= 3))
            return;

        if (deliveryLog == null)
        {
            deliveryLog = new EmailDeliveryLog
            {
                MedicineScheduleId = schedule.Id,
                ScheduledOccurrence = scheduledTime,
                NotificationType = "Reminder",
                RecipientEmail = recipientEmail,
                Status = "Pending"
            };
            context.EmailDeliveryLogs.Add(deliveryLog);
            
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
            
            await context.SaveChangesAsync(stoppingToken);
        }

        try
        {
            await emailService.SendEmailAsync(
                recipientEmail,
                "MediCare - Upcoming Medicine Reminder",
                $"Hello {medicine.Patient.User.Name},\n\n" +
                $"This is a reminder from MediCare that it is almost time to take your medicine.\n" +
                $"Medicine: {medicine.Name}\n" +
                $"Scheduled Time: {scheduledTime:t}\n" +
                $"Dosage: {(string.IsNullOrEmpty(medicine.Dosage) ? "N/A" : medicine.Dosage)}\n" +
                "Please take your medicine at the scheduled time and record your medicine intake in MediCare.\n" +
                "Stay healthy!\n\n" +
                "Regards,\n" +
                "MediCare Team");

            deliveryLog.Status = "Sent";
            deliveryLog.SentAt = DateTime.Now;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reminder email to patient {Email}.", recipientEmail);
            deliveryLog.Status = "Failed";
            deliveryLog.ErrorMessage = ex.Message;
            deliveryLog.RetryCount++;
        }
        finally
        {
            await context.SaveChangesAsync(stoppingToken);
        }
    }

    private async Task TrySendMissedEmailAsync(AppDbContext context, IEmailService emailService, MedicineSchedule schedule, Medicine medicine, DateTime scheduledTime, CancellationToken stoppingToken)
    {
        var recipientEmail = medicine.Patient.User.Email;
        if (string.IsNullOrWhiteSpace(recipientEmail)) return;

        var deliveryLog = await context.EmailDeliveryLogs.FirstOrDefaultAsync(
            l => l.MedicineScheduleId == schedule.Id && l.ScheduledOccurrence == scheduledTime && l.NotificationType == "Missed" && l.RecipientEmail == recipientEmail, stoppingToken);
        
        if (deliveryLog != null && (deliveryLog.Status == "Sent" || deliveryLog.RetryCount >= 3))
            return;

        if (deliveryLog == null)
        {
            deliveryLog = new EmailDeliveryLog
            {
                MedicineScheduleId = schedule.Id,
                ScheduledOccurrence = scheduledTime,
                NotificationType = "Missed",
                RecipientEmail = recipientEmail,
                Status = "Pending"
            };
            context.EmailDeliveryLogs.Add(deliveryLog);
            
            var patientNotification = new Notification
            {
                UserId = medicine.Patient.UserId,
                Title = "Medicine Missed",
                Message = $"You missed your {medicine.Name} dose.",
                Type = "MedicineMissed",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            context.Notifications.Add(patientNotification);

            await context.SaveChangesAsync(stoppingToken);
        }

        try
        {
            await emailService.SendEmailAsync(
                recipientEmail,
                "MediCare - Missed Medicine Alert",
                $"Hello {medicine.Patient.User.Name},\n\n" +
                $"Our MediCare system noticed that you have not recorded taking the following scheduled medicine.\n" +
                $"Medicine: {medicine.Name}\n" +
                $"Scheduled Time: {scheduledTime:t}\n" +
                $"Status: Missed\n" +
                "If you have already taken this medicine, please update your medicine record in MediCare.\n" +
                "Please follow your healthcare professional's instructions.\n\n" +
                "Regards,\n" +
                "MediCare Team");

            deliveryLog.Status = "Sent";
            deliveryLog.SentAt = DateTime.Now;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send missed medicine email to patient {Email}.", recipientEmail);
            deliveryLog.Status = "Failed";
            deliveryLog.ErrorMessage = ex.Message;
            deliveryLog.RetryCount++;
        }
        finally
        {
            await context.SaveChangesAsync(stoppingToken);
        }
    }

    private async Task TrySendCaregiverAlertAsync(AppDbContext context, IEmailService emailService, MedicineSchedule schedule, Medicine medicine, DateTime scheduledTime, CancellationToken stoppingToken)
    {
        var caregivers = await context.CaregiverPatients
            .Include(cp => cp.Caregiver)
                .ThenInclude(c => c.User)
            .Where(cp => cp.PatientId == medicine.PatientId)
            .ToListAsync(stoppingToken);

        foreach (var caregiverPatient in caregivers)
        {
            var caregiverEmail = caregiverPatient.Caregiver.User.Email;
            if (string.IsNullOrWhiteSpace(caregiverEmail)) continue;

            var cgDeliveryLog = await context.EmailDeliveryLogs.FirstOrDefaultAsync(
                l => l.MedicineScheduleId == schedule.Id && l.ScheduledOccurrence == scheduledTime && l.NotificationType == "CaregiverAlert30Min" && l.RecipientEmail == caregiverEmail, stoppingToken);

            if (cgDeliveryLog != null && (cgDeliveryLog.Status == "Sent" || cgDeliveryLog.RetryCount >= 3))
                continue;

            if (cgDeliveryLog == null)
            {
                cgDeliveryLog = new EmailDeliveryLog
                {
                    MedicineScheduleId = schedule.Id,
                    ScheduledOccurrence = scheduledTime,
                    NotificationType = "CaregiverAlert30Min",
                    RecipientEmail = caregiverEmail,
                    Status = "Pending"
                };
                context.EmailDeliveryLogs.Add(cgDeliveryLog);
                
                var caregiverNotification = new Notification
                {
                    UserId = caregiverPatient.Caregiver.UserId,
                    Title = "Patient Medicine Alert",
                    Message = $"The patient {medicine.Patient.User.Name} missed {medicine.Name} for 30 minutes.",
                    Type = "CaregiverMedicineAlert30Min",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                context.Notifications.Add(caregiverNotification);

                await context.SaveChangesAsync(stoppingToken);
            }

            try
            {
                await emailService.SendEmailAsync(
                    caregiverEmail,
                    "MediCare - Patient Medicine Alert",
                    $"Hello {caregiverPatient.Caregiver.User.Name},\n\n" +
                    $"This is an important medicine alert from MediCare.\n\n" +
                    $"The following patient has not recorded taking their scheduled medicine within 30 minutes of the scheduled time.\n\n" +
                    $"Patient: {medicine.Patient.User.Name}\n" +
                    $"Medicine: {medicine.Name}\n" +
                    $"Scheduled Time: {scheduledTime:g}\n" +
                    $"Status: Missed\n\n" +
                    "Please contact the patient to check whether they have taken the medicine.\n\n" +
                    "If the patient has already taken it, please ensure their medicine intake record is updated in MediCare.\n\n" +
                    "Regards,\n" +
                    "MediCare Team");

                cgDeliveryLog.Status = "Sent";
                cgDeliveryLog.SentAt = DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send 30-min caregiver alert to {Email}.", caregiverEmail);
                cgDeliveryLog.Status = "Failed";
                cgDeliveryLog.ErrorMessage = ex.Message;
                cgDeliveryLog.RetryCount++;
            }
            finally
            {
                await context.SaveChangesAsync(stoppingToken);
            }
        }
    }
}