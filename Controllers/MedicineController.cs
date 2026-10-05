using MediCare.DTOs;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers;

[ApiController]
[Route("api/medicine")]
public class MedicineController : ControllerBase
{
    private readonly IMedicineService _service;

    public MedicineController(IMedicineService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var medicines = await _service.GetAllAsync();
        return Ok(medicines);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var medicine = await _service.GetByIdAsync(id);
        if (id == null)
        {
            return NotFound("Medicine Not Found.");
        }
        return Ok(medicine);
    }

    [HttpPost]
    public async Task<IActionResult> Add(MedicineDto dto)
    {
        var medicine = await _service.AddAsync(dto);
        return Ok(medicine);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, MedicineDto dto)
    {
        var medicine = await _service.UpdateAsync(id, dto);
        if (medicine == null)
        {
            return NotFound("Medicine Not Found");
        }
        return Ok(medicine);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound("Medicine not found.");
        }

        return Ok("Medicine deleted successfully.");
    }
}