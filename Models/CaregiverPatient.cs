using MediCare.Data;

namespace MediCare.Models;

public class CaregiverPatient
{
    public int Id { get; set; }
    public int CaregiverId { get; set; }
    public int PatientId { get; set; }
    public string Relationship { get; set; }
    public Caregiver Caregiver { get; set; } = null;
    public Patient Patient { get; set; } = null;

    public ICollection<CaregiverPatient> CaregiverPatients { get; set; }
                = new List<CaregiverPatient>();

}