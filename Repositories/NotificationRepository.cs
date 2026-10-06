using MediCare.Data;
using MediCare.Models;
using MediCare.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Notification>> GetAllAsync()
    {
        return await _context.Notifications.ToListAsync();
    }

    public async Task<Notification?> GetByIdAsync(int id)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Notification> AddAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return notification;
    }

    public async Task<Notification> MarkAsReadAsync(int id)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(x => x.Id == id);

        if (notification == null)
        {
            throw new Exception("Notification not found.");
        }

        notification.IsRead = true;

        await _context.SaveChangesAsync();

        return notification;
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }
}