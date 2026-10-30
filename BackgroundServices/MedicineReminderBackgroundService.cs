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
            .Where(m => !m.IsDeleted)
            .Include(m => m.MedicineSchedules)
            .Include(m => m.Patient)
                .ThenInclude(p => p.User)
                    .ThenInclude(u => u.NotificationPreference)
            .ToListAsync(stoppingToken);

        foreach (var medicine in medicines)
        {
            // Check Low Stock & Refill Alerts for each medicine
            await CheckLowStockAndRefillAlertsAsync(context, medicine, stoppingToken);

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
                        x => (x.ScheduleId.HasValue && x.ScheduleId.Value == schedule.Id && (x.ScheduledTime.Date == today || (x.OriginalScheduledTime.HasValue && x.OriginalScheduledTime.Value.Date == today))) ||
                             (x.MedicineId == medicine.Id &&
                              (x.ScheduledTime == scheduledTime || (x.OriginalScheduledTime.HasValue && x.OriginalScheduledTime.Value == scheduledTime))),
                        stoppingToken);

                // Check Reminder (2 minutes before scheduled time up to 15 minutes after, only while pending)
                if (now >= scheduledTime.AddMinutes(-2) && now < scheduledTime.AddMinutes(15) && (log == null || log.Status == "Pending"))
                {
                    if (log == null)
                    {
                        log = new MedicineLog
                        {
                            MedicineId = medicine.Id,
                            PatientId = medicine.PatientId,
                            ScheduleId = schedule.Id,
                            ScheduledTime = scheduledTime,
                            OriginalScheduledTime = scheduledTime,
                            Status = "Pending",
                            MedicineName = medicine.Name,
                            Dosage = medicine.Dosage,
                            MealTiming = medicine.MealTimeing
                        };

                        context.MedicineLogs.Add(log);
                        await context.SaveChangesAsync(stoppingToken);
                    }

                    await TrySendReminderEmailAsync(context, emailService, schedule, medicine, scheduledTime, stoppingToken);
                }

                // Check Missed (15 minutes grace period from scheduled or snoozed time)
                var effectiveDoseTime = log != null && log.Status == "Snoozed" ? log.ScheduledTime : scheduledTime;
                if (log != null && log.Status != "Taken" && now >= effectiveDoseTime.AddMinutes(15))
                {
                    if (log.Status != "Missed")
                    {
                        log.Status = "Missed";
                        await context.SaveChangesAsync(stoppingToken);
                    }

                    await TrySendMissedEmailAsync(context, emailService, schedule, medicine, scheduledTime, stoppingToken);
                }

                // Check Caregiver Alert (30 minutes grace period from scheduled time)
                if (log != null && log.Status == "Missed" && now >= scheduledTime.AddMinutes(30))
                {
                    await TrySendCaregiverAlertAsync(context, emailService, schedule, medicine, scheduledTime, stoppingToken);
                }
            }
        }
    }

    private async Task TrySendReminderEmailAsync(AppDbContext context, IEmailService emailService, MedicineSchedule schedule, Medicine medicine, DateTime scheduledTime, CancellationToken stoppingToken)
    {
        if (medicine.Patient?.User == null) return;
        var recipientEmail = medicine.Patient.User.Email;
        var patientUserId = medicine.Patient.UserId;
        var patientName = medicine.Patient.User.Name;

        var userPref = await context.UserNotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == patientUserId, stoppingToken);

        // 1. In-App Notification for Patient Reminder
        bool reminderAlreadyNotified = await context.Notifications.AnyAsync(n =>
            n.UserId == patientUserId &&
            n.Type == "MedicineReminder" &&
            n.Message.Contains(medicine.Name) &&
            n.CreatedAt >= scheduledTime.AddMinutes(-5) &&
            n.CreatedAt <= scheduledTime.AddMinutes(60), stoppingToken);

        bool inAppEnabled = userPref == null || (userPref.DoseReminders && userPref.InAppNotifications);
        if (!reminderAlreadyNotified && inAppEnabled)
        {
            var patientNotification = new Notification
            {
                UserId = patientUserId,
                Title = "Medicine Reminder",
                Message = $"Time to take {medicine.Name}.",
                Type = "MedicineReminder",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            context.Notifications.Add(patientNotification);
            await context.SaveChangesAsync(stoppingToken);
        }

        // 2. Email Notification for Patient Reminder
        if (string.IsNullOrWhiteSpace(recipientEmail)) return;

        var deliveryLog = await context.EmailDeliveryLogs.FirstOrDefaultAsync(
            l => l.MedicineScheduleId == schedule.Id &&
                 l.ScheduledOccurrence == scheduledTime &&
                 l.NotificationType == "Reminder" &&
                 l.RecipientEmail == recipientEmail, stoppingToken);

        if (deliveryLog != null && (deliveryLog.Status == "Sent" || deliveryLog.Status == "Suppressed" || deliveryLog.RetryCount >= 3))
            return;

        bool emailEnabled = userPref == null || (userPref.DoseReminders && userPref.EmailNotifications);
        if (!emailEnabled)
        {
            if (deliveryLog == null)
            {
                deliveryLog = new EmailDeliveryLog
                {
                    MedicineScheduleId = schedule.Id,
                    ScheduledOccurrence = scheduledTime,
                    NotificationType = "Reminder",
                    RecipientEmail = recipientEmail,
                    Status = "Suppressed"
                };
                context.EmailDeliveryLogs.Add(deliveryLog);
            }
            else
            {
                deliveryLog.Status = "Suppressed";
            }
            await context.SaveChangesAsync(stoppingToken);
            return;
        }

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
            await context.SaveChangesAsync(stoppingToken);
        }

        try
        {
            await emailService.SendEmailAsync(
                recipientEmail,
                "MediCare - Upcoming Medicine Reminder",
                $"Hello {patientName},\n\n" +
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
            deliveryLog.ErrorMessage = null;
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
        if (medicine.Patient?.User == null) return;
        var recipientEmail = medicine.Patient.User.Email;
        var patientUserId = medicine.Patient.UserId;
        var patientName = medicine.Patient.User.Name;

        var userPref = await context.UserNotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == patientUserId, stoppingToken);

        // 1. In-App Notification for Patient Missed Dose
        bool missedAlreadyNotified = await context.Notifications.AnyAsync(n =>
            n.UserId == patientUserId &&
            n.Type == "MedicineMissed" &&
            n.Message.Contains(medicine.Name) &&
            n.CreatedAt >= scheduledTime.AddMinutes(-5) &&
            n.CreatedAt <= scheduledTime.AddMinutes(360), stoppingToken);

        bool inAppEnabled = userPref == null || (userPref.MissedDoseAlerts && userPref.InAppNotifications);
        if (!missedAlreadyNotified && inAppEnabled)
        {
            var patientNotification = new Notification
            {
                UserId = patientUserId,
                Title = "Medicine Missed",
                Message = $"You missed your {medicine.Name} dose.",
                Type = "MedicineMissed",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            context.Notifications.Add(patientNotification);
            await context.SaveChangesAsync(stoppingToken);
        }

        // 2. Email Notification for Patient Missed Dose
        if (string.IsNullOrWhiteSpace(recipientEmail)) return;

        var deliveryLog = await context.EmailDeliveryLogs.FirstOrDefaultAsync(
            l => l.MedicineScheduleId == schedule.Id &&
                 l.ScheduledOccurrence == scheduledTime &&
                 l.NotificationType == "Missed" &&
                 l.RecipientEmail == recipientEmail, stoppingToken);

        if (deliveryLog != null && (deliveryLog.Status == "Sent" || deliveryLog.Status == "Suppressed" || deliveryLog.RetryCount >= 3))
            return;

        bool emailEnabled = userPref == null || (userPref.MissedDoseAlerts && userPref.EmailNotifications);
        if (!emailEnabled)
        {
            if (deliveryLog == null)
            {
                deliveryLog = new EmailDeliveryLog
                {
                    MedicineScheduleId = schedule.Id,
                    ScheduledOccurrence = scheduledTime,
                    NotificationType = "Missed",
                    RecipientEmail = recipientEmail,
                    Status = "Suppressed"
                };
                context.EmailDeliveryLogs.Add(deliveryLog);
            }
            else
            {
                deliveryLog.Status = "Suppressed";
            }
            await context.SaveChangesAsync(stoppingToken);
            return;
        }

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
            await context.SaveChangesAsync(stoppingToken);
        }

        try
        {
            await emailService.SendEmailAsync(
                recipientEmail,
                "MediCare - Missed Medicine Alert",
                $"Hello {patientName},\n\n" +
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
            deliveryLog.ErrorMessage = null;
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
        if (medicine.Patient?.User == null) return;
        var patientUserId = medicine.Patient.UserId;
        var patientName = medicine.Patient.User.Name;

        var caregivers = await context.CaregiverPatients
            .Include(cp => cp.Caregiver)
                .ThenInclude(c => c.User)
                    .ThenInclude(u => u.NotificationPreference)
            .Where(cp => cp.PatientId == medicine.PatientId)
            .ToListAsync(stoppingToken);

        var patientPref = await context.UserNotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == patientUserId, stoppingToken);

        bool patientAllowedCg = patientPref == null || patientPref.CaregiverAlerts;

        foreach (var caregiverPatient in caregivers)
        {
            if (caregiverPatient.Caregiver?.User == null) continue;

            var caregiverEmail = caregiverPatient.Caregiver.User.Email;
            var caregiverUserId = caregiverPatient.Caregiver.UserId;
            var caregiverName = caregiverPatient.Caregiver.User.Name;

            var cgPref = await context.UserNotificationPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == caregiverUserId, stoppingToken);

            // 1. In-App Notification for Caregiver
            bool cgInAppEnabled = cgPref == null || (cgPref.CaregiverAlerts && cgPref.InAppNotifications);
            if (patientAllowedCg && cgInAppEnabled)
            {
                bool cgAlreadyNotified = await context.Notifications.AnyAsync(n =>
                    n.UserId == caregiverUserId &&
                    n.Type == "CaregiverMedicineAlert30Min" &&
                    n.Message.Contains(medicine.Name) &&
                    n.CreatedAt >= scheduledTime.AddMinutes(-5) &&
                    n.CreatedAt <= scheduledTime.AddMinutes(720), stoppingToken);

                if (!cgAlreadyNotified)
                {
                    var caregiverNotification = new Notification
                    {
                        UserId = caregiverUserId,
                        Title = "Patient Medicine Alert",
                        Message = $"The patient {patientName} missed {medicine.Name} for 30 minutes.",
                        Type = "CaregiverMedicineAlert30Min",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };
                    context.Notifications.Add(caregiverNotification);
                    await context.SaveChangesAsync(stoppingToken);
                }
            }

            // 2. Email Notification for Caregiver
            if (string.IsNullOrWhiteSpace(caregiverEmail)) continue;

            var cgDeliveryLog = await context.EmailDeliveryLogs.FirstOrDefaultAsync(
                l => l.MedicineScheduleId == schedule.Id &&
                     l.ScheduledOccurrence == scheduledTime &&
                     l.NotificationType == "CaregiverAlert30Min" &&
                     l.RecipientEmail == caregiverEmail, stoppingToken);

            if (cgDeliveryLog != null && (cgDeliveryLog.Status == "Sent" || cgDeliveryLog.Status == "Suppressed" || cgDeliveryLog.RetryCount >= 3))
                continue;

            bool cgEmailEnabled = cgPref == null || (cgPref.CaregiverAlerts && cgPref.EmailNotifications);
            if (!patientAllowedCg || !cgEmailEnabled)
            {
                if (cgDeliveryLog == null)
                {
                    cgDeliveryLog = new EmailDeliveryLog
                    {
                        MedicineScheduleId = schedule.Id,
                        ScheduledOccurrence = scheduledTime,
                        NotificationType = "CaregiverAlert30Min",
                        RecipientEmail = caregiverEmail,
                        Status = "Suppressed"
                    };
                    context.EmailDeliveryLogs.Add(cgDeliveryLog);
                }
                else
                {
                    cgDeliveryLog.Status = "Suppressed";
                }
                await context.SaveChangesAsync(stoppingToken);
                continue;
            }

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
                await context.SaveChangesAsync(stoppingToken);
            }

            try
            {
                await emailService.SendEmailAsync(
                    caregiverEmail,
                    "MediCare - Patient Medicine Alert",
                    $"Hello {caregiverName},\n\n" +
                    $"This is an important medicine alert from MediCare.\n\n" +
                    $"The following patient has not recorded taking their scheduled medicine within 30 minutes of the scheduled time.\n\n" +
                    $"Patient: {patientName}\n" +
                    $"Medicine: {medicine.Name}\n" +
                    $"Scheduled Time: {scheduledTime:g}\n" +
                    $"Status: Missed\n\n" +
                    "Please contact the patient to check whether they have taken the medicine.\n\n" +
                    "If the patient has already taken it, please ensure their medicine intake record is updated in MediCare.\n\n" +
                    "Regards,\n" +
                    "MediCare Team");

                cgDeliveryLog.Status = "Sent";
                cgDeliveryLog.SentAt = DateTime.Now;
                cgDeliveryLog.ErrorMessage = null;
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

    // Feature 3: Low-Stock Alerts & Feature 4: Refill Alerts
    public static async Task CheckLowStockAndRefillAlertsAsync(
        AppDbContext context,
        Medicine medicine,
        CancellationToken stoppingToken = default)
    {
        // Enforce existing threshold of 5
        if (medicine == null || medicine.IsDeleted || medicine.Stock > 5)
            return;

        var today = DateTime.Today;

        if (medicine.Patient == null)
        {
            await context.Entry(medicine).Reference(m => m.Patient).LoadAsync(stoppingToken);
        }
        if (medicine.Patient != null && medicine.Patient.User == null)
        {
            await context.Entry(medicine.Patient).Reference(p => p.User).LoadAsync(stoppingToken);
        }

        if (medicine.Patient?.User == null)
            return;

        var patientUserId = medicine.Patient.UserId;
        var patientName = medicine.Patient.User.Name;

        var patientPref = await context.UserNotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == patientUserId, stoppingToken);

        // Feature 3: Respect saved LowStockAlerts preference for patient
        bool patientLowStockEnabled = patientPref == null || patientPref.LowStockAlerts;
        if (patientLowStockEnabled)
        {
            // Prevent duplicate alert for this medicine today
            bool patientAlreadyNotified = await context.Notifications.AnyAsync(n =>
                n.UserId == patientUserId &&
                n.Type == "Low Stock" &&
                n.Message.Contains(medicine.Name) &&
                n.CreatedAt >= today, stoppingToken);

            if (!patientAlreadyNotified)
            {
                var patientNotification = new Notification
                {
                    UserId = patientUserId,
                    Title = "Low Medicine Stock",
                    Message = $"{medicine.Name} stock is low ({medicine.Stock} left). Please refill.",
                    Type = "Low Stock",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                context.Notifications.Add(patientNotification);
            }
        }

        // Feature 3: Notify authorized, linked caregivers
        var caregivers = await context.CaregiverPatients
            .Include(cp => cp.Caregiver)
                .ThenInclude(c => c.User)
            .Where(cp => cp.PatientId == medicine.PatientId)
            .ToListAsync(stoppingToken);

        foreach (var cp in caregivers)
        {
            if (cp.Caregiver?.User == null) continue;

            var cgUserId = cp.Caregiver.UserId;
            var cgPref = await context.UserNotificationPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == cgUserId, stoppingToken);

            bool cgLowStockEnabled = cgPref == null || cgPref.LowStockAlerts;
            if (cgLowStockEnabled)
            {
                // Prevent duplicate low stock alert to caregiver today
                bool cgAlreadyNotified = await context.Notifications.AnyAsync(n =>
                    n.UserId == cgUserId &&
                    n.Type == "Low Stock" &&
                    n.Message.Contains(medicine.Name) &&
                    n.CreatedAt >= today, stoppingToken);

                if (!cgAlreadyNotified)
                {
                    var cgNotification = new Notification
                    {
                        UserId = cgUserId,
                        Title = "Patient Medicine Low Stock Alert",
                        Message = $"Medicine '{medicine.Name}' for patient {patientName} is running low ({medicine.Stock} doses remaining).",
                        Type = "Low Stock",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };
                    context.Notifications.Add(cgNotification);
                }
            }
        }

        // Feature 4: Respect saved RefillAlerts preference for patient
        bool patientRefillEnabled = patientPref == null || patientPref.RefillAlerts;
        if (patientRefillEnabled)
        {
            // Prevent duplicate refill alert today
            bool refillAlreadyNotified = await context.Notifications.AnyAsync(n =>
                n.UserId == patientUserId &&
                n.Type == "Refill" &&
                n.Message.Contains(medicine.Name) &&
                n.CreatedAt >= today, stoppingToken);

            if (!refillAlreadyNotified)
            {
                var refillNotification = new Notification
                {
                    UserId = patientUserId,
                    Title = $"Critical Refill Alert: {medicine.Name}",
                    Message = $"Inventory check: Only {medicine.Stock} units remaining for {medicine.Name}. Please order a refill soon.",
                    Type = "Refill",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                context.Notifications.Add(refillNotification);
            }
        }

        // Feature 4: Refill alerts for linked caregivers if preference enabled
        foreach (var cp in caregivers)
        {
            if (cp.Caregiver?.User == null) continue;

            var cgUserId = cp.Caregiver.UserId;
            var cgPref = await context.UserNotificationPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == cgUserId, stoppingToken);

            bool cgRefillEnabled = cgPref == null || cgPref.RefillAlerts;
            if (cgRefillEnabled)
            {
                bool cgRefillNotified = await context.Notifications.AnyAsync(n =>
                    n.UserId == cgUserId &&
                    n.Type == "Refill" &&
                    n.Message.Contains(medicine.Name) &&
                    n.CreatedAt >= today, stoppingToken);

                if (!cgRefillNotified)
                {
                    var cgRefillNotification = new Notification
                    {
                        UserId = cgUserId,
                        Title = $"Patient Refill Alert: {medicine.Name}",
                        Message = $"Refill needed: Only {medicine.Stock} units remaining for {medicine.Name} (Patient: {patientName}).",
                        Type = "Refill",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };
                    context.Notifications.Add(cgRefillNotification);
                }
            }
        }

        await context.SaveChangesAsync(stoppingToken);
    }
}