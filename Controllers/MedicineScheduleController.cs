using System.Security.Claims;
using MediCare.Data;
using MediCare.DTOs;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Controllers;

[ApiController]
[Route("api/Medicineschedule")]
[Authorize(Roles = "ADMIN,PATIENT,CAREGIVER")]
public class MedicineScheduleController : ControllerBase
{
    private readonly IMedicineScheduleService _service;
    private readonly AppDbContext _context;

    public MedicineScheduleController(
        IMedicineScheduleService service,
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

        // ADMIN can access all patients
        if (role == "ADMIN")
        {
            return await _context.Patients
                .AnyAsync(p => p.Id == patientId);
        }

        // PATIENT can access only own data
        if (role == "PATIENT")
        {
            return await _context.Patients
                .AnyAsync(p =>
                    p.Id == patientId &&
                    p.UserId == userId);
        }

        // CAREGIVER can access only linked patients
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

    // GET: api/Medicineschedule
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var role = GetCurrentRole();
        var userId = GetCurrentUserId();

        if (role == "ADMIN")
        {
            var allSchedules = await _context.MedicineSchedules
                .Include(s => s.Medicine)
                .ToListAsync();

            return Ok(allSchedules);
        }

        if (role == "PATIENT")
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null)
                return NotFound("Patient profile not found.");

            var schedules = await _context.MedicineSchedules
                .Include(s => s.Medicine)
                .Where(s => s.Medicine.PatientId == patient.Id)
                .ToListAsync();

            return Ok(schedules);
        }

        if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (caregiver == null)
                return NotFound("Caregiver profile not found.");

            var schedules = await _context.MedicineSchedules
                .Include(s => s.Medicine)
                .Where(s =>
                    _context.CaregiverPatients.Any(cp =>
                        cp.CaregiverId == caregiver.Id &&
                        cp.PatientId == s.Medicine.PatientId))
                .ToListAsync();

            return Ok(schedules);
        }

        return Forbid();
    }

    // GET: api/Medicineschedule/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var schedule = await _context.MedicineSchedules
            .Include(s => s.Medicine)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
            return NotFound("Schedule not found.");

        if (!await CanAccessPatientAsync(schedule.Medicine.PatientId))
            return Forbid();

        return Ok(schedule);
    }

    // POST: api/Medicineschedule
    [HttpPost]
    public async Task<IActionResult> Add(MedicineScheduleDto dto)
    {
        var medicine = await _context.Medicines
            .FirstOrDefaultAsync(m => m.Id == dto.MedicineId);

        if (medicine == null)
            return NotFound("Medicine not found.");

        if (!await CanAccessPatientAsync(medicine.PatientId))
            return Forbid();

        try
        {
            var schedule = await _service.AddAsync(dto);
            return Ok(schedule);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // PUT: api/Medicineschedule/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        MedicineScheduleDto dto)
    {
        var existingSchedule = await _context.MedicineSchedules
            .Include(s => s.Medicine)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (existingSchedule == null)
            return NotFound("Schedule not found.");

        // Check access to existing medicine
        if (!await CanAccessPatientAsync(existingSchedule.Medicine.PatientId))
            return Forbid();

        // Check new medicine
        var newMedicine = await _context.Medicines
            .FirstOrDefaultAsync(m => m.Id == dto.MedicineId);

        if (newMedicine == null)
            return NotFound("Medicine not found.");

        // Prevent moving schedule to another unauthorized patient
        if (!await CanAccessPatientAsync(newMedicine.PatientId))
            return Forbid();

        try
        {
            var schedule = await _service.UpdateAsync(id, dto);

            if (schedule == null)
                return NotFound("Schedule not found.");

            return Ok(schedule);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // DELETE: api/Medicineschedule/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var schedule = await _context.MedicineSchedules
            .Include(s => s.Medicine)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
            return NotFound("Schedule not found.");

        if (!await CanAccessPatientAsync(schedule.Medicine.PatientId))
            return Forbid();

        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
            return NotFound("Schedule not found.");

        return Ok("Schedule deleted successfully.");
    }
}