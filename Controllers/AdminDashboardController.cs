using MediCare.Data;
using MediCare.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Controllers;

[ApiController]
[Route("api/dashboard/admin")]
[Authorize(Roles = "ADMIN")]
public class AdminDashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminDashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        // Total users
        var totalUsers = await _context.Users.CountAsync();

        // Total patients
        var totalPatients = await _context.Patients.CountAsync();

        // Total caregivers
        var totalCaregivers = await _context.Caregivers.CountAsync();

        // Total medicines
        var totalMedicines = await _context.Medicines.CountAsync();

        // Total medicine logs
        var totalMedicineLogs =
            await _context.MedicineLogs.CountAsync();

        // Taken doses
        var totalTakenDoses =
            await _context.MedicineLogs
                .CountAsync(log => log.Status == "Taken");

        // Missed doses
        var totalMissedDoses =
            await _context.MedicineLogs
                .CountAsync(log => log.Status == "Missed");

        // Pending doses
        var totalPendingDoses =
            await _context.MedicineLogs
                .CountAsync(log => log.Status == "Pending");

        // Overall adherence
        var completedDoses =
            totalTakenDoses + totalMissedDoses;

        var overallAdherencePercentage =
            completedDoses == 0
                ? 0
                : Math.Round(
                    (double)totalTakenDoses /
                    completedDoses * 100,
                    2);

        // Active emergency alerts
        var activeEmergencyAlerts =
            await _context.EmergencyAlerts
                .CountAsync(e => e.Status == "ACTIVE");

        // Total emergency alerts
        var totalEmergencyAlerts =
            await _context.EmergencyAlerts.CountAsync();

        // Unread notifications
        var unreadNotifications =
            await _context.Notifications
                .CountAsync(n => !n.IsRead);

        // Caregiver-patient relationships
        var caregiverPatientRelationships =
            await _context.CaregiverPatients.CountAsync();

        // Low stock medicines
        var lowStockMedicines =
            await _context.Medicines
                .Include(m => m.Patient)
                    .ThenInclude(p => p.User)
                .Where(m => m.Stock <= 5)
                .OrderBy(m => m.Stock)
                .ToListAsync();

        // Active emergency details
        var activeEmergencies =
            await _context.EmergencyAlerts
                .Include(e => e.Patient)
                    .ThenInclude(p => p.User)
                .Where(e => e.Status == "ACTIVE")
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

        // Final dashboard
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