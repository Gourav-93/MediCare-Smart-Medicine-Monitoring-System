using MediCare.Models;

namespace MediCare.Repositories.Interfaces;

public interface INotificationRepository
{
    Task<List<Notification>> GetAllAsync();
    Task<Notification?> GetByIdAsync(int id);
    Task<Notification> AddAsync(Notification notification);
    Task<Notification> MarkAsReadAsync(int id);
    Task MarkAllAsReadAsync(int userId);
}