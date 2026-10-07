using MediCare.Models;

namespace MediCare.Repositories.Interfaces;

public interface IMedicineScheduleRepository
{
    Task<List<MedicineSchedule>> GetAllAsync();

    Task<MedicineSchedule?> GetByIdAsync(int id);

    Task<MedicineSchedule> AddAsync(MedicineSchedule schedule);

    Task<MedicineSchedule> UpdateAsync(MedicineSchedule schedule);

    Task DeleteAsync(MedicineSchedule schedule);

    Task<bool> MedicineExistsAsync(int medicineId);
}