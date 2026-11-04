using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediCare.DTOs;

public class CaregiverProfileDto
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
    public string? PreferredContactMethod { get; set; }
    public string? Relationship { get; set; }
    public DateTime AccountCreatedAt { get; set; }
    public int LinkedPatientCount { get; set; }
    public CaregiverNotificationPreferencesDto? NotificationPreferences { get; set; }
}

public class UpdateCaregiverProfileDto
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

    [StringLength(50, ErrorMessage = "Preferred contact method cannot exceed 50 characters.")]
    public string? PreferredContactMethod { get; set; }

    [StringLength(50, ErrorMessage = "Relationship description cannot exceed 50 characters.")]
    public string? Relationship { get; set; }
}

public class CaregiverLinkedPatientDto
{
    public int CaregiverPatientId { get; set; }
    public int PatientId { get; set; }
    public int UserId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string? ProfilePhotoUrl { get; set; }
    public string RelationType { get; set; } = string.Empty;
    public string? CaregivingNotes { get; set; }
    public bool IsPrimaryCaregiver { get; set; }
    public DateTime LinkedSince { get; set; }
    public int? Age { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string BloodGroup { get; set; } = string.Empty;
    public string? PrimaryEmergencyPhone { get; set; }
    public int ActiveMedicineCount { get; set; }
    public double? AdherencePercentage { get; set; }
}

public class UpdateCaregiverPatientLinkDto
{
    [StringLength(50, ErrorMessage = "Relation type cannot exceed 50 characters.")]
    public string? RelationType { get; set; }

    [StringLength(500, ErrorMessage = "Caregiving notes cannot exceed 500 characters.")]
    public string? CaregivingNotes { get; set; }

    public bool? IsPrimary { get; set; }
}

public class CaregiverNotificationPreferencesDto
{
    public bool DoseReminders { get; set; } = true;
    public bool MissedDoseAlerts { get; set; } = true;
    public bool CaregiverAlerts { get; set; } = true;
    public bool RefillAlerts { get; set; } = true;
    public bool LowStockAlerts { get; set; } = true;
    public bool EmailNotifications { get; set; } = true;
    public bool InAppNotifications { get; set; } = true;
    public bool EmergencyAlerts { get; set; } = true;
    public bool WhatsAppReminders { get; set; } = false;
}

