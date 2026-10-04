namespace MediCare.Models;

public class MedicineLog
{
    public int Id { get; set; }

    public int MedicineId { get; set; }

    public int PatientId { get; set; }  

    public DateTime ScheduledTime { get; set; }

    public DateTime? TakenTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public Medicine Medicine { get; set; } = null!;

    public Patient Patient { get; set; } = null!;
}