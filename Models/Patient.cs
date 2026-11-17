using System;
using System.Collections.Generic;
using MediCare.Data;

namespace MediCare.Models;

public class Patient
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime DateOfBirth { get; set; }

    public string EmergencyContact { get; set; } = string.Empty;
    public string DoctorContact { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;

    public string? DoctorName { get; set; }
    public string? HospitalOrClinicName { get; set; }
    public string? MedicalConditions { get; set; }
    public string? Allergies { get; set; }
    public string? PastMedicalHistory { get; set; }
    public string? MedicalNotes { get; set; }

    public User User { get; set; } = null!;

    public ICollection<CaregiverPatient> CaregiverPatients { get; set; }
        = new List<CaregiverPatient>();

    public ICollection<EmergencyContact> EmergencyContacts { get; set; }
        = new List<EmergencyContact>();
}
