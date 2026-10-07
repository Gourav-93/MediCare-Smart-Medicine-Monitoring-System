using MediCare.DTOs;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers;

[ApiController]
[Route("api/Caregiverpatient")]
public class CaregiverPatientController : ControllerBase
{
    private readonly ICaregiverPatientService _service;

    public CaregiverPatientController(
        ICaregiverPatientService service)
    {
        _service = service;
    }

    [HttpPost("link")]
    public async Task<IActionResult> LinkPatient(
        CaregiverPatientDto dto)
    {
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
    public async Task<IActionResult> GetPatients(
        int caregiverId)
    {
        var patients = await _service
            .GetPatientsAsync(caregiverId);

        return Ok(patients);
    }

    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetCaregivers(
        int patientId)
    {
        var caregivers = await _service
            .GetCaregiversAsync(patientId);

        return Ok(caregivers);
    }

    [HttpDelete("unlink")]
    public async Task<IActionResult> UnlinkPatient(
        int caregiverId,
        int patientId)
    {
        var result = await _service
            .UnlinkPatientAsync(caregiverId, patientId);

        if (!result)
        {
            return NotFound("Relationship not found.");
        }

        return Ok("Patient unlinked successfully.");
    }
}