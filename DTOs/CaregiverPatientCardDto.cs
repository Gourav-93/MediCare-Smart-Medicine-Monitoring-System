namespace Medicare.DTOs;

public class CaregiverPatientCardDto
{
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string EmergencyContact { get; set; } = string.Empty;
    public string AdherenceRate { get; set; } 
    public int PendingDosesToday { get; set; }
    public int MissedDosesToday { get; set; }
}