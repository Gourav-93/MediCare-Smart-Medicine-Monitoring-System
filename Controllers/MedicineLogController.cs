using MediCare.DTOs;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicineLogController : ControllerBase
{
    private readonly IMedicineLogService _service;

    public MedicineLogController(IMedicineLogService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var logs = await _service.GetAllAsync();

        return Ok(logs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var log = await _service.GetByIdAsync(id);

        if (log == null)
        {
            return NotFound("Medicine log not found.");
        }

        return Ok(log);
    }

    [HttpPost]
    public async Task<IActionResult> Add(MedicineLogDto dto)
    {
        var log = await _service.AddAsync(dto);

        return Ok(log);
    }

    [HttpPut("{id}/taken")]
    public async Task<IActionResult> MarkAsTaken(int id)
    {
        var log = await _service.MarkAsTakenAsync(id);

        if (log == null)
        {
            return NotFound("Medicine log not found.");
        }

        return Ok(log);
    }
}