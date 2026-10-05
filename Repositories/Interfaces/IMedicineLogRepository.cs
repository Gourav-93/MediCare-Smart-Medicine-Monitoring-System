using MediCare.Models;
namespace MediCare.Repositories.Interfaces;

public interface IMedicineLogRepository
{
    Task<List<MedicineLog>> GetAllAsync();
    Task<MedicineLog?> GetByIdAsync(int id);
    Task<MedicineLog> AddAsync(MedicineLog log);
    Task<MedicineLog> UpdateAsync(MedicineLog log);
}