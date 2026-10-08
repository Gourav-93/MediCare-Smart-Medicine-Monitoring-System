using MediCare.Models;

namespace MediCare.DTOs;

public class CaregiverDashboardDto
{
    public CaregiverInfoDto Caregiver { get; set; } = new();

    public List<PatientDashboardDto> Patients { get; set; } = new();

    public List<EmergencyAlert> ActiveEmergencies { get; set; } = new();

    public List<MedicineLog> MissedMedicines { get; set; } = new();

    public List<MedicineSchedule> TodaySchedules { get; set; } = new();

    public List<MedicineLog> PendingMedicines { get; set; } = new();

    public List<PatientAdherenceDto> Adherence { get; set; } = new();

    public int UnreadNotifications { get; set; }
}

public class CaregiverInfoDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Relationship { get; set; } = string.Empty;
}

public class PatientDashboardDto
{
    public int PatientId { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string EmergencyContact { get; set; } = string.Empty;
}

public class PatientAdherenceDto
{
    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public int TotalDoses { get; set; }

    public int TakenDoses { get; set; }

    public int MissedDoses { get; set; }

    public double AdherencePercentage { get; set; }
}