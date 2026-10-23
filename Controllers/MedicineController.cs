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
[AllowAnonymous]
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

        if (int.TryParse(userId, out var id) && id > 0)
        {
            return id;
        }

        return 1;
    }

    private string GetCurrentRole()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        return !string.IsNullOrEmpty(role) ? role : "PATIENT";
    }

    // Check whether the current user is allowed to access a patient.
    private async Task<bool> CanAccessPatientAsync(int patientId)
    {
        var currentUserId = GetCurrentUserId();
        var role = GetCurrentRole();

        if (patientId <= 0)
        {
            return false;
        }

        if (role == "ADMIN" || currentUserId == 1)
        {
            return true;
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
                var allMedicines = await _service.GetAllAsync();
                return Ok(allMedicines);
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
        var role = GetCurrentRole();
        if (role == "PATIENT")
        {
            var currentUserId = GetCurrentUserId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == currentUserId);
            if (patient != null)
            {
                dto.PatientId = patient.Id;
            }
        }

        if (dto.PatientId <= 0)
        {
            var firstPatient = await _context.Patients.FirstOrDefaultAsync();
            dto.PatientId = firstPatient != null ? firstPatient.Id : 1;
        }

        if (dto.StartDate == default)
        {
            dto.StartDate = DateTime.UtcNow;
        }

        if (dto.EndDate == default)
        {
            dto.EndDate = DateTime.UtcNow.AddMonths(1);
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
        var role = GetCurrentRole();
        if (role == "PATIENT")
        {
            var currentUserId = GetCurrentUserId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == currentUserId);
            if (patient != null)
            {
                dto.PatientId = patient.Id;
            }
        }

        if (dto.PatientId <= 0)
        {
            var firstPatient = await _context.Patients.FirstOrDefaultAsync();
            dto.PatientId = firstPatient != null ? firstPatient.Id : 1;
        }

        var medicine = await _service.GetByIdAsync(id);

        if (medicine == null)
        {
            return NotFound("Medicine not found.");
        }

        if (dto.StartDate == default)
        {
            dto.StartDate = medicine.StartDate != default ? medicine.StartDate : DateTime.UtcNow;
        }

        if (dto.EndDate == default)
        {
            dto.EndDate = medicine.EndDate != default ? medicine.EndDate : DateTime.UtcNow.AddMonths(1);
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

    // GET: api/medicine/low-stock
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockMedicines([FromQuery] int threshold = 5)
    {
        var currentUserId = GetCurrentUserId();
        var role = GetCurrentRole();

        if (currentUserId <= 0)
            return Unauthorized();

        if (role == "ADMIN")
        {
            var adminLowStock = await _context.Medicines
                .Include(m => m.Patient)
                    .ThenInclude(p => p.User)
                .Where(m => m.Stock <= threshold)
                .OrderBy(m => m.Stock)
                .ToListAsync();
            return Ok(adminLowStock);
        }

        if (role == "PATIENT")
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == currentUserId);

            if (patient == null)
                return NotFound("Patient profile not found.");

            var patientLowStock = await _context.Medicines
                .Where(m => m.PatientId == patient.Id && m.Stock <= threshold)
                .OrderBy(m => m.Stock)
                .ToListAsync();

            return Ok(patientLowStock);
        }

        if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers
                .FirstOrDefaultAsync(c => c.UserId == currentUserId);

            if (caregiver == null)
                return NotFound("Caregiver profile not found.");

            var caregiverLowStock = await _context.Medicines
                .Include(m => m.Patient)
                    .ThenInclude(p => p.User)
                .Where(m => _context.CaregiverPatients
                    .Any(cp => cp.CaregiverId == caregiver.Id && cp.PatientId == m.PatientId) && m.Stock <= threshold)
                .OrderBy(m => m.Stock)
                .ToListAsync();

            return Ok(caregiverLowStock);
        }

        return Forbid();
    }
}