using System.Text.Json.Serialization;
using MediCare.Models;

namespace MediCare.Data;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public string? PasswordResetToken { get; set; }

    [JsonIgnore]
    public DateTime? PasswordResetTokenExpiry { get; set; }

    public Patient? Patient { get; set; }
    public Caregiver? Caregiver { get; set; }
    public UserNotificationPreference? NotificationPreference { get; set; }
}
