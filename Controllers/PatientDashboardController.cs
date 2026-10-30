using System.Security.Claims;
using MediCare.Data;
using MediCare.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediCare.Models;

namespace MediCare.Controllers;

[ApiController]
[Route("api/dashboard/patient")]
[Authorize(Roles = "PATIENT")]
public class PatientDashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public PatientDashboardController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) && id > 0 ? id : 0;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
            return Unauthorized();

        // Find patient profile
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return NotFound("Patient profile not found.");

        // Get active medicines (exclude soft-deleted)
        var medicines = await _context.Medicines
            .Where(m => m.PatientId == patient.Id && !m.IsDeleted)
            .OrderBy(m => m.Name)
            .ToListAsync();

        // Total active medicines
        var totalMedicines = medicines.Count;

        // Today's date and time
        var now = DateTime.Now;
        var today = now.Date;

        // Auto-mark overdue pending or snoozed logs for this patient as Missed
        var overdueLogs = await _context.MedicineLogs
            .Where(l => l.PatientId == patient.Id &&
                        (l.Status == "Pending" || l.Status == "Snoozed") &&
                        l.ScheduledTime.AddMinutes(15) <= now)
            .ToListAsync();

        if (overdueLogs.Any())
        {
            foreach (var ol in overdueLogs)
            {
                ol.Status = "Missed";
            }
            await _context.SaveChangesAsync();
        }

        // Today's active schedules (exclude soft-deleted)
        var todayAllSchedules = await _context.MedicineSchedules
            .Include(s => s.Medicine)
            .Where(s =>
                !s.Medicine.IsDeleted &&
                s.Medicine.PatientId == patient.Id &&
                s.Medicine.StartDate.Date <= today &&
                s.Medicine.EndDate.Date >= today)
            .OrderBy(s => s.Time)
            .ToListAsync();

        // Today's medicine logs for this patient
        var todaysLogs = await _context.MedicineLogs
            .Include(log => log.Medicine)
            .Where(log =>
                log.PatientId == patient.Id &&
                (log.ScheduledTime.Date == today || (log.OriginalScheduledTime.HasValue && log.OriginalScheduledTime.Value.Date == today)))
            .ToListAsync();

        // Auto-create Missed logs for past schedules with no log
        bool hasNewMissed = false;
        foreach (var s in todayAllSchedules)
        {
            var scheduledTime = today.Add(s.Time);
            var log = todaysLogs.FirstOrDefault(l =>
                (l.ScheduleId.HasValue && l.ScheduleId.Value == s.Id) ||
                (l.MedicineId == s.MedicineId &&
                 ((l.OriginalScheduledTime.HasValue && l.OriginalScheduledTime.Value == scheduledTime) ||
                  l.ScheduledTime == scheduledTime ||
                  (l.Status == "Snoozed" && l.OriginalScheduledTime.HasValue && l.OriginalScheduledTime.Value == scheduledTime) ||
                  Math.Abs((l.ScheduledTime - scheduledTime).TotalMinutes) < 2)));

            if (log != null && !log.ScheduleId.HasValue)
            {
                log.ScheduleId = s.Id;
                hasNewMissed = true;
            }

            if (log == null && now > scheduledTime.AddMinutes(15))
            {
                log = new MedicineLog
                {
                    MedicineId = s.MedicineId,
                    Medicine = s.Medicine,
                    PatientId = patient.Id,
                    ScheduleId = s.Id,
                    ScheduledTime = scheduledTime,
                    OriginalScheduledTime = scheduledTime,
                    Status = "Missed",
                    MedicineName = s.Medicine.Name,
                    Dosage = s.Medicine.Dosage,
                    MealTiming = s.Medicine.MealTimeing
                };
                _context.MedicineLogs.Add(log);
                todaysLogs.Add(log);
                hasNewMissed = true;
            }
        }

        if (hasNewMissed)
        {
            await _context.SaveChangesAsync();
        }

        // Categorize doses for today: Only pending/upcoming doses go into the active dashboard list
        var pendingMedicines = new List<MedicineLog>();
        var pendingSchedules = new List<MedicineSchedule>();
        int todayTakenCount = 0;
        int todayMissedCount = 0;

        foreach (var schedule in todayAllSchedules)
        {
            var scheduledTime = today.Add(schedule.Time);
            var log = todaysLogs.FirstOrDefault(l =>
                (l.ScheduleId.HasValue && l.ScheduleId.Value == schedule.Id) ||
                (l.MedicineId == schedule.MedicineId &&
                 ((l.OriginalScheduledTime.HasValue && l.OriginalScheduledTime.Value == scheduledTime) ||
                  l.ScheduledTime == scheduledTime ||
                  (l.Status == "Snoozed" && l.OriginalScheduledTime.HasValue && l.OriginalScheduledTime.Value == scheduledTime) ||
                  Math.Abs((l.ScheduledTime - scheduledTime).TotalMinutes) < 2)));

            if (log != null && string.Equals(log.Status, "Taken", StringComparison.OrdinalIgnoreCase))
            {
                todayTakenCount++;
            }
            else if (log != null && string.Equals(log.Status, "Missed", StringComparison.OrdinalIgnoreCase))
            {
                todayMissedCount++;
            }
            else
            {
                // Pending, snoozed, or upcoming dose
                pendingSchedules.Add(schedule);

                if (log != null)
                {
                    if (!log.ScheduleId.HasValue)
                        log.ScheduleId = schedule.Id;
                    pendingMedicines.Add(log);
                }
                else
                {
                    pendingMedicines.Add(new MedicineLog
                    {
                        MedicineId = schedule.MedicineId,
                        Medicine = schedule.Medicine,
                        PatientId = patient.Id,
                        ScheduleId = schedule.Id,
                        ScheduledTime = scheduledTime,
                        OriginalScheduledTime = scheduledTime,
                        Status = "Pending",
                        MedicineName = schedule.Medicine.Name,
                        Dosage = schedule.Medicine.Dosage,
                        MealTiming = schedule.Medicine.MealTimeing
                    });
                }
            }
        }

        pendingMedicines = pendingMedicines.OrderBy(log => log.ScheduledTime).ToList();

        // Missed medicine logs for audit and history
        var missedMedicines = await _context.MedicineLogs
            .Include(log => log.Medicine)
            .Where(log =>
                log.PatientId == patient.Id &&
                log.Status == "Missed")
            .OrderByDescending(log => log.ScheduledTime)
            .Take(20)
            .ToListAsync();

        // Taken medicine logs for today & recent history
        var takenMedicines = await _context.MedicineLogs
            .Include(log => log.Medicine)
            .Where(log =>
                log.PatientId == patient.Id &&
                log.Status == "Taken")
            .OrderByDescending(log => log.TakenTime)
            .Take(20)
            .ToListAsync();

        // All logs for overall historical adherence calculation
        var allLogs = await _context.MedicineLogs
            .Where(log => log.PatientId == patient.Id)
            .ToListAsync();

        var totalDoses = allLogs.Count(log =>
            string.Equals(log.Status, "Taken", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(log.Status, "Missed", StringComparison.OrdinalIgnoreCase));

        var takenDoses = allLogs.Count(log =>
            string.Equals(log.Status, "Taken", StringComparison.OrdinalIgnoreCase));

        var missedDoses = allLogs.Count(log =>
            string.Equals(log.Status, "Missed", StringComparison.OrdinalIgnoreCase));

        var adherencePercentage = totalDoses == 0
            ? 100
            : Math.Round((double)takenDoses / totalDoses * 100, 2);

        // Low stock medicines
        var lowStockMedicines = medicines
            .Where(m => m.Stock <= 5)
            .ToList();

        // Active emergency alerts
        var activeEmergencies = await _context.EmergencyAlerts
            .Where(e =>
                e.PatientId == patient.Id &&
                e.Status == "ACTIVE")
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        // Unread notifications
        var unreadNotifications = await _context.Notifications
            .CountAsync(n =>
                n.UserId == userId &&
                !n.IsRead);

        // Patient information
        var patientInfo = new PatientInfoDto
        {
            Id = patient.Id,
            UserId = patient.UserId,
            Name = patient.User.Name,
            Email = patient.User.Email,
            DateOfBirth = patient.DateOfBirth,
            EmergencyContact = patient.EmergencyContact
        };

        // Final dashboard: returns only pending and upcoming doses for active routine display
        var dashboard = new PatientDashboardDto
        {
            Patient = patientInfo,

            TotalMedicines = totalMedicines,

            Medicines = medicines,

            TodaySchedules = pendingSchedules,

            PendingMedicines = pendingMedicines,

            MissedMedicines = missedMedicines,

            TakenMedicines = takenMedicines,

            TotalDoses = totalDoses,

            TakenDoses = takenDoses,

            MissedDoses = missedDoses,

            TodayTotalDoses = todayAllSchedules.Count,

            TodayTakenDoses = todayTakenCount,

            TodayMissedDoses = todayMissedCount,

            TodayPendingDoses = pendingMedicines.Count,

            AdherencePercentage = adherencePercentage,

            LowStockMedicines = lowStockMedicines,

            ActiveEmergencies = activeEmergencies,

            UnreadNotifications = unreadNotifications
        };

        return Ok(dashboard);
    }
}