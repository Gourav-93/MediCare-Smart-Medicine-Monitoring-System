namespace MediCare.DTOs;

public class AdherenceDto
{
    public int PatientId { get; set; }
    public int TotalDoses { get; set; }
    public int TakenDoses { get; set; }
    public int MissedDoses { get; set; }
    public double AdherencePercentage { get; set; }
}