using MediCare.Models;

namespace MediCare.Data;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Patient? Patient { get; set; }
    public Caregiver? Caregiver { get; set; }

}