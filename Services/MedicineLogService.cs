using MediCare.DTOs;
using MediCare.Models;
using MediCare.Repositories.Interfaces;
using MediCare.Services.Interfaces;

namespace MediCare.Service;

public class MedicineLogService : IMedicineLogService
{
    private readonly IMedicineLogRepository _repository;
    private readonly INotificationRepository _notificationRepository;

    public MedicineLogService(
        IMedicineLogRepository repository,
        INotificationRepository notificationRepository)
    {
        _repository = repository;
        _notificationRepository = notificationRepository;
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

        if (log.Medicine.Stock >= log.Medicine.Dose)
        {
            log.Medicine.Stock -= log.Medicine.Dose;
        }
        else
        {
            log.Medicine.Stock = 0;
        }

        return await _repository.UpdateAsync(log);
    }
}