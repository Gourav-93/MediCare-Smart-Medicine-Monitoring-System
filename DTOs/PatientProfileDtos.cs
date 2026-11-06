using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediCare.DTOs;

public class PatientProfileDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string BloodGroup { get; set; } = string.Empty;

    public string? DoctorName { get; set; }
    public string? DoctorContact { get; set; }
    public string? HospitalOrClinicName { get; set; }
    public string? MedicalConditions { get; set; }
    public string? Allergies { get; set; }
    public string? PastMedicalHistory { get; set; }
    public string? MedicalNotes { get; set; }

    public string? PrimaryEmergencyPhone { get; set; }
    public int LinkedCaregiverCount { get; set; }
    public int ActiveMedicineCount { get; set; }
    public double? AdherencePercentage { get; set; }
    public int ProfileCompletionPercentage { get; set; }
    public DateTime AccountCreatedAt { get; set; }
}

public class UpdatePatientProfileDto
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full Name must be between 2 and 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format.")]
    [StringLength(25, ErrorMessage = "Phone number cannot exceed 25 characters.")]
    public string? PhoneNumber { get; set; }

    [StringLength(20, ErrorMessage = "Gender cannot exceed 20 characters.")]
    public string? Gender { get; set; }

    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
    public string? Address { get; set; }

    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    public string? City { get; set; }

    public DateTime? DateOfBirth { get; set; }
}

public class PatientMedicalDto
{
    public string BloodGroup { get; set; } = string.Empty;
    public string? DoctorName { get; set; }
    public string? DoctorContact { get; set; }
    public string? HospitalOrClinicName { get; set; }
    public string? MedicalConditions { get; set; }
    public string? Allergies { get; set; }
    public string? PastMedicalHistory { get; set; }
    public string? MedicalNotes { get; set; }
}

public class UpdatePatientMedicalDto
{
    [RegularExpression(@"^(A\+|A\-|B\+|B\-|AB\+|AB\-|O\+|O\-|Unknown)?$", ErrorMessage = "Valid blood groups are A+, A-, B+, B-, AB+, AB-, O+, O-, or Unknown.")]
    public string? BloodGroup { get; set; }

    [StringLength(100, ErrorMessage = "Doctor Name cannot exceed 100 characters.")]
    public string? DoctorName { get; set; }

    [Phone(ErrorMessage = "Invalid doctor contact number format.")]
    [StringLength(25, ErrorMessage = "Doctor Contact cannot exceed 25 characters.")]
    public string? DoctorContact { get; set; }

    [StringLength(150, ErrorMessage = "Hospital or Clinic Name cannot exceed 150 characters.")]
    public string? HospitalOrClinicName { get; set; }

    [StringLength(500, ErrorMessage = "Medical conditions cannot exceed 500 characters.")]
    public string? MedicalConditions { get; set; }

    [StringLength(500, ErrorMessage = "Allergies cannot exceed 500 characters.")]
    public string? Allergies { get; set; }

    [StringLength(1000, ErrorMessage = "Past medical history cannot exceed 1000 characters.")]
    public string? PastMedicalHistory { get; set; }

    [StringLength(1000, ErrorMessage = "Medical notes cannot exceed 1000 characters.")]
    public string? MedicalNotes { get; set; }
}

public class EmergencyContactDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? AlternatePhoneNumber { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateEmergencyContactDto
{
    [Required(ErrorMessage = "Contact Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Contact Name must be between 2 and 100 characters.")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Relationship is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Relationship must be between 2 and 50 characters.")]
    public string Relationship { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone Number is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    [StringLength(25, ErrorMessage = "Phone number cannot exceed 25 characters.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid alternate phone number format.")]
    [StringLength(25, ErrorMessage = "Alternate phone number cannot exceed 25 characters.")]
    public string? AlternatePhoneNumber { get; set; }

    public bool IsPrimary { get; set; } = false;
}

public class UpdateEmergencyContactDto
{
    [Required(ErrorMessage = "Contact Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Contact Name must be between 2 and 100 characters.")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Relationship is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Relationship must be between 2 and 50 characters.")]
    public string Relationship { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone Number is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    [StringLength(25, ErrorMessage = "Phone number cannot exceed 25 characters.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid alternate phone number format.")]
    [StringLength(25, ErrorMessage = "Alternate phone number cannot exceed 25 characters.")]
    public string? AlternatePhoneNumber { get; set; }

    public bool IsPrimary { get; set; } = false;
}

public class PatientProfileSummaryDto
{
    public int ProfileCompletionPercentage { get; set; }
    public int CurrentMedicineCount { get; set; }
    public int LinkedCaregiverCount { get; set; }
    public int EmergencyContactCount { get; set; }
    public bool HasEmergencyContact { get; set; }
    public double? AdherencePercentage { get; set; }
    public List<string> MissingInformation { get; set; } = new();
    public List<PatientScheduleSummaryItemDto> UpcomingSchedules { get; set; } = new();
    public List<PatientRecentActivityItemDto> RecentActivity { get; set; } = new();
}

public class PatientScheduleSummaryItemDto
{
    public int ScheduleId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string MealTiming { get; set; } = string.Empty;
    public string ScheduledTime { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
}

public class PatientRecentActivityItemDto
{
    public int LogId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public DateTime? TakenTime { get; set; }
}

public class PatientLinkedCaregiverDto
{
    public int CaregiverPatientId { get; set; }
    public int CaregiverId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string Relationship { get; set; } = string.Empty;
    public string RelationType { get; set; } = string.Empty;
    public string? PreferredContactMethod { get; set; }
    public string? CaregivingNotes { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime LinkedSince { get; set; }
}

