using System.Security.Claims;
using MediCare.Data;
using MediCare.DTOs;
using MediCare.Models;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Controllers;

[ApiController]
[Route("api/medicineschedule")]
[Authorize(Roles = "ADMIN,PATIENT,CAREGIVER")]
public class MedicineScheduleController : ControllerBase
{
    private readonly IMedicineScheduleService _service;
    private readonly IMedicineLogService _medicineLogService;
    private readonly AppDbContext _context;

    public MedicineScheduleController(
        IMedicineScheduleService service,
        IMedicineLogService medicineLogService,
        AppDbContext context)
    {
        _service = service;
        _medicineLogService = medicineLogService;
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

    // GET: api/medicineschedule/today
    [HttpGet("today")]
    public async Task<IActionResult> GetTodaySchedules([FromQuery] int? patientId = null)
    {
        var role = GetCurrentRole();
        var userId = GetCurrentUserId();

        int targetPatientId = 0;

        if (role == "PATIENT")
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
            if (patient == null) return NotFound("Patient profile not found.");
            targetPatientId = patient.Id;
        }
        else if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (caregiver == null) return NotFound("Caregiver profile not found.");

            if (patientId.HasValue && patientId.Value > 0)
            {
                var isLinked = await _context.CaregiverPatients
                    .AnyAsync(cp => cp.CaregiverId == caregiver.Id && cp.PatientId == patientId.Value);
                if (!isLinked) return Forbid();
                targetPatientId = patientId.Value;
            }
            else
            {
                var firstLinked = await _context.CaregiverPatients
                    .Where(cp => cp.CaregiverId == caregiver.Id)
                    .Select(cp => cp.PatientId)
                    .FirstOrDefaultAsync();
                targetPatientId = firstLinked;
            }
        }
        else if (role == "ADMIN")
        {
            if (patientId.HasValue && patientId.Value > 0)
            {
                targetPatientId = patientId.Value;
            }
            else
            {
                var first = await _context.Patients.Select(p => p.Id).FirstOrDefaultAsync();
                targetPatientId = first;
            }
        }

        if (targetPatientId <= 0)
        {
            return Ok(new List<object>());
        }

        // Auto mark overdue logs as missed
        var now = DateTime.Now;
        var today = now.Date;

        var overdueLogs = await _context.MedicineLogs
            .Where(l => l.PatientId == targetPatientId &&
                        (l.Status == "Pending" || l.Status == "Snoozed") &&
                        l.ScheduledTime.AddMinutes(15) <= now)
            .ToListAsync();

        if (overdueLogs.Any())
        {
            foreach (var ol in overdueLogs)
            {
                ol.Status = "Missed";
            }
            await _context.SaveChangesAsync();
        }

        // Fetch patient medicines and schedules (excluding soft-deleted)
        var schedules = await _context.MedicineSchedules
            .Include(s => s.Medicine)
            .Where(s => !s.Medicine.IsDeleted &&
                        s.Medicine.PatientId == targetPatientId &&
                        s.Medicine.StartDate.Date <= today &&
                        s.Medicine.EndDate.Date >= today)
            .OrderBy(s => s.Time)
            .ToListAsync();

        // Fetch today's logs for this patient
        var todayLogs = await _context.MedicineLogs
            .Where(l => l.PatientId == targetPatientId &&
                        (l.ScheduledTime.Date == today || (l.OriginalScheduledTime.HasValue && l.OriginalScheduledTime.Value.Date == today)))
            .ToListAsync();

        var result = new List<object>();

