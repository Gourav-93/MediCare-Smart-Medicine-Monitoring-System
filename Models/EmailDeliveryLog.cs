using MediCare.Data;

namespace MediCare.Models;

public class EmailDeliveryLog
{
    public int Id { get; set; }
    
    public int MedicineScheduleId { get; set; }
    
    public DateTime ScheduledOccurrence { get; set; }
    
    public string NotificationType { get; set; } = string.Empty; // "Reminder", "Missed", "CaregiverMissed"
    
    public string RecipientEmail { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty; // "Pending", "Sent", "Failed"
    
    public DateTime? SentAt { get; set; }
    
    public int RetryCount { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public MedicineSchedule MedicineSchedule { get; set; } = null!;
}
