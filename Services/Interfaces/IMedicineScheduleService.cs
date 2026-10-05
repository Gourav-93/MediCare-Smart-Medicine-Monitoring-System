using MediCare.DTOs;
using MediCare.Models;

namespace MediCare.Services.Interfaces;

public interface IMedicineScheduleService
{
    Task<List<MedicineSchedule>> GetAllAsync();
    Task<MedicineSchedule?> GetByIdAsync(int id);
    Task<MedicineSchedule> AddAsync(MedicineScheduleDto dto);
    Task<MedicineSchedule?> UpdateAsync(int id, MedicineScheduleDto dto);
    Task<bool> DeleteAsync(int id);
}