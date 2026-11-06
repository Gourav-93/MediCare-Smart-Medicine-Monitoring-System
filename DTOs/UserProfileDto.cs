namespace MediCare.DTOs;

public class UserProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string BloodGroup { get; set; } = string.Empty;
    public string EmergencyContact { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public double? AdherencePercentage { get; set; }
    public int ActiveMedicineCount { get; set; }
}
