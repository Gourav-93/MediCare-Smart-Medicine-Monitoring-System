using MediCare.Data;
using MediCare.DTOs;
using MediCare.Models;
using MediCare.Repositories.Interfaces;
using MediCare.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Service;

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

    public async Task<MedicineLog?> MarkAsTakenAsync(int id)
    {
        var log = await _repository.GetByIdAsync(id);

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

        // Low Stock Alert Trigger (Patient + Linked Caregivers)
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

        return await _repository.UpdateAsync(log);
    }

    // Complete Filterable History Logic
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
            .Select(l => new
            {
                l.Id,
                l.MedicineId,
                MedicineName = l.Medicine.Name,
                Dosage = l.Medicine.Dosage,
                l.ScheduledTime,
                l.TakenTime,
                l.Status
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

    public async Task<MedicineLog?> SnoozeAsync(int id, int minutes = 15)
    {
        var log = await _context.MedicineLogs.Include(l => l.Medicine).FirstOrDefaultAsync(l => l.Id == id);
        if (log == null)
            return null;
        if (log.Status == "Taken")
            throw new InvalidOperationException("Cannot snooze a log that has already been marked as taken.");
        if (log.SnoozeCount >= 2)
            throw new InvalidOperationException("Cannot snooze more than 3 times.");
        if (log.OriginalScheduledTime.HasValue)
        {
            log.OriginalScheduledTime = log.ScheduledTime;
        }
        log.ScheduledTime = DateTime.Now.AddMinutes(minutes);
        log.Status = "Snoozed";
        log.SnoozeCount++;
        await _context.SaveChangesAsync();
        return log;
    }

}