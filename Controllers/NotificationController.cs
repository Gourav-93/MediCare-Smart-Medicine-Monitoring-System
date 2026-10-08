using System.Security.Claims;
using MediCare.Data;
using MediCare.Models;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Controllers;

[ApiController]
[Route("api/notification")]
[Authorize(Roles = "ADMIN,PATIENT,CAREGIVER")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _service;
    private readonly AppDbContext _context;

    public NotificationController(
        INotificationService service,
        AppDbContext context)
    {
        _service = service;
        _context = context;
    }

    private int GetCurrentUserId()
    {
        return int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );
    }

    private string GetCurrentRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? "";
    }

    // GET: api/notification
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentRole();

        if (role == "ADMIN")
        {
            var allNotifications = await _service.GetAllAsync();
            return Ok(allNotifications);
        }

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Ok(notifications);
    }

    // GET: api/notification/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id);

        if (notification == null)
            return NotFound("Notification not found.");

        var userId = GetCurrentUserId();
        var role = GetCurrentRole();

        if (role != "ADMIN" && notification.UserId != userId)
            return Forbid();

        return Ok(notification);
    }

    // POST: api/notification
    // Only ADMIN can manually create notifications.
    // System notifications are still created through the service.
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create(Notification notification)
    {
        var userExists = await _context.Users
            .AnyAsync(u => u.Id == notification.UserId);

        if (!userExists)
            return NotFound("User not found.");

        var result = await _service.CreateAsync(notification);

        return Ok(result);
    }

    // PUT: api/notification/{id}/read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id);

        if (notification == null)
            return NotFound("Notification not found.");

        var userId = GetCurrentUserId();
        var role = GetCurrentRole();

        if (role != "ADMIN" && notification.UserId != userId)
            return Forbid();

        var result = await _service.MarkAsReadAsync(id);

        if (result == null)
            return NotFound("Notification not found.");

        return Ok(result);
    }

    // PUT: api/notification/read-all
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();

        await _service.MarkAllAsReadAsync(userId);

        return Ok("All notifications marked as read.");
    }
}