using MediCare.Data;
using MediCare.DTOs;
using MediCare.Models;
using MediCare.Repositories.Interfaces;
using MediCare.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Services;

public class MedicineLogService : IMedicineLogService
{
    private readonly IMedicineLogRepository _repository;
    private readonly INotificationRepository _notificationRepository;
    private readonly AppDbContext _context;

    public MedicineLogService(
        IMedicineLogRepository repository,
        INotificationRepository notificationRepository,
        AppDbContext context)
    {
        _repository = repository;
        _notificationRepository = notificationRepository;
        _context = context;
    }

    public async Task<List<MedicineLog>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<MedicineLog?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<MedicineLog> AddAsync(MedicineLogDto dto)
    {
        if (!await _repository.MedicineExistsAsync(dto.MedicineId))
        {
            throw new ArgumentException("Medicine not found.");
        }

        if (!await _repository.PatientExistsAsync(dto.PatientId))
        {
            throw new ArgumentException("Patient not found.");
        }

        var log = new MedicineLog
        {
            MedicineId = dto.MedicineId,
            PatientId = dto.PatientId,
            ScheduledTime = dto.ScheduledTime,
            Status = dto.Status
        };

        return await _repository.AddAsync(log);
    }

    public async Task<MedicineLog> AddAsync(MedicineLog log)
    {
        return await _repository.AddAsync(log);
    }

    // Feature 3: Low Stock Alert (Patient + Linked Caregivers) & Stock Deduction
    public async Task<MedicineLog?> MarkAsTakenAsync(int id)
    {
        var log = await _context.MedicineLogs
            .Include(l => l.Medicine)
                .ThenInclude(m => m.Patient)
                    .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (log == null)
            return null;

        if (log.Status == "Taken")
            return log;

        log.Status = "Taken";
        log.TakenTime = DateTime.Now;

        var previousStock = log.Medicine.Stock;

        if (log.Medicine.Stock >= log.Medicine.Dose)
        {
            log.Medicine.Stock -= log.Medicine.Dose;
        }
        else
        {
            log.Medicine.Stock = 0;
        }

        // Low stock threshold check (Trigger only once when stock falls to or below 5)
        if (previousStock > 5 && log.Medicine.Stock <= 5)
        {
            // 1. Patient Notification
            var patientNotification = new Notification
            {
                UserId = log.Medicine.Patient.UserId,
                Title = "Low Medicine Stock",
                Message = $"{log.Medicine.Name} stock is low ({log.Medicine.Stock} left). Please refill.",
                Type = "Low Stock",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            await _notificationRepository.AddAsync(patientNotification);

            // 2. Linked Caregivers Notifications
            var caregivers = await _context.CaregiverPatients
                .Include(cp => cp.Caregiver)
                .Where(cp => cp.PatientId == log.PatientId)
                .ToListAsync();

            foreach (var cp in caregivers)
            {
                var caregiverNotification = new Notification
                {
                    UserId = cp.Caregiver.UserId,
                    Title = "Patient Medicine Low Stock Alert",
                    Message = $"Medicine '{log.Medicine.Name}' for patient is running low ({log.Medicine.Stock} doses remaining).",
                    Type = "Low Stock",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                await _notificationRepository.AddAsync(caregiverNotification);
            }
        }

        await _context.SaveChangesAsync();
        return log;
    }

    // Feature 6: Snooze Medicine for 15 minutes (Max 2 times)
    public async Task<MedicineLog?> SnoozeAsync(int id, int minutes = 15)
    {
        var log = await _context.MedicineLogs
            .Include(l => l.Medicine)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (log == null)
            return null;

        if (log.Status == "Taken")
            throw new InvalidOperationException("Taken medicine cannot be snoozed.");

        if (log.SnoozeCount >= 2)
            throw new InvalidOperationException("Maximum snooze limit (2 times) reached for this dose.");

        if (!log.OriginalScheduledTime.HasValue)
        {
            log.OriginalScheduledTime = log.ScheduledTime;
        }

        log.ScheduledTime = DateTime.Now.AddMinutes(minutes);
        log.Status = "Snoozed";
        log.SnoozeCount++;

        await _context.SaveChangesAsync();
        return log;
    }

    // Complete Filterable History Logic with MealTiming & SnoozeCount Projection
    public async Task<object> GetPatientHistoryAsync(int patientId, MedicineLogFilterDto filter)
    {
        var query = _context.MedicineLogs
            .Include(l => l.Medicine)
            .Where(l => l.PatientId == patientId)
            .AsQueryable();

        if (filter.FromDate.HasValue)
        {
            query = query.Where(l => l.ScheduledTime.Date >= filter.FromDate.Value.Date);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(l => l.ScheduledTime.Date <= filter.ToDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "All")
        {
            query = query.Where(l => l.Status == filter.Status);
        }

        var totalRecords = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.ScheduledTime)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(l => new PatientHistoryDto
            {
                Id = l.Id,
                MedicineId = l.MedicineId,
                MedicineName = l.Medicine.Name,
                Dosage = l.Medicine.Dosage,
                MealTiming = l.Medicine.MealTimeing,
                ScheduledTime = l.ScheduledTime,
                TakenTime = l.TakenTime,
                Status = l.Status,
                SnoozeCount = l.SnoozeCount
            })
            .ToListAsync();

        return new
        {
            TotalRecords = totalRecords,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalRecords / filter.PageSize),
            Data = logs
        };
    }

    // Feature 1 & 7: History with MealTiming Projection
    public async Task<IEnumerable<PatientHistoryDto>> GetPatientHistoryAsync(int patientId)
    {
        return await _context.MedicineLogs
            .Include(l => l.Medicine)
            .Where(l => l.PatientId == patientId)
            .OrderByDescending(l => l.ScheduledTime)
            .Select(l => new PatientHistoryDto
            {
                Id = l.Id,
                MedicineId = l.MedicineId,
                MedicineName = l.Medicine.Name,
                Dosage = l.Medicine.Dosage,
                MealTiming = l.Medicine.MealTimeing,
                ScheduledTime = l.ScheduledTime,
                TakenTime = l.TakenTime,
                Status = l.Status,
                SnoozeCount = l.SnoozeCount
            })
            .ToListAsync();
    }
}