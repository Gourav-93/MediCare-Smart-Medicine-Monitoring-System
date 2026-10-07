namespace MediCare.Services.Interfaces;

public interface IAdherenceService
{
    Task<object> GetPatientAdherenceAsync(int patientId);
}