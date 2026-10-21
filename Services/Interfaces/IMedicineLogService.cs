using MediCare.DTOs;
using MediCare.Models;

namespace MediCare.Services.Interfaces;

public interface IMedicineLogService
{
    Task<List<MedicineLog>> GetAllAsync();
    Task<MedicineLog?> GetByIdAsync(int id);
    Task<MedicineLog> AddAsync(MedicineLogDto dto);
    Task<MedicineLog> AddAsync(MedicineLog log);
    Task<MedicineLog?> MarkAsTakenAsync(int id);
    Task<object> GetPatientHistoryAsync(int patientId, MedicineLogFilterDto filter);
    Task<IEnumerable<PatientHistoryDto>> GetPatientHistoryAsync(int patientId);
    Task<MedicineLog?> SnoozeAsync(int id, int minutes = 15);
}