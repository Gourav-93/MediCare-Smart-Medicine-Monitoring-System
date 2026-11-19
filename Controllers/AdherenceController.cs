
using System.Security.Claims;
using MediCare.Data;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Controllers;

[ApiController]
[Route("api/adherence")]
[Authorize(Roles = "ADMIN,PATIENT,CAREGIVER")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AdherenceController : ControllerBase
{
    private readonly IAdherenceService _service;
    private readonly AppDbContext _context;

    public AdherenceController(
        IAdherenceService service,
        AppDbContext context)
    {
        _service = service;
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(claim, out var id) && id > 0
            ? id
            : 0;
    }

    private string GetCurrentRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? "";
    }

    // Common authorization for both adherence endpoints
    private async Task<IActionResult?> ValidatePatientAccessAsync(
        int patientId)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentRole();

        if (userId <= 0)
            return Unauthorized();

        if (patientId <= 0)
            return BadRequest("Valid PatientId is required.");

        // ADMIN can access any existing patient
        if (role == "ADMIN")
        {
            var patientExists = await _context.Patients
                .AnyAsync(p => p.Id == patientId);

            if (!patientExists)
                return NotFound("Patient not found.");

            return null;
        }

        // PATIENT can access only their own records
        if (role == "PATIENT")
        {
            var isOwnPatient = await _context.Patients
                .AnyAsync(p =>
                    p.Id == patientId &&
                    p.UserId == userId);

            if (!isOwnPatient)
                return Forbid();

            return null;
        }

        // CAREGIVER requires patient approval
        if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId);

            if (caregiver == null)
            {
                return NotFound(
                    "Caregiver profile not found.");
            }

            var isLinked = await _context.CaregiverPatients
                .AnyAsync(cp =>
                    cp.CaregiverId == caregiver.Id &&
                    cp.PatientId == patientId &&
                    cp.Status == "APPROVED");

            if (!isLinked)
                return Forbid();

            return null;
        }

        return Forbid();
    }

    // GET: api/adherence/patient/{patientId}
    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetPatientAdherence(
        int patientId)
    {
        var accessError =
            await ValidatePatientAccessAsync(patientId);

        if (accessError != null)
            return accessError;

        var result = await _service
            .GetPatientAdherenceAsync(patientId);

        return Ok(result);
    }

    // GET: api/adherence/patient/{patientId}/report?period=Daily
    [HttpGet("patient/{patientId}/report")]
    public async Task<IActionResult> GetPeriodicAdherence(
        int patientId,
        [FromQuery] string period = "Daily")
    {
        var accessError =
            await ValidatePatientAccessAsync(patientId);

        if (accessError != null)
            return accessError;

        if (string.IsNullOrWhiteSpace(period))
        {
            return BadRequest(new
            {
                message = "Report period is required."
            });
        }

        var normalizedPeriod = period.Trim();

        // Normalize the confirmed default period.
        // Other supported periods will be validated
        // after reviewing AdherenceService.cs.
        if (string.Equals(
                normalizedPeriod,
                "Daily",
                StringComparison.OrdinalIgnoreCase))
        {
            normalizedPeriod = "Daily";
        }

        try
        {
            var result = await _service
                .GetPeriodicAdherenceAsync(
                    patientId,
                    normalizedPeriod);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}
