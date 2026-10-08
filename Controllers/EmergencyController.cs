using System.Security.Claims;
using MediCare.Data;
using MediCare.DTOs;
using MediCare.Models;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Controllers;

[ApiController]
[Route("api/emergency")]
[Authorize(Roles = "PATIENT,CAREGIVER,ADMIN")]
public class EmergencyController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public EmergencyController(
        AppDbContext context,
        IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    private int GetCurrentUserId()
    {
        return int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );
    }

    private string GetCurrentRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? "";
    }

    // POST: api/emergency
    [HttpPost]
    [Authorize(Roles = "PATIENT")]
    public async Task<IActionResult> CreateEmergency(
        EmergencyAlertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest("Emergency message is required.");

        var userId = GetCurrentUserId();

        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return NotFound("Patient profile not found.");

        var emergency = new EmergencyAlert
        {
            PatientId = patient.Id,
            Message = dto.Message.Trim(),
            Status = "ACTIVE",
            CreatedAt = DateTime.Now
        };

        _context.EmergencyAlerts.Add(emergency);

        // Find all linked caregivers
        var caregivers = await _context.CaregiverPatients
            .Include(cp => cp.Caregiver)
                .ThenInclude(c => c.User)
            .Where(cp => cp.PatientId == patient.Id)
            .ToListAsync();

        foreach (var caregiverPatient in caregivers)
        {
            var caregiver = caregiverPatient.Caregiver;

            // Database notification
            var notification = new Notification
            {
                UserId = caregiver.UserId,
                Title = "🚨 Emergency Alert",
                Message =
                    $"Emergency alert from patient {patient.User.Name}: " +
                    $"{dto.Message}",
                Type = "EmergencyAlert",
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);

            // Email notification
            if (!string.IsNullOrWhiteSpace(
                    caregiver.User.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        caregiver.User.Email,
                        "🚨 Emergency Alert - MediCare",
                        $"Hello {caregiver.User.Name},\n\n" +
                        $"An emergency alert has been triggered by " +
                        $"patient {patient.User.Name}.\n\n" +
                        $"Message: {dto.Message}\n\n" +
                        $"Please check on the patient immediately.\n\n" +
                        $"Regards,\n" +
                        $"MediCare"
                    );
                }
                catch (Exception ex)
                {
                    // Email failure should not stop emergency creation
                    Console.WriteLine(
                        $"Emergency email failed: {ex.Message}"
                    );
                }
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Emergency alert created successfully.",
            emergencyId = emergency.Id,
            status = emergency.Status,
            caregiversNotified = caregivers.Count
        });
    }

    // GET: api/emergency
    [HttpGet]
    public async Task<IActionResult> GetEmergencies()
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentRole();

        if (role == "PATIENT")
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null)
                return NotFound("Patient profile not found.");

            var emergencies = await _context.EmergencyAlerts
                .Where(e => e.PatientId == patient.Id)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return Ok(emergencies);
        }

        if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (caregiver == null)
                return NotFound("Caregiver profile not found.");

            var emergencies = await _context.EmergencyAlerts
                .Include(e => e.Patient)
                    .ThenInclude(p => p.User)
                .Where(e =>
                    _context.CaregiverPatients.Any(cp =>
                        cp.CaregiverId == caregiver.Id &&
                        cp.PatientId == e.PatientId))
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return Ok(emergencies);
        }

        // ADMIN
        var allEmergencies = await _context.EmergencyAlerts
            .Include(e => e.Patient)
                .ThenInclude(p => p.User)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return Ok(allEmergencies);
    }

    // PUT: api/emergency/{id}/resolve
    [HttpPut("{id}/resolve")]
    public async Task<IActionResult> ResolveEmergency(int id)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentRole();

        var emergency = await _context.EmergencyAlerts
            .Include(e => e.Patient)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (emergency == null)
            return NotFound("Emergency alert not found.");

        if (role == "PATIENT")
        {
            if (emergency.Patient.UserId != userId)
                return Forbid();
        }
        else if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (caregiver == null)
                return Forbid();

            var linked = await _context.CaregiverPatients
                .AnyAsync(cp =>
                    cp.CaregiverId == caregiver.Id &&
                    cp.PatientId == emergency.PatientId);

            if (!linked)
                return Forbid();
        }

        if (emergency.Status == "RESOLVED")
            return BadRequest("Emergency is already resolved.");

        emergency.Status = "RESOLVED";
        emergency.ResolvedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Emergency resolved successfully.",
            emergencyId = emergency.Id,
            status = emergency.Status,
            resolvedAt = emergency.ResolvedAt
        });
    }
}