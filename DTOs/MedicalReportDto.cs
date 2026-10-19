namespace mediCare.DTOs;

public class MedicalReportDto
{
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string EmergencyContact { get; set; } = string.Empty;
    public DateTime ReportGeneratedAt { get; set; } = DateTime.Now;
    public double OverallAdherence { get; set; }
    public int TotalDosesRecorded { get; set; }
    public int TotalTaken { get; set; }
    public int TotalMissed { get; set; }
    public List<MedicineItemSummaryDto> ActiveMedicines { get; set; } = new();
    public List<LogItemSummaryDto> RecentLogs { get; set; } = new();
}

public class MedicineItemSummaryDto
{
    public string Name { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public int Dose { get; set; }
    public int Stock { get; set; }
    public List<string> ScheduledTimes { get; set; } = new();
}

public class LogItemSummaryDto
{
    public string MedicineName { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public DateTime? TakenTime { get; set; }
    public string Status { get; set; } = string.Empty;
}