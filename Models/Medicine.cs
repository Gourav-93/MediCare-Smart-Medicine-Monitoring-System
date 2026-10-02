namespace MediCare.Models;

public class Medicine
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Dosage { get; set; } = string.Empty;

    public int Dose { get; set; }

    public int Stock { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public Patient Patient { get; set; } = null!;

    public ICollection<MedicineSchedule> Schedules { get; set; }
        = new List<MedicineSchedule>();

    public ICollection<MedicineLog> MedicineLogs { get; set; }
        = new List<MedicineLog>();
}   