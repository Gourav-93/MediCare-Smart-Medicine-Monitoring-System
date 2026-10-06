using MediCare.DTOs;
using MediCare.Models;
using MediCare.Repositories.Interfaces;
using MediCare.Services.Interfaces;

namespace MediCare.Service;

public class CaregiverPatientService : ICaregiverPatientService
{
    private readonly ICaregiverPatientRepository _repository;

    public CaregiverPatientService(
        ICaregiverPatientRepository repository)
    {
        _repository = repository;
    }

    public async Task<CaregiverPatient> LinkPatientAsync(
        CaregiverPatientDto dto)
    {
        var relationship = new CaregiverPatient
        {
            CaregiverId = dto.CaregiverId,
            PatientId = dto.PatientId,
            RelationType = dto.RelationType
        };

        return await _repository.AddAsync(relationship);
    }

    public async Task<List<CaregiverPatient>> GetPatientsAsync(
        int caregiverId)
    {
        return await _repository.GetByCaregiverIdAsync(caregiverId);
    }

    public async Task<List<CaregiverPatient>> GetCaregiversAsync(
        int patientId)
    {
        return await _repository.GetByPatientIdAsync(patientId);
    }

    public async Task<bool> UnlinkPatientAsync(
        int caregiverId,
        int patientId)
    {
        return await _repository.DeleteAsync(
            caregiverId,
            patientId);
    }
}