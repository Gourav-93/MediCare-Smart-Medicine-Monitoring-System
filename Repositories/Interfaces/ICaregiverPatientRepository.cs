using MediCare.Models;

namespace MediCare.Repositories.Interfaces;

public interface ICaregiverPatientRepository
{
    Task<CaregiverPatient> AddAsync(CaregiverPatient caregiverPatient);
    Task<List<CaregiverPatient>> GetByCaregiverIdAsync(int caregiverId);
    Task<List<CaregiverPatient>> GetByPatientIdAsync(int patientId);
    Task<bool> DeleteAsync(int caregiverId, int patientId);
    Task<bool> CaregiverExistsAsync(int caregiverId);
    Task<bool> PatientExistsAsync(int patientId);
}