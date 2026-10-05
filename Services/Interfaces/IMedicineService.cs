using MediCare.DTOs;
using MediCare.Models;

namespace MediCare.Services.Interfaces;

public interface IMedicineService
{
    Task<List<Medicine>> GetAllAsync();

    Task<Medicine?> GetByIdAsync(int id);

    Task<Medicine> AddAsync(MedicineDto dto);

    Task<Medicine?> UpdateAsync(int id, MedicineDto dto);

    Task<bool> DeleteAsync(int id);
}