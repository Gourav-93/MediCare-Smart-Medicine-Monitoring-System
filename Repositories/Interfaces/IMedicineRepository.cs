using MediCare.Models;

namespace MediCare.Repositories.Interfaces;

public interface IMedicineRepository
{
    Task<List<Medicine>> GetAllAsync();

    Task<Medicine?> GetByIdAsync(int id);

    Task<Medicine> AddAsync(Medicine medicine);

    Task<Medicine> UpdateAsync(Medicine medicine);

    Task DeleteAsync(Medicine medicine);

    Task<bool> PatientExistsAsync(int patientId);
}