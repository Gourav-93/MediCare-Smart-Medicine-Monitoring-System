namespace MediCare.DTOs;

public class PatientHistoryDto
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string MealTiming { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public DateTime? TakenTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public int SnoozeCount { get; set; }
}
