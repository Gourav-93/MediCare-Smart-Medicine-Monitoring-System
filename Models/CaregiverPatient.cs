using System;
using MediCare.Data;

namespace MediCare.Models;

public class CaregiverPatient
{
    public int Id { get; set; }

    public int CaregiverId { get; set; }

    public int PatientId { get; set; }

    public string RelationType { get; set; } = string.Empty;

    public string? CaregivingNotes { get; set; }

    public bool IsPrimary { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Caregiver Caregiver { get; set; } = null!;

    public Patient Patient { get; set; } = null!;
}
