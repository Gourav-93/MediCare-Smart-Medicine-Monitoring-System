using System.Security.Claims;
using MediCare.Data;
using MediCare.DTOs;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Controllers;

[ApiController]
[Route("api/medicine")]
[Authorize(Roles = "ADMIN,PATIENT,CAREGIVER")]
public class MedicineController : ControllerBase
{
    private readonly IMedicineService _service;
    private readonly AppDbContext _context;

    public MedicineController(
        IMedicineService service,
        AppDbContext context)
    {
        _service = service;
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(userId, out var id) ? id : 0;
    }

    private string GetCurrentRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }

    // Check whether the current user is allowed to access a patient.
    private async Task<bool> CanAccessPatientAsync(int patientId)
    {
        var currentUserId = GetCurrentUserId();
        var role = GetCurrentRole();

        if (currentUserId <= 0)
        {
            return false;
        }

        // ADMIN can access every patient.
        if (role == "ADMIN")
        {
            return await _context.Patients
                .AnyAsync(p => p.Id == patientId);
        }

        // PATIENT can access only his own patient record.
        if (role == "PATIENT")
        {
            return await _context.Patients
                .AnyAsync(p =>
                    p.Id == patientId &&
                    p.UserId == currentUserId);
        }

        // CAREGIVER can access only linked patients.
        if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers
                .FirstOrDefaultAsync(c => c.UserId == currentUserId);

            if (caregiver == null)
            {
                return false;
            }

            return await _context.CaregiverPatients
                .AnyAsync(cp =>
                    cp.CaregiverId == caregiver.Id &&
                    cp.PatientId == patientId);
        }

        return false;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var currentUserId = GetCurrentUserId();
        var role = GetCurrentRole();

        if (currentUserId <= 0)
        {
            return Unauthorized();
        }

        if (role == "ADMIN")
        {
            var allMedicines = await _service.GetAllAsync();
            return Ok(allMedicines);
        }

        if (role == "PATIENT")
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == currentUserId);

            if (patient == null)
            {
                return NotFound("Patient profile not found.");
            }

            var medicines = await _context.Medicines
                .Where(m => m.PatientId == patient.Id)
                .ToListAsync();

            return Ok(medicines);
        }

        if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers
                .FirstOrDefaultAsync(c => c.UserId == currentUserId);

            if (caregiver == null)
            {
                return NotFound("Caregiver profile not found.");
            }

            var medicines = await _context.Medicines
                .Where(m => _context.CaregiverPatients
                    .Any(cp =>
                        cp.CaregiverId == caregiver.Id &&
                        cp.PatientId == m.PatientId))
                .ToListAsync();

            return Ok(medicines);
        }

        return Forbid();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var medicine = await _service.GetByIdAsync(id);

        if (medicine == null)
        {
            return NotFound("Medicine not found.");
        }

        if (!await CanAccessPatientAsync(medicine.PatientId))
        {
            return Forbid();
        }

        return Ok(medicine);
    }

    [HttpPost]
    public async Task<IActionResult> Add(MedicineDto dto)
    {
        if (dto.PatientId <= 0)
        {
            return BadRequest("Valid patientId is required.");
        }

        if (!await CanAccessPatientAsync(dto.PatientId))
        {
            var patientExists = await _context.Patients
                .AnyAsync(p => p.Id == dto.PatientId);

            if (!patientExists)
            {
                return NotFound("Patient not found.");
            }

            return Forbid();
        }

        try
        {
            var medicine = await _service.AddAsync(dto);

            return Ok(medicine);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        MedicineDto dto)
    {
        if (dto.PatientId <= 0)
        {
            return BadRequest("Valid patientId is required.");
        }

        var medicine = await _service.GetByIdAsync(id);

        if (medicine == null)
        {
            return NotFound("Medicine not found.");
        }

        if (!await CanAccessPatientAsync(medicine.PatientId))
        {
            return Forbid();
        }
        if (!await CanAccessPatientAsync(dto.PatientId))
        {
            var patientExists = await _context.Patients
                .AnyAsync(p => p.Id == dto.PatientId);

            if (!patientExists)
            {
                return NotFound("Patient not found.");
            }

            return Forbid();
        }

        try
        {
            var updatedMedicine =
                await _service.UpdateAsync(id, dto);

            if (updatedMedicine == null)
            {
                return NotFound("Medicine not found.");
            }

            return Ok(updatedMedicine);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var medicine = await _service.GetByIdAsync(id);

        if (medicine == null)
        {
            return NotFound("Medicine not found.");
        }

        if (!await CanAccessPatientAsync(medicine.PatientId))
        {
            return Forbid();
        }

        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound("Medicine not found.");
        }

        return Ok("Medicine deleted successfully.");
    }
}