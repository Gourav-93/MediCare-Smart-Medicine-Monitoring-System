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
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
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

        return 0;
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

        if (patientId <= 0 || currentUserId <= 0)
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

        // CAREGIVER can access only approved linked patients.
        if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == currentUserId);

            if (caregiver == null)
            {
                return false;
            }

            return await _context.CaregiverPatients
                .AnyAsync(cp =>
                    cp.CaregiverId == caregiver.Id &&
                    cp.PatientId == patientId &&
                    cp.Status == "APPROVED");
        }

        return false;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var currentUserId = GetCurrentUserId();
        var role = GetCurrentRole();

        if (currentUserId <= 0)
            return Unauthorized();

        if (role == "ADMIN")
        {
            var allMedicines = await _service.GetAllAsync();
            return Ok(allMedicines);
        }

        if (role == "PATIENT")
        {
            var patient = await _context.Patients.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == currentUserId);

            if (patient == null)
            {
                return NotFound("Patient profile not found.");
            }

            var medicines = await _context.Medicines.AsNoTracking()
                .Where(m => m.PatientId == patient.Id && !m.IsDeleted)
                .ToListAsync();

            return Ok(medicines);
        }

        if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == currentUserId);

            if (caregiver == null)
            {
                return NotFound("Caregiver profile not found.");
            }

            var medicines = await _context.Medicines.AsNoTracking()
                .Where(m => !m.IsDeleted && _context.CaregiverPatients
                    .Any(cp =>
                        cp.CaregiverId == caregiver.Id &&
                        cp.PatientId == m.PatientId &&
                        cp.Status == "APPROVED"))
                .ToListAsync();

            return Ok(medicines);
        }

        return Forbid();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (GetCurrentUserId() <= 0)
            return Unauthorized();

        if (id <= 0)
            return BadRequest("A valid medicine ID is required.");
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
        if (dto == null)
            return BadRequest("Medicine details are required.");
        var role = GetCurrentRole();
        var currentUserId = GetCurrentUserId();

        if (currentUserId <= 0)
            return Unauthorized();

        if (role == "PATIENT")
        {
            var patient = await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == currentUserId);
            if (patient == null)
            {
                return NotFound("Patient profile not found.");
            }
            dto.PatientId = patient.Id;
        }

        if (dto.PatientId <= 0)
        {
            return BadRequest("Valid PatientId is required.");
        }

        if (!await CanAccessPatientAsync(dto.PatientId))
        {
            return Forbid();
        }

        // Require explicit prescription dates rather than inventing a duration.
        if (dto.StartDate == default || dto.EndDate == default ||
            dto.EndDate.Date < dto.StartDate.Date)
            return BadRequest("Valid start and end dates are required; end date cannot precede start date.");

        if (dto.Dose <= 0 || dto.Stock < 0)
            return BadRequest("Dose must be positive and stock cannot be negative.");

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
        if (GetCurrentUserId() <= 0)
            return Unauthorized();

        if (id <= 0)
            return BadRequest("A valid medicine ID is required.");

        if (dto == null)
            return BadRequest("Medicine details are required.");

        var medicine = await _service.GetByIdAsync(id);
        if (medicine == null)
            return NotFound("Medicine not found.");

        if (!await CanAccessPatientAsync(medicine.PatientId))
            return Forbid();

        if (dto.PatientId == 0)
            dto.PatientId = medicine.PatientId;

        if (dto.PatientId != medicine.PatientId)
            return BadRequest("An existing medicine cannot be transferred to another patient.");

        // The client must send the version originally loaded with the edit form.
        if (!dto.Version.HasValue || dto.Version.Value < 0 ||
            dto.Version.Value == long.MaxValue)
        {
            return BadRequest(new
            {
                message = "A valid medicine version is required. Refresh the medicine details."
            });
        }

        if (dto.Version.Value != medicine.Version)
        {
            return Conflict(new
            {
                message = "Medicine details or stock changed. Refresh and try again."
            });
        }

        // Preserve the client version; service and repository recheck it.
        // Preserve existing dates when omitted on update.
        if (dto.StartDate == default)
            dto.StartDate = medicine.StartDate;
        if (dto.EndDate == default)
            dto.EndDate = medicine.EndDate;

        if (dto.StartDate == default || dto.EndDate == default ||
            dto.EndDate.Date < dto.StartDate.Date)
            return BadRequest("Invalid medicine date range.");

        if (dto.Dose <= 0 || dto.Stock < 0)
            return BadRequest("Dose must be positive and stock cannot be negative.");

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
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Medicine details or stock changed. Refresh and try again." });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Medicine not found or has been deleted.")
        {
            return NotFound("Medicine not found.");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (GetCurrentUserId() <= 0)
            return Unauthorized();

        if (id <= 0)
            return BadRequest("A valid medicine ID is required.");
        var medicine = await _service.GetByIdAsync(id);

        if (medicine == null)
        {
            return NotFound("Medicine not found.");
        }

        if (!await CanAccessPatientAsync(medicine.PatientId))
        {
            return Forbid();
        }

        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound("Medicine not found.");

            return Ok("Medicine deleted successfully.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Medicine changed or was deleted. Refresh and try again." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // GET: api/medicine/low-stock
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockMedicines([FromQuery] int threshold = 5)
    {
        var currentUserId = GetCurrentUserId();
        var role = GetCurrentRole();

        if (currentUserId <= 0)
            return Unauthorized();

        if (threshold < 0)
            return BadRequest("Stock threshold cannot be negative.");

        if (role == "ADMIN")
        {
            var adminLowStock = await _context.Medicines.AsNoTracking()
                .Include(m => m.Patient)
                    .ThenInclude(p => p.User)
                .Where(m => !m.IsDeleted && m.Stock <= threshold)
                .OrderBy(m => m.Stock)
                .ToListAsync();
            return Ok(adminLowStock);
        }

        if (role == "PATIENT")
        {
            var patient = await _context.Patients.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == currentUserId);

            if (patient == null)
                return NotFound("Patient profile not found.");

            var patientLowStock = await _context.Medicines.AsNoTracking()
                .Where(m => !m.IsDeleted && m.PatientId == patient.Id && m.Stock <= threshold)
                .OrderBy(m => m.Stock)
                .ToListAsync();

            return Ok(patientLowStock);
        }

        if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == currentUserId);

            if (caregiver == null)
                return NotFound("Caregiver profile not found.");

            var caregiverLowStock = await _context.Medicines.AsNoTracking()
                .Include(m => m.Patient)
                    .ThenInclude(p => p.User)
                .Where(m => !m.IsDeleted && _context.CaregiverPatients
                    .Any(cp =>
                        cp.CaregiverId == caregiver.Id &&
                        cp.PatientId == m.PatientId &&
                        cp.Status == "APPROVED") && m.Stock <= threshold)
                .OrderBy(m => m.Stock)
                .ToListAsync();

            return Ok(caregiverLowStock);
        }

        return Forbid();
    }
}