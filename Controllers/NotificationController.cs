using MediCare.Models;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers;

[ApiController]
[Route("api/notification")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationController(INotificationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var notifications = await _service.GetAllAsync();

        return Ok(notifications);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var notification = await _service.GetByIdAsync(id);

        if (notification == null)
        {
            return NotFound("Notification not found.");
        }

        return Ok(notification);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Notification notification)
    {
        var result = await _service.CreateAsync(notification);

        return Ok(result);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var notification = await _service.MarkAsReadAsync(id);

        if (notification == null)
        {
            return NotFound("Notification not found.");
        }

        return Ok(notification);
    }

    [HttpPut("user/{userId}/read-all")]
    public async Task<IActionResult> MarkAllAsRead(int userId)
    {
        await _service.MarkAllAsReadAsync(userId);

        return Ok("All notifications marked as read.");
    }
}