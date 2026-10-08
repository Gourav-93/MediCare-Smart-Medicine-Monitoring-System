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

    // =========================================================
    // LINK PATIENT
    // ADMIN -> can link any caregiver with any patient
    // CAREGIVER -> can link only himself with a patient
    // PATIENT -> not allowed
    // =========================================================

    [HttpPost("link")]
    [Authorize(Roles = "ADMIN,CAREGIVER")]
    public async Task<IActionResult> LinkPatient(
        CaregiverPatientDto dto)
    {
        if (dto.CaregiverId <= 0 || dto.PatientId <= 0)
        {
            return BadRequest("Valid caregiverId and patientId are required.");
        }

        var role = GetCurrentRole();
        var currentUserId = GetCurrentUserId();

        var caregiver = await _context.Caregivers
            .FirstOrDefaultAsync(c => c.Id == dto.CaregiverId);

        if (caregiver == null)
        {
            return NotFound("Caregiver not found.");
        }

        // Caregiver can only link himself.
        if (role == "CAREGIVER" &&
            caregiver.UserId != currentUserId)
        {
            return Forbid();
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

    // =========================================================
    // GET PATIENTS OF CAREGIVER
    // ADMIN -> any caregiver
    // CAREGIVER -> only himself
    // PATIENT -> not allowed
    // =========================================================

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
    // UNLINK PATIENT
    // ADMIN -> any relationship
    // CAREGIVER -> only his own relationship
    // PATIENT -> not allowed
    // =========================================================

    [HttpDelete("unlink")]
    [Authorize(Roles = "ADMIN,CAREGIVER")]
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

        var caregiver = await _context.Caregivers
            .FirstOrDefaultAsync(c => c.Id == caregiverId);

        if (caregiver == null)
        {
            return NotFound("Caregiver not found.");
        }

        // Caregiver can only unlink his own relationship.
        if (role == "CAREGIVER" &&
            caregiver.UserId != currentUserId)
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
