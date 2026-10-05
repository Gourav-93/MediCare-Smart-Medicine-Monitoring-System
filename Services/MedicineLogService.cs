using MediCare.DTOs;
using MediCare.Models;
using MediCare.Repositories.Interfaces;
using MediCare.Services.Interfaces;

namespace MediCare.Service;

public class MedicineLogService : IMedicineLogService
{
    private readonly IMedicineLogRepository _repository;

    public MedicineLogService(IMedicineLogRepository repository)
    {
        _repository = repository;
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
        {
            return null;
        }

        log.Status = "Taken";
        log.TakenTime = DateTime.Now;

        return await _repository.UpdateAsync(log);
    }
}