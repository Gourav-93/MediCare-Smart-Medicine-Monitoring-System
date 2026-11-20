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

    // Bounded recovery: today plus the previous seven calendar days.
    private const int RecoveryDays = 7;

    private async Task CheckMedicineSchedulesAsync(
        CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // Preserve the existing local-time convention until B17 is addressed.
        var now = DateTime.Now;
        var today = now.Date;
        var recoveryStart = today.AddDays(-RecoveryDays);

        // Do not globally mark overdue rows: a cancelled or unknown legacy
        // occurrence must not be converted into a missed dose.

        var medicines = await context.Medicines.AsNoTracking()
            .Where(m => !m.IsDeleted)
            .Include(m => m.MedicineSchedules)
            .Include(m => m.Patient)
                .ThenInclude(p => p.User)
                    .ThenInclude(u => u.NotificationPreference)
            .ToListAsync(stoppingToken);

        foreach (var medicine in medicines)
        {
            stoppingToken.ThrowIfCancellationRequested();
            await CheckLowStockAndRefillAlertsAsync(context, medicine, stoppingToken);

            var firstDay = medicine.StartDate.Date > recoveryStart
                ? medicine.StartDate.Date : recoveryStart;
            var lastDay = medicine.EndDate.Date < today.AddDays(1)
                ? medicine.EndDate.Date : today.AddDays(1);

            foreach (var schedule in medicine.MedicineSchedules)
            {
                if (!string.Equals(schedule.Frequency, "Daily", StringComparison.OrdinalIgnoreCase) ||
                    schedule.Time < TimeSpan.Zero || schedule.Time >= TimeSpan.FromDays(1))
                    continue;

                for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    var occurrence = day.Add(schedule.Time);
                    if (occurrence > now.AddMinutes(2) ||
                         !IsEffectiveOccurrence(schedule, medicine, occurrence))
                        continue;

                    var log = await GetOrCreateOccurrenceAsync(
                        context, medicine, schedule, occurrence, now, stoppingToken);
                    if (log == null)
                        continue; // Ambiguous legacy record; do not guess.

                    // The occurrence was validated and any overdue transition
                    // was performed under the medicine lock in the helper.
                    var effectiveTime = log.ScheduledTime;

                    // Do not replay old reminders or flood users after downtime.
                    // Keep existing email occurrence keys stable.
                    if (log.Status == "Pending" && now >= occurrence.AddMinutes(-2) &&
                        now < occurrence.AddMinutes(15))
                    {
                        await TrySendReminderEmailAsync(context, emailService,
                            schedule, medicine, occurrence, stoppingToken);
                    }

                    if (log.Status == "Missed" &&
                        now >= effectiveTime.AddMinutes(15) &&
                        now <= occurrence.AddHours(6))
                    {
                        await TrySendMissedEmailAsync(context, emailService,
                            schedule, medicine, occurrence, stoppingToken);
                    }

                    if (log.Status == "Missed" &&
                        now >= effectiveTime.AddMinutes(15) &&
                        now >= occurrence.AddMinutes(30) &&
                        now <= occurrence.AddHours(12))
                    {
                        await TrySendCaregiverAlertAsync(context, emailService,
                            schedule, medicine, occurrence, stoppingToken);
                    }
                }
            }
        }
    }

    // Lifecycle timestamps are UTC; occurrence keys retain the existing
    // server-local wall time convention until B17 is migrated consistently.
    private static bool IsEffectiveOccurrence(
        MedicineSchedule schedule, Medicine medicine, DateTime occurrence)
    {
        if (medicine.IsDeleted || !schedule.EffectiveFrom.HasValue ||
            (schedule.IsRetired && !schedule.EffectiveUntil.HasValue) ||
            !string.Equals(schedule.Frequency, "Daily", StringComparison.OrdinalIgnoreCase) ||
            schedule.Time < TimeSpan.Zero || schedule.Time >= TimeSpan.FromDays(1) ||
            occurrence.TimeOfDay != schedule.Time ||
            occurrence.Date < medicine.StartDate.Date ||
            occurrence.Date > medicine.EndDate.Date)
            return false;

        var wallTime = DateTime.SpecifyKind(occurrence, DateTimeKind.Unspecified);
        if (TimeZoneInfo.Local.IsInvalidTime(wallTime) ||
            TimeZoneInfo.Local.IsAmbiguousTime(wallTime))
            return false;

        var utc = TimeZoneInfo.ConvertTimeToUtc(wallTime, TimeZoneInfo.Local);
        return utc >= schedule.EffectiveFrom.Value &&
            (!schedule.EffectiveUntil.HasValue || utc < schedule.EffectiveUntil.Value);
    }

    private async Task<MedicineLog?> GetOrCreateOccurrenceAsync(
        AppDbContext context, Medicine medicine, MedicineSchedule schedule,
        DateTime occurrence, DateTime now, CancellationToken stoppingToken)
    {
        // Share the parent-row lock protocol with schedule mutations. Reload
        // lifecycle and prescription data after obtaining the lock.
        await using var transaction = await context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.ReadCommitted, stoppingToken);
        var ids = await context.Database.SqlQuery<int>(
            $"SELECT Id AS Value FROM Medicines WHERE Id = {medicine.Id} AND IsDeleted = 0 FOR UPDATE")
            .ToListAsync(stoppingToken);
        if (ids.Count != 1) return null;

        var current = await context.MedicineSchedules.AsNoTracking()
            .Include(s => s.Medicine)
            .FirstOrDefaultAsync(s => s.Id == schedule.Id &&
                s.MedicineId == medicine.Id, stoppingToken);
        if (current == null || current.Medicine.PatientId != medicine.PatientId ||
            !IsEffectiveOccurrence(current, current.Medicine, occurrence))
            return null;

        var exact = context.MedicineLogs.AsNoTracking().Where(x =>
            x.ScheduleId == current.Id && x.MedicineId == medicine.Id &&
            x.PatientId == medicine.PatientId && x.OriginalScheduledTime == occurrence);
        var matches = await exact.Take(2).ToListAsync(stoppingToken);
        if (matches.Count > 1)
        {
            _logger.LogWarning("Duplicate occurrence for schedule {ScheduleId} at {Occurrence}.", current.Id, occurrence);
            return null;
        }

        // Never generate or mutate an occurrence alongside an incomplete legacy
        // identity. Such records require explicit reconciliation.
        var hasLegacy = await context.MedicineLogs.AsNoTracking().AnyAsync(x =>
            x.MedicineId == medicine.Id && x.PatientId == medicine.PatientId &&
            (x.ScheduleId == current.Id || x.ScheduleId == null) &&
            (x.ScheduleId == null || x.OriginalScheduledTime == null) &&
            (x.OriginalScheduledTime ?? x.ScheduledTime) == occurrence, stoppingToken);
        if (hasLegacy)
        {
            _logger.LogWarning("Legacy occurrence requires reconciliation for schedule {ScheduleId} at {Occurrence}.", current.Id, occurrence);
            return null;
        }

        var log = matches.SingleOrDefault();
        if (log == null)
        {
            log = new MedicineLog
            {
                MedicineId = medicine.Id,
                PatientId = medicine.PatientId,
                ScheduleId = current.Id,
                ScheduledTime = occurrence,
                OriginalScheduledTime = occurrence,
                Status = now >= occurrence.AddMinutes(15) ? "Missed" : "Pending",
                MedicineName = current.Medicine.Name,
                Dosage = current.Medicine.Dosage,
                MealTiming = current.Medicine.MealTimeing
            };
            context.MedicineLogs.Add(log);
            try
            {
                await context.SaveChangesAsync(stoppingToken);
                context.Entry(log).State = EntityState.Detached;
            }
            catch (DbUpdateException ex) when (ex.InnerException is MySqlConnector.MySqlException { Number: 1062 })
            {
                context.Entry(log).State = EntityState.Detached;
                var concurrent = await exact.Take(2).ToListAsync(stoppingToken);
                if (concurrent.Count != 1) throw;
                log = concurrent[0];
            }
        }

        var cutoff = now.AddMinutes(-15);
        if ((log.Status == "Pending" || log.Status == "Snoozed") && log.ScheduledTime <= cutoff)
        {
            await context.MedicineLogs.Where(x => x.Id == log.Id &&
                (x.Status == "Pending" || x.Status == "Snoozed") && x.ScheduledTime <= cutoff)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, "Missed"), stoppingToken);
            log = await exact.FirstOrDefaultAsync(stoppingToken);
        }
        await transaction.CommitAsync(stoppingToken);
        return log;
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
            .Where(cp => cp.PatientId == medicine.PatientId &&
                         cp.Status == "APPROVED")
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
            .Where(cp => cp.PatientId == medicine.PatientId &&
                         cp.Status == "APPROVED")
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