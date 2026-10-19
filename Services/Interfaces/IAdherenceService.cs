using MediCare.DTOs;

namespace MediCare.Services.Interfaces;

public interface IAdherenceService
{
    Task<object> GetPatientAdherenceAsync(int patientId);
    Task<AdherenceReportDto> GetPeriodicAdherenceAsync(int patientId, string period); // <-- Naya Method
}