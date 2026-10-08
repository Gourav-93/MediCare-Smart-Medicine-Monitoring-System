using MediCare.Models;

namespace MediCare.DTOs;

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }

    public int TotalPatients { get; set; }

    public int TotalCaregivers { get; set; }

    public int TotalMedicines { get; set; }

    public int TotalMedicineLogs { get; set; }

    public int TotalTakenDoses { get; set; }

    public int TotalMissedDoses { get; set; }

    public int TotalPendingDoses { get; set; }

    public double OverallAdherencePercentage { get; set; }

    public int ActiveEmergencyAlerts { get; set; }

    public int TotalEmergencyAlerts { get; set; }

    public int UnreadNotifications { get; set; }

    public int CaregiverPatientRelationships { get; set; }

    public List<Medicine> LowStockMedicines { get; set; } = new();

    public List<EmergencyAlert> ActiveEmergencies { get; set; } = new();
}