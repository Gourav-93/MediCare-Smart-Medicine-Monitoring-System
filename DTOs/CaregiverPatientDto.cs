namespace MediCare.DTOs;

public class CaregiverPatientDto
{
    public int CaregiverId { get; set; }
    public int PatientId { get; set; }
    public string RelationType { get; set; } = string.Empty;
}