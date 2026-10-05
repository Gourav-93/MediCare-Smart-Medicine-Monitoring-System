using MediCare.DTOs;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicineScheduleController : ControllerBase
{
    private readonly IMedicineScheduleService _service;

    public MedicineScheduleController(IMedicineScheduleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var schedules = await _service.GetAllAsync();

        return Ok(schedules);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var schedule = await _service.GetByIdAsync(id);

        if (schedule == null)
        {
            return NotFound("Schedule not found.");
        }

        return Ok(schedule);
    }

    [HttpPost]
    public async Task<IActionResult> Add(MedicineScheduleDto dto)
    {
        var schedule = await _service.AddAsync(dto);

        return Ok(schedule);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        MedicineScheduleDto dto)
    {
        var schedule = await _service.UpdateAsync(id, dto);

        if (schedule == null)
        {
            return NotFound("Schedule not found.");
        }

        return Ok(schedule);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound("Schedule not found.");
        }

        return Ok("Schedule deleted successfully.");
    }
}