        foreach (var s in schedules)
        {
            var scheduledDateTime = today.Add(s.Time);

            // Match log for this schedule occurrence
            var log = todayLogs.FirstOrDefault(l =>
                l.MedicineId == s.MedicineId &&
                ((l.OriginalScheduledTime.HasValue && l.OriginalScheduledTime.Value == scheduledDateTime) ||
                 l.ScheduledTime == scheduledDateTime ||
                 (l.Status == "Snoozed" && l.OriginalScheduledTime.HasValue && l.OriginalScheduledTime.Value == scheduledDateTime) ||
                 (Math.Abs((l.ScheduledTime - scheduledDateTime).TotalMinutes) < 1)));

            string computedStatus = "Upcoming";
            DateTime displayScheduledTime = scheduledDateTime;
            int snoozeCount = 0;
            DateTime? takenTime = null;
            int? logId = null;

            if (log != null)
            {
                logId = log.Id;
                snoozeCount = log.SnoozeCount;
                takenTime = log.TakenTime;
                displayScheduledTime = log.ScheduledTime;

                if (log.Status == "Taken")
                {
                    computedStatus = "Taken";
                }
                else if (log.Status == "Missed")
                {
                    computedStatus = "Missed";
                }
                else if (log.Status == "Snoozed")
                {
                    computedStatus = "Snoozed";
                }
                else
                {
                    if (now > log.ScheduledTime.AddMinutes(15))
                        computedStatus = "Missed";
                    else
                        computedStatus = "Upcoming";
                }
            }
            else
            {
                if (now > scheduledDateTime.AddMinutes(15))
                {
                    computedStatus = "Missed";
                }
                else
                {
                    computedStatus = "Upcoming";
                }
            }

            bool canTake = computedStatus != "Taken" && computedStatus != "Missed";
            bool canSnooze = computedStatus != "Taken" && computedStatus != "Missed" && snoozeCount < 2 && (now <= (log != null ? log.ScheduledTime.AddMinutes(15) : scheduledDateTime.AddMinutes(15)));

            result.Add(new
            {
                scheduleId = s.Id,
                medicineId = s.MedicineId,
                medicineName = s.Medicine.Name,
                dosage = s.Medicine.Dosage,
                dose = s.Medicine.Dose,
                stock = s.Medicine.Stock,
                startDate = s.Medicine.StartDate,
                endDate = s.Medicine.EndDate,
                mealTiming = s.Medicine.MealTimeing,
                frequency = s.Frequency,
                time = s.Time,
                scheduledTime = displayScheduledTime,
                originalScheduledTime = scheduledDateTime,
                status = computedStatus,
                logId = logId,
                takenTime = takenTime,
                snoozeCount = snoozeCount,
                patientId = targetPatientId,
                canTake = canTake,
                canSnooze = canSnooze
            });
        }

