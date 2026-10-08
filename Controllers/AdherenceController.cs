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
        return int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );
    }

    private string GetCurrentRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? "";
    }

    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetPatientAdherence(int patientId)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentRole();

        // ADMIN can access any existing patient
        if (role == "ADMIN")
        {
            var patientExists = await _context.Patients
                .AnyAsync(p => p.Id == patientId);

            if (!patientExists)
                return NotFound("Patient not found.");

            var adminResult =
                await _service.GetPatientAdherenceAsync(patientId);

            return Ok(adminResult);
        }

        // PATIENT can access only their own adherence
        if (role == "PATIENT")
        {
            var isOwnPatient = await _context.Patients
                .AnyAsync(p =>
                    p.Id == patientId &&
                    p.UserId == userId);

            if (!isOwnPatient)
                return Forbid();

            var patientResult =
                await _service.GetPatientAdherenceAsync(patientId);

            return Ok(patientResult);
        }

        // CAREGIVER can access only linked patient's adherence
        if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (caregiver == null)
                return NotFound("Caregiver profile not found.");

            var isLinked = await _context.CaregiverPatients
                .AnyAsync(cp =>
                    cp.CaregiverId == caregiver.Id &&
                    cp.PatientId == patientId);

            if (!isLinked)
                return Forbid();

            var caregiverResult =
                await _service.GetPatientAdherenceAsync(patientId);

            return Ok(caregiverResult);
        }

        return Forbid();
    }
}