
using Medicare.DTOs;

namespace MediCare.Services.Interfaces;

public interface ICaregiverService
{
    Task<IEnumerable<CaregiverPatientCardDto>> GetLinkedPatientsCardsAsync(int currentUserId);
}