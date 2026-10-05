using MediCare.DTOs;
using MediCare.Models;

namespace MediCare.Services.Interfaces;

public interface IMedicineLogService
{
    Task<List<MedicineLog>> GetAllAsync();
    Task<MedicineLog?> GetByIdAsync(int id);
    Task<MedicineLog> AddAsync(MedicineLogDto dto);
    Task<MedicineLog?> MarkAsTakenAsync(int id);
}