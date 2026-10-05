using MediCare.DTOs;
using MediCare.Models;
using MediCare.Repositories.Interfaces;
using MediCare.Services.Interfaces;

namespace MediCare.Service;

public class MedicineScheduleService : IMedicineScheduleService
{
    private readonly IMedicineScheduleRepository _repository;

    public MedicineScheduleService(IMedicineScheduleRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<MedicineSchedule>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<MedicineSchedule?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<MedicineSchedule> AddAsync(MedicineScheduleDto dto)
    {
        var schedule = new MedicineSchedule
        {
            MedicineId = dto.MedicineId,
            Time = dto.Time,
            Frequency = dto.Frequency
        };

        return await _repository.AddAsync(schedule);
    }

    public async Task<MedicineSchedule?> UpdateAsync(
        int id,
        MedicineScheduleDto dto)
    {
        var schedule = await _repository.GetByIdAsync(id);

        if (schedule == null)
        {
            return null;
        }

        schedule.MedicineId = dto.MedicineId;
        schedule.Time = dto.Time;
        schedule.Frequency = dto.Frequency;

        return await _repository.UpdateAsync(schedule);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var schedule = await _repository.GetByIdAsync(id);

        if (schedule == null)
        {
            return false;
        }

        await _repository.DeleteAsync(schedule);

        return true;
    }
}