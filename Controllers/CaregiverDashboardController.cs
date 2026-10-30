using System.Security.Claims;
using MediCare.Data;
using MediCare.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediCare.Models;

namespace MediCare.Controllers;

[ApiController]
[Route("api/dashboard/caregiver")]
[Authorize(Roles = "CAREGIVER")]
public class CaregiverDashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public CaregiverDashboardController(AppDbContext context)
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

        // Find caregiver profile
        var caregiver = await _context.Caregivers
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (caregiver == null)
            return NotFound("Caregiver profile not found.");

        // Find linked patients
        var linkedPatients = await _context.CaregiverPatients
            .Include(cp => cp.Patient)
                .ThenInclude(p => p.User)
            .Where(cp => cp.CaregiverId == caregiver.Id)
            .Select(cp => cp.Patient)
            .ToListAsync();

        var patientIds = linkedPatients
            .Select(p => p.Id)
            .ToList();

        // Caregiver information
        var caregiverInfo = new CaregiverInfoDto
        {
            Id = caregiver.Id,
            UserId = caregiver.UserId,
            Name = caregiver.User.Name,
            Email = caregiver.User.Email,
            Relationship = caregiver.Relationship
        };

        // Patient information
        var patients = linkedPatients
            .Select(p => new CaregiverDashboardPatientDto
            {
                PatientId = p.Id,
                UserId = p.UserId,
                Name = p.User.Name,
                Email = p.User.Email,
                DateOfBirth = p.DateOfBirth,
                EmergencyContact = p.EmergencyContact
            })
                    .ToList();

        // Active emergency alerts
        var activeEmergencies = await _context.EmergencyAlerts
            .Include(e => e.Patient)
                .ThenInclude(p => p.User)
            .Where(e =>
                patientIds.Contains(e.PatientId) &&
                e.Status == "ACTIVE")
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        // Missed medicines
        var missedMedicines = await _context.MedicineLogs
            .Include(log => log.Medicine)
            .Include(log => log.Patient)
                .ThenInclude(p => p.User)
            .Where(log =>
                patientIds.Contains(log.PatientId) &&
                log.Status == "Missed")
            .OrderByDescending(log => log.ScheduledTime)
            .Take(20)
            .ToListAsync();

        // Today's date and time
        var now = DateTime.Now;
        var today = now.Date;

        // Auto mark overdue pending or snoozed logs for linked patients as Missed
        var overdueLogs = await _context.MedicineLogs
            .Where(l => patientIds.Contains(l.PatientId) &&
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

        var todayAllSchedules = await _context.MedicineSchedules
            .Include(schedule => schedule.Medicine)
                .ThenInclude(m => m.Patient)
                    .ThenInclude(p => p.User)
            .Where(schedule =>
                !schedule.Medicine.IsDeleted &&
                patientIds.Contains(schedule.Medicine.PatientId) &&
                schedule.Medicine.StartDate.Date <= today &&
                schedule.Medicine.EndDate.Date >= today)
            .OrderBy(schedule => schedule.Time)
            .ToListAsync();

        // Today's medicine logs for pending calculation
        var todaysLogs = await _context.MedicineLogs
            .Include(log => log.Medicine)
            .Include(log => log.Patient)
                .ThenInclude(p => p.User)
            .Where(log =>
                patientIds.Contains(log.PatientId) &&
                (log.ScheduledTime.Date == today || (log.OriginalScheduledTime.HasValue && log.OriginalScheduledTime.Value.Date == today)))
            .ToListAsync();

        // Auto-create Missed logs for past schedules with no log
        bool hasNewMissedCg = false;
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
                hasNewMissedCg = true;
            }

            if (log == null && now > scheduledTime.AddMinutes(15))
            {
                log = new MedicineLog
                {
                    MedicineId = s.MedicineId,
                    Medicine = s.Medicine,
                    PatientId = s.Medicine.PatientId,
                    Patient = s.Medicine.Patient,
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
                hasNewMissedCg = true;
            }
        }

        if (hasNewMissedCg)
        {
            await _context.SaveChangesAsync();
        }

        var pendingMedicines = new List<MedicineLog>();
        var pendingSchedules = new List<MedicineSchedule>();

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

            if (log != null && (string.Equals(log.Status, "Taken", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(log.Status, "Missed", StringComparison.OrdinalIgnoreCase)))
            {
                // Completed or missed doses are excluded from pending dashboard section
                continue;
            }

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
                    PatientId = schedule.Medicine.PatientId,
                    Patient = schedule.Medicine.Patient,
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

        pendingMedicines = pendingMedicines.OrderBy(log => log.ScheduledTime).Take(20).ToList();

        // Adherence calculation
        var logs = await _context.MedicineLogs
            .Where(log => patientIds.Contains(log.PatientId))
            .ToListAsync();

        var adherence = linkedPatients
            .Select(patient =>
            {
                var patientLogs = logs
                    .Where(log => log.PatientId == patient.Id)
                    .ToList();

                var total = patientLogs.Count;

                var taken = patientLogs.Count(log =>
                    string.Equals(
                        log.Status,
                        "Taken",
                        StringComparison.OrdinalIgnoreCase));

                var missed = patientLogs.Count(log =>
                    string.Equals(
                        log.Status,
                        "Missed",
                        StringComparison.OrdinalIgnoreCase));

                var percentage = total == 0
                    ? 0
                    : Math.Round(
                        (double)taken / total * 100,
                        2);

                return new PatientAdherenceDto
                {
                    PatientId = patient.Id,
                    PatientName = patient.User.Name,
                    TotalDoses = total,
                    TakenDoses = taken,
                    MissedDoses = missed,
                    AdherencePercentage = percentage
                };
            })
            .ToList();

        // Unread notifications for this caregiver
        var unreadNotifications = await _context.Notifications
            .CountAsync(n =>
                n.UserId == userId &&
                !n.IsRead);

        // Recent intake activity logs across linked patients
        var recentLogs = await _context.MedicineLogs
            .Include(log => log.Medicine)
            .Include(log => log.Patient)
                .ThenInclude(p => p.User)
            .Where(log => patientIds.Contains(log.PatientId))
            .OrderByDescending(log => log.ScheduledTime)
            .Take(30)
            .ToListAsync();

        // Final dashboard response
        var dashboard = new CaregiverDashboardDto
        {
            Caregiver = caregiverInfo,
            Patients = patients,
            ActiveEmergencies = activeEmergencies,
            MissedMedicines = missedMedicines,
            TodaySchedules = pendingSchedules,
            PendingMedicines = pendingMedicines,
            RecentLogs = recentLogs,
            Adherence = adherence,
            UnreadNotifications = unreadNotifications
        };

        return Ok(dashboard);
    }
}