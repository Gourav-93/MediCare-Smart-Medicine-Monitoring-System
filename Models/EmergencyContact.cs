using System;

namespace MediCare.Models;

public class EmergencyContact
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string ContactName { get; set; } = string.Empty;

    public string Relationship { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? AlternatePhoneNumber { get; set; }

    public bool IsPrimary { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Patient Patient { get; set; } = null!;
}

