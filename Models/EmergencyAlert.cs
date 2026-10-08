namespace MediCare.Models;

public class EmergencyAlert
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = "ACTIVE";

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public Patient Patient { get; set; } = null!;
}