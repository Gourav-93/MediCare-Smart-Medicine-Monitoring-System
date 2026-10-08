namespace MediCare.DTOs;

public class CaregiverDashboardPatientDto
{
    public int PatientId { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string EmergencyContact { get; set; } = string.Empty;
}