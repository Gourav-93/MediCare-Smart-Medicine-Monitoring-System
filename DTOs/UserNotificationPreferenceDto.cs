namespace MediCare.DTOs;

public class UserNotificationPreferenceDto
{
    public bool DoseReminders { get; set; } = true;
    public bool WhatsAppReminders { get; set; } = true;
    public bool CaregiverAlerts { get; set; } = true;
    public bool RefillAlerts { get; set; } = true;
    public bool LowStockAlerts { get; set; } = true;
    public bool EmailNotifications { get; set; } = true;
    public bool InAppNotifications { get; set; } = true;
    public bool EmergencyAlerts { get; set; } = true;
    public bool MissedDoseAlerts { get; set; } = true;
}
