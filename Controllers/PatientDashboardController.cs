using System.Security.Claims;
using MediCare.Data;
using MediCare.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        return int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetCurrentUserId();

        // Find patient profile
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return NotFound("Patient profile not found.");

        // Get all medicines
        var medicines = await _context.Medicines
            .Where(m => m.PatientId == patient.Id)
            .OrderBy(m => m.Name)
            .ToListAsync();

        // Total medicines
        var totalMedicines = medicines.Count;

        // Today's date
        var today = DateTime.Today;

        // Today's schedules
        var todaySchedules = await _context.MedicineSchedules
            .Include(s => s.Medicine)
            .Where(s =>
                s.Medicine.PatientId == patient.Id &&
                s.Medicine.StartDate.Date <= today &&
                s.Medicine.EndDate.Date >= today)
            .OrderBy(s => s.Time)
            .ToListAsync();

        // Pending medicine logs
        var pendingMedicines = await _context.MedicineLogs
            .Include(log => log.Medicine)
            .Where(log =>
                log.PatientId == patient.Id &&
                log.Status == "Pending")
            .OrderBy(log => log.ScheduledTime)
            .ToListAsync();

        // Missed medicine logs
        var missedMedicines = await _context.MedicineLogs
            .Include(log => log.Medicine)
            .Where(log =>
                log.PatientId == patient.Id &&
                log.Status == "Missed")
            .OrderByDescending(log => log.ScheduledTime)
            .Take(20)
            .ToListAsync();

        // Taken medicine logs
        var takenMedicines = await _context.MedicineLogs
            .Include(log => log.Medicine)
            .Where(log =>
                log.PatientId == patient.Id &&
                log.Status == "Taken")
            .OrderByDescending(log => log.TakenTime)
            .Take(20)
            .ToListAsync();

        // All logs for adherence
        var allLogs = await _context.MedicineLogs
            .Where(log => log.PatientId == patient.Id)
            .ToListAsync();

        var totalDoses = allLogs.Count;

        var takenDoses = allLogs.Count(log =>
            string.Equals(
                log.Status,
                "Taken",
                StringComparison.OrdinalIgnoreCase));

        var missedDoses = allLogs.Count(log =>
            string.Equals(
                log.Status,
                "Missed",
                StringComparison.OrdinalIgnoreCase));

        var adherencePercentage = totalDoses == 0
            ? 0
            : Math.Round(
                (double)takenDoses / totalDoses * 100,
                2);

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

        // Final dashboard
        var dashboard = new PatientDashboardDto
        {
            Patient = patientInfo,

            TotalMedicines = totalMedicines,

            Medicines = medicines,

            TodaySchedules = todaySchedules,

            PendingMedicines = pendingMedicines,

            MissedMedicines = missedMedicines,

            TakenMedicines = takenMedicines,

            TotalDoses = totalDoses,

            TakenDoses = takenDoses,

            MissedDoses = missedDoses,

            AdherencePercentage = adherencePercentage,

            LowStockMedicines = lowStockMedicines,

            ActiveEmergencies = activeEmergencies,

            UnreadNotifications = unreadNotifications
        };

        return Ok(dashboard);
    }
}