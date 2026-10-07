using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers;

[ApiController]
[Route("api/adherence")]
public class AdherenceController : ControllerBase
{
    private readonly IAdherenceService _service;

    public AdherenceController(IAdherenceService service)
    {
        _service = service;
    }

    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetPatientAdherence(int patientId)
    {
        var result =
            await _service.GetPatientAdherenceAsync(patientId);

        return Ok(result);
    }
}