        return Ok(result);
    }

    // POST: api/medicineschedule/{id}/take
    [HttpPost("{id}/take")]
    public async Task<IActionResult> TakeDose(int id)
    {
        var schedule = await _context.MedicineSchedules
            .Include(s => s.Medicine)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
            return NotFound("Schedule not found.");

        if (schedule.Medicine.IsDeleted)
            return BadRequest("Cannot record dose for a deleted medicine.");

        if (!await CanAccessPatientAsync(schedule.Medicine.PatientId))
            return Forbid();

        var today = DateTime.Today;
        var scheduledTime = today.Add(schedule.Time);

        // Find existing log for today
        var candidateLogs = await _context.MedicineLogs
            .Where(l => l.MedicineId == schedule.MedicineId &&
                        l.PatientId == schedule.Medicine.PatientId &&
                        (l.ScheduledTime.Date == today || (l.OriginalScheduledTime.HasValue && l.OriginalScheduledTime.Value.Date == today)))
            .ToListAsync();

        var log = candidateLogs.FirstOrDefault(l =>
            (l.OriginalScheduledTime.HasValue && l.OriginalScheduledTime.Value == scheduledTime) ||
            l.ScheduledTime == scheduledTime ||
            Math.Abs((l.ScheduledTime - scheduledTime).TotalMinutes) < 2);

        if (log == null)
        {
            log = new MedicineLog
            {
                MedicineId = schedule.MedicineId,
                PatientId = schedule.Medicine.PatientId,
                ScheduledTime = scheduledTime,
                OriginalScheduledTime = scheduledTime,
                Status = "Pending",
                MedicineName = schedule.Medicine.Name,
                Dosage = schedule.Medicine.Dosage,
                MealTiming = schedule.Medicine.MealTimeing
            };
            _context.MedicineLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        if (log.Status == "Taken")
        {
            return Ok(new { message = "Dose has already been marked as Taken.", log, stock = schedule.Medicine.Stock });
        }

        var updated = await _medicineLogService.MarkAsTakenAsync(log.Id);

        // Refresh stock
        var currentMed = await _context.Medicines.FindAsync(schedule.MedicineId);

        return Ok(new
        {
            message = $"Dose of {schedule.Medicine.Name} marked as Taken.",
            log = updated,
            stock = currentMed?.Stock ?? 0
        });
    }

    // POST: api/medicineschedule/{id}/snooze
    [HttpPost("{id}/snooze")]
    public async Task<IActionResult> SnoozeDose(int id, [FromQuery] int minutes = 15)
    {
        var schedule = await _context.MedicineSchedules
            .Include(s => s.Medicine)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
            return NotFound("Schedule not found.");

        if (schedule.Medicine.IsDeleted)
            return BadRequest("Cannot snooze dose for a deleted medicine.");

        if (!await CanAccessPatientAsync(schedule.Medicine.PatientId))
            return Forbid();

        var today = DateTime.Today;
        var scheduledTime = today.Add(schedule.Time);

        // Find existing log for today
        var candidateLogs = await _context.MedicineLogs
            .Where(l => l.MedicineId == schedule.MedicineId &&
                        l.PatientId == schedule.Medicine.PatientId &&
                        (l.ScheduledTime.Date == today || (l.OriginalScheduledTime.HasValue && l.OriginalScheduledTime.Value.Date == today)))
            .ToListAsync();

        var log = candidateLogs.FirstOrDefault(l =>
            (l.OriginalScheduledTime.HasValue && l.OriginalScheduledTime.Value == scheduledTime) ||
            l.ScheduledTime == scheduledTime ||
            Math.Abs((l.ScheduledTime - scheduledTime).TotalMinutes) < 2);

        if (log == null)
        {
            log = new MedicineLog
            {
                MedicineId = schedule.MedicineId,
                PatientId = schedule.Medicine.PatientId,
                ScheduledTime = scheduledTime,
                OriginalScheduledTime = scheduledTime,
                Status = "Pending",
                MedicineName = schedule.Medicine.Name,
                Dosage = schedule.Medicine.Dosage,
                MealTiming = schedule.Medicine.MealTimeing
            };
            _context.MedicineLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        try
        {
            var updated = await _medicineLogService.SnoozeAsync(log.Id, minutes);
            return Ok(new
            {
                message = $"Dose of {schedule.Medicine.Name} snoozed by {minutes} minutes.",
                log = updated,
                newScheduledTime = updated?.ScheduledTime,
                snoozeCount = updated?.SnoozeCount
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
                .Where(s => !s.Medicine.IsDeleted)
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
                .Where(s => !s.Medicine.IsDeleted && s.Medicine.PatientId == patient.Id)
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
                .Where(s => !s.Medicine.IsDeleted &&
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

        if (schedule == null || schedule.Medicine.IsDeleted)
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

        if (medicine == null || medicine.IsDeleted)
            return BadRequest("Cannot create a schedule for a deleted or non-existent medicine.");

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

        if (existingSchedule == null || existingSchedule.Medicine.IsDeleted)
            return NotFound("Schedule not found.");

        // Check access to existing medicine
        if (!await CanAccessPatientAsync(existingSchedule.Medicine.PatientId))
            return Forbid();

        // Check new medicine
        var newMedicine = await _context.Medicines
            .FirstOrDefaultAsync(m => m.Id == dto.MedicineId);

        if (newMedicine == null || newMedicine.IsDeleted)
            return BadRequest("Cannot link schedule to a deleted or non-existent medicine.");

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