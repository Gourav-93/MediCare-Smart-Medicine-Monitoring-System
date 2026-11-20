
using System.Security.Claims;
using MediCare.Data;
using MediCare.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Controllers;

[ApiController]
[Route("api/dashboard/admin")]
[Authorize(Roles = "ADMIN")]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public class AdminDashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminDashboardController(AppDbContext context)
    {
        _context = context;
    }

    // Validate authenticated user identity.
    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(claim, out var id) && id > 0
            ? id
            : 0;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetCurrentUserId();

        if (userId <= 0)
        {
            return Unauthorized();
        }

        var now = DateTime.Now;

        // Overall system statistics.
        var totalUsers = await _context.Users
            .CountAsync();

        var totalPatients = await _context.Patients
            .CountAsync();

        var totalCaregivers = await _context.Caregivers
            .CountAsync();

        var totalMedicines = await _context.Medicines
            .CountAsync(m => !m.IsDeleted);

        // Preserve the total number of stored medicine logs
        // for administrative and historical reporting.
        var totalMedicineLogs = await _context.MedicineLogs
            .CountAsync();

        // Count only finalized occurrences that are due.
        // OriginalScheduledTime preserves occurrence identity
        // even when a medicine dose has been snoozed.
        var finalizedCounts = await _context.MedicineLogs
            .AsNoTracking()
            .Where(log =>
                (log.Status == "Taken" ||
                 log.Status == "Missed") &&

                (log.OriginalScheduledTime ??
                 log.ScheduledTime) <= now)
            .GroupBy(log => log.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync();

        var totalTakenDoses = finalizedCounts
            .Where(x => x.Status == "Taken")
            .Sum(x => x.Count);

        var totalMissedDoses = finalizedCounts
            .Where(x => x.Status == "Missed")
            .Sum(x => x.Count);

        // Pending and Snoozed are both unfinished doses.
        // Deleted medicines are excluded from active pending
        // statistics, while their historical logs are retained.
        var totalPendingDoses = await _context.MedicineLogs
            .AsNoTracking()
            .CountAsync(log =>
                !log.Medicine.IsDeleted &&
                (log.Status == "Pending" ||
                 log.Status == "Snoozed"));

        // Only finalized doses contribute to adherence.
        var completedDoses =
            totalTakenDoses + totalMissedDoses;

        var overallAdherencePercentage =
            completedDoses == 0
                ? 0
                : Math.Round(
                    (double)totalTakenDoses /
                    completedDoses * 100,
                    2);

        // Emergency alert statistics.
        var activeEmergencyAlerts =
            await _context.EmergencyAlerts
                .CountAsync(e =>
                    e.Status == "ACTIVE");

        var totalEmergencyAlerts =
            await _context.EmergencyAlerts
                .CountAsync();

        // System-wide unread notification count.
        var unreadNotifications =
            await _context.Notifications
                .CountAsync(n => !n.IsRead);

        // Count only patient-approved relationships.
        var caregiverPatientRelationships =
            await _context.CaregiverPatients
                .CountAsync(cp =>
                    cp.Status == "APPROVED");

        // Low-stock medicines.
        var lowStockMedicines =
            await _context.Medicines
                .AsNoTracking()
                .Include(m => m.Patient)
                    .ThenInclude(p => p.User)
                .Where(m =>
                    !m.IsDeleted &&
                    m.Stock <= 5)
                .OrderBy(m => m.Stock)
                .ToListAsync();

        // Active emergency records.
        var activeEmergencies =
            await _context.EmergencyAlerts
                .AsNoTracking()
                .Include(e => e.Patient)
                    .ThenInclude(p => p.User)
                .Where(e =>
                    e.Status == "ACTIVE")
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

        // Preserve the existing dashboard response structure.
        var dashboard = new AdminDashboardDto
        {
            TotalUsers = totalUsers,

            TotalPatients = totalPatients,

            TotalCaregivers = totalCaregivers,

            TotalMedicines = totalMedicines,

            TotalMedicineLogs = totalMedicineLogs,

            TotalTakenDoses = totalTakenDoses,

            TotalMissedDoses = totalMissedDoses,

            TotalPendingDoses = totalPendingDoses,

            OverallAdherencePercentage =
                overallAdherencePercentage,

            ActiveEmergencyAlerts =
                activeEmergencyAlerts,

            TotalEmergencyAlerts =
                totalEmergencyAlerts,

            UnreadNotifications =
                unreadNotifications,

            CaregiverPatientRelationships =
                caregiverPatientRelationships,

            LowStockMedicines =
                lowStockMedicines,

            ActiveEmergencies =
                activeEmergencies
        };

        return Ok(dashboard);
    }
}
