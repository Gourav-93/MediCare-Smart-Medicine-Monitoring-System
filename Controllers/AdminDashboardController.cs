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
        var totalUsers = await _context.Users.CountAsync();
        var totalPatients = await _context.Patients.CountAsync();
        var totalCaregivers = await _context.Caregivers.CountAsync();
        var totalMedicines = await _context.Medicines.CountAsync(m => !m.IsDeleted);
        var totalMedicineLogs = await _context.MedicineLogs.CountAsync();
        var totalTakenDoses = await _context.MedicineLogs.CountAsync(log => log.Status == "Taken");
        var totalMissedDoses = await _context.MedicineLogs.CountAsync(log => log.Status == "Missed");
        var totalPendingDoses = await _context.MedicineLogs.CountAsync(log => log.Status == "Pending");
        var completedDoses = totalTakenDoses + totalMissedDoses;

        var overallAdherencePercentage =
            completedDoses == 0
                ? 0
                : Math.Round(
                    (double)totalTakenDoses /
                    completedDoses * 100,
                    2);

        var activeEmergencyAlerts =
            await _context.EmergencyAlerts
                .CountAsync(e => e.Status == "ACTIVE");

        var totalEmergencyAlerts =
            await _context.EmergencyAlerts.CountAsync();

        var unreadNotifications =
            await _context.Notifications
                .CountAsync(n => !n.IsRead);

        var caregiverPatientRelationships =
            await _context.CaregiverPatients.CountAsync();

        var lowStockMedicines =
            await _context.Medicines
                .Include(m => m.Patient)
                .ThenInclude(p => p.User)
                .Where(m => !m.IsDeleted && m.Stock <= 5)
                .OrderBy(m => m.Stock)
                .ToListAsync();

        var activeEmergencies =
            await _context.EmergencyAlerts
                .Include(e => e.Patient)
                    .ThenInclude(p => p.User)
                .Where(e => e.Status == "ACTIVE")
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

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