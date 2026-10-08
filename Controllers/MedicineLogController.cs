using System.Security.Claims;
using MediCare.Data;
using MediCare.DTOs;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Controllers;

[ApiController]
[Route("api/medicinelog")]
[Authorize(Roles = "ADMIN,PATIENT,CAREGIVER")]
public class MedicineLogController : ControllerBase
{
    private readonly IMedicineLogService _service;
    private readonly AppDbContext _context;

    public MedicineLogController(
        IMedicineLogService service,
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

    private async Task<bool> CanAccessPatientAsync(int patientId)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentRole();

        if (role == "ADMIN")
        {
            return await _context.Patients
                .AnyAsync(p => p.Id == patientId);
        }

        if (role == "PATIENT")
        {
            return await _context.Patients
                .AnyAsync(p =>
                    p.Id == patientId &&
                    p.UserId == userId);
        }

        if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (caregiver == null)
                return false;

            return await _context.CaregiverPatients
                .AnyAsync(cp =>
                    cp.CaregiverId == caregiver.Id &&
                    cp.PatientId == patientId);
        }

        return false;
    }

    // GET: api/medicinelog
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var role = GetCurrentRole();
        var userId = GetCurrentUserId();

        if (role == "ADMIN")
        {
            var allLogs = await _context.MedicineLogs
                .Include(l => l.Medicine)
                .Include(l => l.Patient)
                    .ThenInclude(p => p.User)
                .ToListAsync();

            return Ok(allLogs);
        }

        if (role == "PATIENT")
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null)
                return NotFound("Patient profile not found.");

            var logs = await _context.MedicineLogs
                .Include(l => l.Medicine)
                .Where(l => l.PatientId == patient.Id)
                .ToListAsync();

            return Ok(logs);
        }

        if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (caregiver == null)
                return NotFound("Caregiver profile not found.");

            var logs = await _context.MedicineLogs
                .Include(l => l.Medicine)
                .Where(l =>
                    _context.CaregiverPatients.Any(cp =>
                        cp.CaregiverId == caregiver.Id &&
                        cp.PatientId == l.PatientId))
                .ToListAsync();

            return Ok(logs);
        }

        return Forbid();
    }

    // GET: api/medicinelog/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var log = await _context.MedicineLogs
            .Include(l => l.Medicine)
            .Include(l => l.Patient)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (log == null)
            return NotFound("Medicine log not found.");

        if (!await CanAccessPatientAsync(log.PatientId))
            return Forbid();

        return Ok(log);
    }

    // POST: api/medicinelog
    [HttpPost]
    public async Task<IActionResult> Add(MedicineLogDto dto)
    {
        var medicine = await _context.Medicines
            .FirstOrDefaultAsync(m => m.Id == dto.MedicineId);

        if (medicine == null)
            return NotFound("Medicine not found.");

        // Medicine must belong to the same patient
        if (medicine.PatientId != dto.PatientId)
            return BadRequest(
                "Medicine does not belong to the specified patient."
            );

        if (!await CanAccessPatientAsync(dto.PatientId))
            return Forbid();

        try
        {
            var log = await _service.AddAsync(dto);
            return Ok(log);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // PUT: api/medicinelog/{id}/taken
    [HttpPut("{id}/taken")]
    [Authorize(Roles = "PATIENT")]
    public async Task<IActionResult> MarkAsTaken(int id)
    {
        var userId = GetCurrentUserId();

        var log = await _context.MedicineLogs
            .Include(l => l.Patient)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (log == null)
            return NotFound("Medicine log not found.");

        // Patient can mark only their own medicine as taken
        if (log.Patient.UserId != userId)
            return Forbid();

        var result = await _service.MarkAsTakenAsync(id);

        if (result == null)
            return NotFound("Medicine log not found.");

        return Ok(result);
    }
}