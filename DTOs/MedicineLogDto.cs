namespace MediCare.DTOs;

public class MedicineLogDto
{
    public int MedicineId { get; set; }
    public int PatientId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; } = string.Empty;
}