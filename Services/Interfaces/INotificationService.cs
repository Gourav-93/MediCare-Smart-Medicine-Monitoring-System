using MediCare.Models;

namespace MediCare.Services.Interfaces;

public interface INotificationService
{
    Task<List<Notification>> GetAllAsync();
    Task<Notification?> GetByIdAsync(int id);
    Task<Notification> CreateAsync(Notification notification);
    Task<Notification?> MarkAsReadAsync(int id);
    Task MarkAllAsReadAsync(int userId);
}