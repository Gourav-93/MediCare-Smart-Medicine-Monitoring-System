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
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
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

    private static bool IsEffectiveOccurrence(
        MedicineSchedule schedule, DateTime occurrence)
    {
        if (!string.Equals(schedule.Frequency, "Daily", StringComparison.OrdinalIgnoreCase) ||
            schedule.Time < TimeSpan.Zero || schedule.Time >= TimeSpan.FromDays(1) ||
            schedule.Medicine.IsDeleted ||
            occurrence.Date < schedule.Medicine.StartDate.Date ||
            occurrence.Date > schedule.Medicine.EndDate.Date ||
            !schedule.EffectiveFrom.HasValue)
            return false;

        var wallTime = DateTime.SpecifyKind(occurrence, DateTimeKind.Unspecified);
        if (TimeZoneInfo.Local.IsInvalidTime(wallTime) ||
            TimeZoneInfo.Local.IsAmbiguousTime(wallTime))
            return false;

        var utc = TimeZoneInfo.ConvertTimeToUtc(wallTime, TimeZoneInfo.Local);
        return utc >= schedule.EffectiveFrom.Value &&
            (!schedule.EffectiveUntil.HasValue ||
             utc < schedule.EffectiveUntil.Value) &&
            (!schedule.IsRetired || schedule.EffectiveUntil.HasValue);
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
            return Unauthorized();

        // Find caregiver profile
        var caregiver = await _context.Caregivers
            .AsNoTracking()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (caregiver == null)
            return NotFound("Caregiver profile not found.");

        // Find only patient-approved relationships
        var linkedPatients = await _context.CaregiverPatients
            .AsNoTracking()
            .Include(cp => cp.Patient)
                .ThenInclude(p => p.User)
            .Where(cp =>
                cp.CaregiverId == caregiver.Id &&
                cp.Status == "APPROVED")
            .Select(cp => cp.Patient)
            .ToListAsync();

        var patientIds = linkedPatients
            .Select(p => p.Id)
            .Distinct()
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
            .AsNoTracking()
            .Include(e => e.Patient)
                .ThenInclude(p => p.User)
            .Where(e =>
                patientIds.Contains(e.PatientId) &&
                e.Status == "ACTIVE")
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        // Read-only dashboard: recovery and status transitions belong to the worker.
        var now = DateTime.Now;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var overdueCutoff = now.AddMinutes(-15);

        var missedMedicines = await _context.MedicineLogs
            .AsNoTracking()
            .Include(log => log.Medicine)
            .Include(log => log.Patient)
                .ThenInclude(p => p.User)
            .Where(log => patientIds.Contains(log.PatientId) && log.Status == "Missed")
            .OrderByDescending(log => log.ScheduledTime)
            .Take(20)
            .ToListAsync();

        var todayAllSchedules = await _context.MedicineSchedules
            .AsNoTracking()
            .Include(schedule => schedule.Medicine)
                .ThenInclude(m => m.Patient)
                    .ThenInclude(p => p.User)
            .Where(schedule =>
                !schedule.Medicine.IsDeleted &&
                schedule.Frequency.ToUpper() == "DAILY" &&
                patientIds.Contains(schedule.Medicine.PatientId) &&
                schedule.Medicine.StartDate.Date <= today &&
                schedule.Medicine.EndDate.Date >= today)
            .OrderBy(schedule => schedule.Time)
            .ToListAsync();

        // Match against all effective schedule identities, including records
        // with inconsistent patient IDs. Such records are never returned.
        todayAllSchedules = todayAllSchedules
            .Where(s => s.Time >= TimeSpan.Zero &&
                        s.Time < TimeSpan.FromDays(1) &&
                        IsEffectiveOccurrence(s, today.Add(s.Time)))
            .ToList();

        var todayScheduleIds = todayAllSchedules.Select(s => s.Id).ToList();

        // Today's medicine logs for pending calculation
        var todaysLogs = await _context.MedicineLogs
            .AsNoTracking()
            .Include(log => log.Medicine)
            .Include(log => log.Patient)
                .ThenInclude(p => p.User)
            .Where(log =>
                (patientIds.Contains(log.PatientId) ||
                 (log.ScheduleId.HasValue &&
                  todayScheduleIds.Contains(log.ScheduleId.Value))) &&
                ((log.OriginalScheduledTime.HasValue &&
                  log.OriginalScheduledTime.Value >= today &&
                  log.OriginalScheduledTime.Value < tomorrow) ||
                 (!log.OriginalScheduledTime.HasValue &&
                  log.ScheduledTime >= today && log.ScheduledTime < tomorrow)))
            .ToListAsync();

        var pendingMedicines = new List<MedicineLog>();
        var pendingSchedules = new List<MedicineSchedule>();

        foreach (var schedule in todayAllSchedules)
        {
            var scheduledTime = today.Add(schedule.Time);

            var matches = todaysLogs.Where(l =>
                l.ScheduleId == schedule.Id &&
                l.OriginalScheduledTime == scheduledTime)
                .Take(2).ToList();

            if (matches.Count > 1 || matches.Any(l =>
                l.MedicineId != schedule.MedicineId ||
                l.PatientId != schedule.Medicine.PatientId))
                continue;

            var log = matches.SingleOrDefault();

            // Do not present an actionable occurrence if a legacy
            // record with incomplete identity requires reconciliation.
            var hasLegacy = todaysLogs.Any(l =>
                (l.ScheduleId == null || l.OriginalScheduledTime == null) &&
                (l.ScheduleId == schedule.Id ||
                 (l.ScheduleId == null &&
                  l.MedicineId == schedule.MedicineId &&
                  l.PatientId == schedule.Medicine.PatientId)) &&
                (l.OriginalScheduledTime ?? l.ScheduledTime) == scheduledTime);

            if (hasLegacy)
                continue;

            // Cancelled occurrences remain in history, not the active routine.
            if (log?.Status == "Cancelled")
                continue;

            if (log != null && log.Status != "Pending" &&
                log.Status != "Snoozed")
                continue;

            // Do not present expired doses as pending while recovery catches up.
            if ((log?.ScheduledTime ?? scheduledTime) <= overdueCutoff)
                continue;

            pendingSchedules.Add(schedule);

            if (log != null)
            {
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

        // Aggregate finalized occurrences in the database instead of
        // loading every historical log into application memory.
        var finalizedCounts = await _context.MedicineLogs
            .AsNoTracking()
            .Where(log => patientIds.Contains(log.PatientId) &&
                (log.Status == "Taken" || log.Status == "Missed") &&
                (log.OriginalScheduledTime ?? log.ScheduledTime) <= now)
            .GroupBy(log => new { log.PatientId, log.Status })
            .Select(group => new
            {
                group.Key.PatientId,
                group.Key.Status,
                Count = group.Count()
            })
            .ToListAsync();

        var countsByPatient = finalizedCounts.ToLookup(x => x.PatientId);

        var adherence = linkedPatients
            .Select(patient =>
            {
                var counts = countsByPatient[patient.Id];
                var taken = counts.Where(x => string.Equals(
                    x.Status, "Taken", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.Count);
                var missed = counts.Where(x => string.Equals(
                    x.Status, "Missed", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.Count);
                var total = taken + missed;

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
            .AsNoTracking()
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