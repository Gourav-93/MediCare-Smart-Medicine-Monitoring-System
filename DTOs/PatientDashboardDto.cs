using MediCare.Models;

namespace MediCare.DTOs;

public class PatientDashboardDto
{
    public PatientInfoDto Patient { get; set; } = new();

    public int TotalMedicines { get; set; }

    public List<Medicine> Medicines { get; set; } = new();

    public List<MedicineSchedule> TodaySchedules { get; set; } = new();

    public List<MedicineLog> PendingMedicines { get; set; } = new();

    public List<MedicineLog> MissedMedicines { get; set; } = new();

    public List<MedicineLog> TakenMedicines { get; set; } = new();

    public int TotalDoses { get; set; }

    public int TakenDoses { get; set; }

    public int MissedDoses { get; set; }

    public double AdherencePercentage { get; set; }

    public List<Medicine> LowStockMedicines { get; set; } = new();

    public List<EmergencyAlert> ActiveEmergencies { get; set; } = new();

    public int UnreadNotifications { get; set; }
}

public class PatientInfoDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string EmergencyContact { get; set; } = string.Empty;
}