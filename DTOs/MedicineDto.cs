namespace MediCare.DTOs;

public class MedicineDto
{
    public int PatientId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Dosage { get; set; } = string.Empty;

    public int Dose { get; set; }

    public int Stock { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}