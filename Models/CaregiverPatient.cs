using MediCare.Data;
namespace MediCare.Models;

public class CaregiverPatient
{
    public int Id { get; set; }

    public int CaregiverId { get; set; }

    public int PatientId { get; set; }

    public string RelationType { get; set; } = string.Empty;

    public Caregiver Caregiver { get; set; } = null!;

    public Patient Patient { get; set; } = null!;
}