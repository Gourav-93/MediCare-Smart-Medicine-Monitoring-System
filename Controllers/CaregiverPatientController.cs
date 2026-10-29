using System.Security.Claims;
using MediCare.Data;
using MediCare.DTOs;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Controllers;

[ApiController]
[Route("api/Caregiverpatient")]
[Authorize]
public class CaregiverPatientController : ControllerBase
{
    private readonly ICaregiverPatientService _service;
    private readonly AppDbContext _context;

    public CaregiverPatientController(
        ICaregiverPatientService service,
        AppDbContext context)
    {
        _service = service;
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userId, out var id))
        {
            return 0;
        }

        return id;
    }

    private string GetCurrentRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }

    [HttpPost("link")]
    [Authorize(Roles = "ADMIN,CAREGIVER,PATIENT")]
    public async Task<IActionResult> LinkPatient(
        CaregiverPatientDto dto)
    {
        var role = GetCurrentRole();
        var currentUserId = GetCurrentUserId();

        if (currentUserId <= 0)
        {
            return Unauthorized();
        }

        if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers
                .FirstOrDefaultAsync(c => c.UserId == currentUserId);

            if (caregiver == null)
            {
                return NotFound(new { message = "Caregiver profile not found." });
            }

            dto.CaregiverId = caregiver.Id;

            // Resolve Patient by Email if PatientId is not provided
            if (dto.PatientId <= 0 && !string.IsNullOrWhiteSpace(dto.PatientEmail))
            {
                var pUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.PatientEmail.Trim().ToLower() && u.Role == "PATIENT");

                if (pUser == null)
                {
                    return NotFound(new { message = $"No registered Patient found with email '{dto.PatientEmail}'." });
                }

                var pat = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == pUser.Id);

                if (pat == null)
                {
                    return NotFound(new { message = "Patient profile record not found." });
                }

                dto.PatientId = pat.Id;
            }

            if (dto.PatientId <= 0)
            {
                return BadRequest(new { message = "Valid PatientId or PatientEmail is required." });
            }
        }
        else if (role == "PATIENT")
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == currentUserId);

            if (patient == null)
            {
                return NotFound("Patient profile not found.");
            }

            // Ensure patient can only link for their own profile
            if (dto.PatientId > 0 && dto.PatientId != patient.Id)
            {
                return Forbid();
            }

            dto.PatientId = patient.Id;

            // Resolve Caregiver by Email if CaregiverId is not provided
            if (dto.CaregiverId <= 0 && !string.IsNullOrWhiteSpace(dto.CaregiverEmail))
            {
                var cgUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.CaregiverEmail.ToLower() && u.Role == "CAREGIVER");

                if (cgUser == null)
                {
                    return NotFound("Caregiver with that email not found.");
                }

                var cg = await _context.Caregivers
                    .FirstOrDefaultAsync(c => c.UserId == cgUser.Id);

                if (cg == null)
                {
                    return NotFound("Caregiver profile not found.");
                }

                dto.CaregiverId = cg.Id;
            }

            if (dto.CaregiverId <= 0)
            {
                return BadRequest("Valid CaregiverId or CaregiverEmail is required.");
            }
        }
        else if (role == "ADMIN")
        {
            if (dto.CaregiverId <= 0)
            {
                return BadRequest("Valid caregiverId is required.");
            }

            if (dto.PatientId <= 0 && string.IsNullOrWhiteSpace(dto.PatientEmail))
            {
                return BadRequest("Either PatientId or PatientEmail is required.");
            }

            if (dto.PatientId <= 0 && !string.IsNullOrWhiteSpace(dto.PatientEmail))
            {
                var pUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.PatientEmail.ToLower() && u.Role == "PATIENT");

                if (pUser == null)
                {
                    return NotFound("Patient with that email not found.");
                }

                var pat = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == pUser.Id);

                if (pat == null)
                {
                    return NotFound("Patient profile not found.");
                }

                dto.PatientId = pat.Id;
            }
        }

        var caregiverExists = await _context.Caregivers
            .AnyAsync(c => c.Id == dto.CaregiverId);

        if (!caregiverExists)
        {
            return NotFound("Caregiver not found.");
        }

        var patientExists = await _context.Patients
            .AnyAsync(p => p.Id == dto.PatientId);

        if (!patientExists)
        {
            return NotFound("Patient not found.");
        }

        try
        {
            var result = await _service.LinkPatientAsync(dto);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("caregiver/{caregiverId}")]
    [Authorize(Roles = "ADMIN,CAREGIVER")]
    public async Task<IActionResult> GetPatients(
        int caregiverId)
    {
        if (caregiverId <= 0)
        {
            return BadRequest("Invalid caregiverId.");
        }

        var role = GetCurrentRole();
        var currentUserId = GetCurrentUserId();

        var caregiver = await _context.Caregivers
            .FirstOrDefaultAsync(c => c.Id == caregiverId);

        if (caregiver == null)
        {
            return NotFound("Caregiver not found.");
        }

        // Caregiver can only see his own linked patients.
        if (role == "CAREGIVER" &&
            caregiver.UserId != currentUserId)
        {
            return Forbid();
        }

        var patients = await _service
            .GetPatientsAsync(caregiverId);

        return Ok(patients);
    }

    // =========================================================
    // GET CAREGIVERS OF PATIENT
    // ADMIN -> any patient
    // PATIENT -> only himself
    // CAREGIVER -> only patients linked to himself
    // =========================================================

    [HttpGet("patient/{patientId}")]
    [Authorize(Roles = "ADMIN,PATIENT,CAREGIVER")]
    public async Task<IActionResult> GetCaregivers(
        int patientId)
    {
        if (patientId <= 0)
        {
            return BadRequest("Invalid patientId.");
        }

        var role = GetCurrentRole();
        var currentUserId = GetCurrentUserId();

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient == null)
        {
            return NotFound("Patient not found.");
        }

        // Patient can only see caregivers connected to himself.
        if (role == "PATIENT" &&
            patient.UserId != currentUserId)
        {
            return Forbid();
        }

        // Caregiver can only see caregivers of a patient
        // who is linked to that caregiver.
        if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers
                .FirstOrDefaultAsync(c => c.UserId == currentUserId);

            if (caregiver == null)
            {
                return Forbid();
            }

            var isLinked = await _context.CaregiverPatients
                .AnyAsync(cp =>
                    cp.CaregiverId == caregiver.Id &&
                    cp.PatientId == patientId);

            if (!isLinked)
            {
                return Forbid();
            }
        }

        var caregivers = await _service
            .GetCaregiversAsync(patientId);

        return Ok(caregivers);
    }

    // =========================================================
    // GET MY CAREGIVERS (READ-ONLY FOR PATIENT)
    // Uses logged-in patient identity from validated JWT claim.
    // Does not trust client-supplied patientId.
    // Returns only safe, read-only profile fields for linked caregivers.
    // =========================================================
    [HttpGet("my-caregivers")]
    [Authorize(Roles = "PATIENT,ADMIN")]
    public async Task<IActionResult> GetMyCaregivers()
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId <= 0)
        {
            return Unauthorized();
        }

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == currentUserId);

        if (patient == null)
        {
            return NotFound(new { message = "Patient profile record not found." });
        }

        var links = await _context.CaregiverPatients
            .Include(cp => cp.Caregiver)
                .ThenInclude(c => c.User)
            .Where(cp => cp.PatientId == patient.Id)
            .OrderByDescending(cp => cp.IsPrimary)
            .ThenByDescending(cp => cp.CreatedAt)
            .ToListAsync();

        var dtos = links.Where(cp => cp.Caregiver?.User != null).Select(cp => new PatientLinkedCaregiverDto
        {
            CaregiverPatientId = cp.Id,
            CaregiverId = cp.CaregiverId,
            UserId = cp.Caregiver.UserId,
            FullName = cp.Caregiver.User.Name,
            Email = cp.Caregiver.User.Email,
            PhoneNumber = cp.Caregiver.User.PhoneNumber ?? string.Empty,
            Gender = cp.Caregiver.User.Gender,
            Address = cp.Caregiver.User.Address,
            City = cp.Caregiver.User.City,
            ProfilePhotoUrl = cp.Caregiver.User.ProfilePhotoUrl,
            Relationship = cp.Caregiver.Relationship ?? string.Empty,
            RelationType = string.IsNullOrWhiteSpace(cp.RelationType) ? (cp.Caregiver.Relationship ?? "Family Caregiver") : cp.RelationType,
            PreferredContactMethod = cp.Caregiver.PreferredContactMethod ?? "Phone / Email",
            CaregivingNotes = cp.CaregivingNotes,
            IsPrimary = cp.IsPrimary,
            LinkedSince = cp.CreatedAt
        }).ToList();

        return Ok(dtos);
    }

    [HttpDelete("unlink")]
    [Authorize(Roles = "ADMIN,CAREGIVER,PATIENT")]
    public async Task<IActionResult> UnlinkPatient(
        int caregiverId,
        int patientId)
    {
        if (caregiverId <= 0 || patientId <= 0)
        {
            return BadRequest("Valid caregiverId and patientId are required.");
        }

        var role = GetCurrentRole();
        var currentUserId = GetCurrentUserId();

        if (currentUserId <= 0)
        {
            return Unauthorized();
        }

        var caregiver = await _context.Caregivers
            .FirstOrDefaultAsync(c => c.Id == caregiverId);

        if (caregiver == null)
        {
            return NotFound("Caregiver not found.");
        }

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient == null)
        {
            return NotFound("Patient not found.");
        }

        // Caregiver can only unlink their own relationship.
        if (role == "CAREGIVER" &&
            caregiver.UserId != currentUserId)
        {
            return Forbid();
        }

        // Patient can only unlink their own relationship.
        if (role == "PATIENT" &&
            patient.UserId != currentUserId)
        {
            return Forbid();
        }

        var relationshipExists = await _context.CaregiverPatients
            .AnyAsync(cp =>
                cp.CaregiverId == caregiverId &&
                cp.PatientId == patientId);

        if (!relationshipExists)
        {
            return NotFound("Relationship not found.");
        }

        var result = await _service
            .UnlinkPatientAsync(caregiverId, patientId);

        if (!result)
        {
            return NotFound("Relationship not found.");
        }

        return Ok("Patient unlinked successfully.");
    }
}
