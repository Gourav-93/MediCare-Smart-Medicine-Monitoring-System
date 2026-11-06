namespace MediCare.DTOs;

public class UpdateProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string BloodGroup { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
}
