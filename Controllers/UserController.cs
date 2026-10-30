using System;
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
[Route("api/user")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserController> _logger;

    public UserController(
        AppDbContext context,
        ILogger<UserController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private static int? CalculateAge(DateTime? dob)
    {
        if (!dob.HasValue || dob.Value == DateTime.MinValue) return null;
        var today = DateTime.Today;
        if (dob.Value.Date > today) return null;
        var age = today.Year - dob.Value.Year;
        if (dob.Value.Date > today.AddYears(-age)) age--;
        return age >= 0 ? age : 0;
    }

    // GET: api/user/profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
            return Unauthorized(new { message = "User identity claim not found in token." });

        var user = await _context.Users
            .Include(u => u.Patient)
            .Include(u => u.Caregiver)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound(new { message = "User record not found." });

        double? adherenceScore = null;
        int activeMedCount = 0;

        if (user.Patient != null)
        {
            activeMedCount = await _context.Medicines
                .CountAsync(m => m.PatientId == user.Patient.Id && !m.IsDeleted);

            var totalLogs = await _context.MedicineLogs
                .CountAsync(l => l.PatientId == user.Patient.Id);
            if (totalLogs > 0)
            {
                var takenLogs = await _context.MedicineLogs
                    .CountAsync(l => l.PatientId == user.Patient.Id && l.Status == "Taken");
                adherenceScore = Math.Round((double)takenLogs / totalLogs * 100, 1);
            }
            else
            {
                adherenceScore = 100.0;
            }
        }

        var profile = new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = !string.IsNullOrWhiteSpace(user.PhoneNumber)
                ? user.PhoneNumber
                : (user.Patient?.EmergencyContact ?? string.Empty),
            Role = user.Role,
            DateOfBirth = user.Patient?.DateOfBirth,
            Age = CalculateAge(user.Patient?.DateOfBirth),
            Gender = user.Gender,
            Address = user.Address,
            City = user.City,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            BloodGroup = user.Patient?.BloodGroup ?? string.Empty,
            EmergencyContact = user.Patient?.EmergencyContact ?? string.Empty,
            Relationship = user.Caregiver?.Relationship ?? string.Empty,
            AdherencePercentage = adherenceScore,
            ActiveMedicineCount = activeMedCount
        };

        return Ok(profile);
    }

    // PUT: api/user/profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
            return Unauthorized(new { message = "User identity claim not found in token." });

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest(new { message = "Full Name is required." });

        var user = await _context.Users
            .Include(u => u.Patient)
            .Include(u => u.Caregiver)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound(new { message = "User record not found." });

        // Update core user fields
        user.Name = dto.FullName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            user.PhoneNumber = dto.PhoneNumber.Trim();
        }
        if (dto.Gender != null) user.Gender = dto.Gender.Trim();
        if (dto.Address != null) user.Address = dto.Address.Trim();
        if (dto.City != null) user.City = dto.City.Trim();

        // Update role-specific entity fields
        if (user.Patient != null)
        {
            if (dto.DateOfBirth.HasValue)
            {
                user.Patient.DateOfBirth = dto.DateOfBirth.Value.Date;
            }
            if (!string.IsNullOrWhiteSpace(dto.BloodGroup))
            {
                user.Patient.BloodGroup = dto.BloodGroup.Trim();
            }
        }

        if (user.Caregiver != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.Relationship))
            {
                user.Caregiver.Relationship = dto.Relationship.Trim();
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Profile updated successfully.",
            profile = new UserProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                DateOfBirth = user.Patient?.DateOfBirth,
                Age = CalculateAge(user.Patient?.DateOfBirth),
                Gender = user.Gender,
                Address = user.Address,
                City = user.City,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                BloodGroup = user.Patient?.BloodGroup ?? string.Empty,
                EmergencyContact = user.Patient?.EmergencyContact ?? string.Empty,
                Relationship = user.Caregiver?.Relationship ?? string.Empty
            }
        });
    }

    // GET: api/user/preferences
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
            return Unauthorized(new { message = "User identity claim not found in token." });

        var pref = await _context.UserNotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (pref == null)
        {
            pref = new UserNotificationPreference
            {
                UserId = userId,
                DoseReminders = true,
                WhatsAppReminders = true,
                CaregiverAlerts = true,
                RefillAlerts = true,
                LowStockAlerts = true,
                EmailNotifications = true,
                InAppNotifications = true,
                EmergencyAlerts = true,
                MissedDoseAlerts = true,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserNotificationPreferences.Add(pref);
            await _context.SaveChangesAsync();
        }

        var dto = new UserNotificationPreferenceDto
        {
            DoseReminders = pref.DoseReminders,
            WhatsAppReminders = pref.WhatsAppReminders,
            CaregiverAlerts = pref.CaregiverAlerts,
            RefillAlerts = pref.RefillAlerts,
            LowStockAlerts = pref.LowStockAlerts,
            EmailNotifications = pref.EmailNotifications,
            InAppNotifications = pref.InAppNotifications,
            EmergencyAlerts = pref.EmergencyAlerts,
            MissedDoseAlerts = pref.MissedDoseAlerts
        };

        return Ok(dto);
    }

    // PUT: api/user/preferences
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UserNotificationPreferenceDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
            return Unauthorized(new { message = "User identity claim not found in token." });

        var pref = await _context.UserNotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (pref == null)
        {
            pref = new UserNotificationPreference
            {
                UserId = userId
            };
            _context.UserNotificationPreferences.Add(pref);
        }

        pref.DoseReminders = dto.DoseReminders;
        pref.WhatsAppReminders = dto.WhatsAppReminders;
        pref.CaregiverAlerts = dto.CaregiverAlerts;
        pref.RefillAlerts = dto.RefillAlerts;
        pref.LowStockAlerts = dto.LowStockAlerts;
        pref.EmailNotifications = dto.EmailNotifications;
        pref.InAppNotifications = dto.InAppNotifications;
        pref.EmergencyAlerts = dto.EmergencyAlerts;
        pref.MissedDoseAlerts = dto.MissedDoseAlerts;
        pref.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Notification preferences updated successfully.",
            preferences = dto
        });
    }
}
