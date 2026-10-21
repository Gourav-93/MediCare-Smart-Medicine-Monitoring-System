using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediCare.Services.Interfaces;

namespace MediCare.Controllers;

[ApiController]
[Route("api/caregiver")]
[Authorize(Roles = "CAREGIVER,ADMIN")]
public class CaregiverController : ControllerBase
{
    private readonly ICaregiverService _caregiverService;

    public CaregiverController(ICaregiverService caregiverService)
    {
        _caregiverService = caregiverService;
    }

    // GET: api/caregiver/patients-contact-cards
    [HttpGet("patients-contact-cards")]
    public async Task<IActionResult> GetLinkedPatientsWithContact()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized();

        var currentUserId = int.Parse(userIdClaim);
        var result = await _caregiverService.GetLinkedPatientsCardsAsync(currentUserId);

        return Ok(result);
    }
}