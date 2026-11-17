using System;
using MediCare.Data;

namespace MediCare.Models;

public class UserNotificationPreference
{
    public int Id { get; set; }

    public int UserId { get; set; }
 
    public bool DoseReminders { get; set; } = true;

    public bool CaregiverAlerts { get; set; } = true;

    public bool RefillAlerts { get; set; } = true;

    public bool WhatsAppReminders { get; set; } = false;

    public bool LowStockAlerts { get; set; } = true;

    public bool EmailNotifications { get; set; } = true;

    public bool InAppNotifications { get; set; } = true;

    public bool EmergencyAlerts { get; set; } = true;

    public bool MissedDoseAlerts { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
