namespace MediCare.Models;

public class MedicineLog
{
    public int Id { get; set; }

    public int MedicineId { get; set; }

    public int PatientId { get; set; }  

    public DateTime ScheduledTime { get; set; }

    public DateTime? TakenTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public int SnoozeCount { get; set; } = 0;

    public DateTime? OriginalScheduledTime { get; set; }
    
    // Historical snapshot fields to preserve medicine identity even if modified or soft-deleted
    public string? MedicineName { get; set; }

    public string? Dosage { get; set; }

    public string? MealTiming { get; set; }

    // Related schedule id
    public int? ScheduleId { get; set; }

    public Medicine Medicine { get; set; } = null!;
    
    public Patient Patient { get; set; } = null!;

    public MedicineSchedule? Schedule { get; set; }
}