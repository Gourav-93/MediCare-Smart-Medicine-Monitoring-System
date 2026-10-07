using MediCare.Models;
namespace MediCare.Repositories.Interfaces;

public interface IMedicineLogRepository
{
    Task<List<MedicineLog>> GetAllAsync();
    Task<MedicineLog?> GetByIdAsync(int id);
    Task<MedicineLog> AddAsync(MedicineLog log);
    Task<MedicineLog> UpdateAsync(MedicineLog log);
    Task DeleteAsync(int id);
    Task<bool> MedicineExistsAsync(int medicineId);
    Task<bool> PatientExistsAsync(int patientId);
}