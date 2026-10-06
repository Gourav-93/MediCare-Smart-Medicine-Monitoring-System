using MediCare.Models;
using MediCare.Repositories.Interfaces;
using MediCare.Services.Interfaces;

namespace MediCare.Service;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;

    public NotificationService(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Notification>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Notification?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        notification.IsRead = false;
        notification.CreatedAt = DateTime.Now;

        return await _repository.AddAsync(notification);
    }

    public async Task<Notification?> MarkAsReadAsync(int id)
    {
        return await _repository.MarkAsReadAsync(id);
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        await _repository.MarkAllAsReadAsync(userId);
    }
}