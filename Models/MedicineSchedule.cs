namespace MediCare.Models;

public class MedicineSchedule
{
    public int Id { get; set; }

    public int MedicineId { get; set; }

    public TimeSpan Time { get; set; }

    public string Frequency { get; set; } = string.Empty;

    public Medicine Medicine { get; set; } = null!;
}