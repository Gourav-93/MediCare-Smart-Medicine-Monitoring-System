using MediCare.DTOs;
using MediCare.Models;

namespace MediCare.Services.Interfaces;

public interface ICaregiverPatientService
{
    Task<CaregiverPatient> LinkPatientAsync(CaregiverPatientDto dto);
    Task<List<CaregiverPatient>> GetPatientsAsync(int caregiverId);
    Task<List<CaregiverPatient>> GetCaregiversAsync(int patientId);
    Task<bool> UnlinkPatientAsync(int caregiverId, int patientId);
}