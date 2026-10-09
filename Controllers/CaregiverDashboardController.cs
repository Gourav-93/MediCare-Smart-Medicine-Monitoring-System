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
        return int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetCurrentUserId();

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

        // Today's medicine schedules
        var today = DateTime.Today;

        var todaySchedules = await _context.MedicineSchedules
            .Include(schedule => schedule.Medicine)
            .Where(schedule =>
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
                log.ScheduledTime.Date == today)
            .ToListAsync();

        var pendingMedicines = new List<MedicineLog>();

        foreach (var schedule in todaySchedules)
        {
            var scheduledTime = today.Add(schedule.Time);
            var log = todaysLogs.FirstOrDefault(l =>
                l.MedicineId == schedule.MedicineId &&
                l.ScheduledTime == scheduledTime);

            if (log == null)
            {
                pendingMedicines.Add(new MedicineLog
                {
                    MedicineId = schedule.MedicineId,
                    Medicine = schedule.Medicine,
                    PatientId = schedule.Medicine.PatientId,
                    ScheduledTime = scheduledTime,
                    Status = "Pending"
                });
            }
            else if (log.Status == "Pending")
            {
                pendingMedicines.Add(log);
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

        // Final dashboard response
        var dashboard = new CaregiverDashboardDto
        {
            Caregiver = caregiverInfo,
            Patients = patients,
            ActiveEmergencies = activeEmergencies,
            MissedMedicines = missedMedicines,
            TodaySchedules = todaySchedules,
            PendingMedicines = pendingMedicines,
            Adherence = adherence,
            UnreadNotifications = unreadNotifications
        };

        return Ok(dashboard);
    }
}