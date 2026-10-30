using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using MediCare.DTOs;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public ProfileController(IProfileService profileService, IWebHostEnvironment env)
    {
        _profileService = profileService;
        _env = env;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private string GetCurrentRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }

    // ==========================================
    // PATIENT ENDPOINTS
    // ==========================================

    // GET: api/profile/patient
    [HttpGet("patient")]
    [Authorize(Roles = "PATIENT,CAREGIVER,ADMIN")]
    public async Task<IActionResult> GetPatientProfile([FromQuery] int? patientId)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentRole();

        try
        {
            var profile = await _profileService.GetPatientProfileAsync(userId, patientId, role);
            if (profile == null)
                return NotFound(new { message = "Patient profile not found." });

            return Ok(profile);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    // PUT: api/profile/patient
    [HttpPut("patient")]
    [Authorize(Roles = "PATIENT")]
    public async Task<IActionResult> UpdatePatientProfile([FromBody] UpdatePatientProfileDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        try
        {
            var updated = await _profileService.UpdatePatientProfileAsync(userId, dto);
            return Ok(new
            {
                message = "Patient profile updated successfully.",
                profile = updated
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // GET: api/profile/patient/medical
    [HttpGet("patient/medical")]
    [Authorize(Roles = "PATIENT,CAREGIVER,ADMIN")]
    public async Task<IActionResult> GetPatientMedical([FromQuery] int? patientId)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentRole();

        try
        {
            var medical = await _profileService.GetPatientMedicalAsync(userId, patientId, role);
            if (medical == null)
                return NotFound(new { message = "Medical details not found." });

            return Ok(medical);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    // PUT: api/profile/patient/medical
    [HttpPut("patient/medical")]
    [Authorize(Roles = "PATIENT")]
    public async Task<IActionResult> UpdatePatientMedical([FromBody] UpdatePatientMedicalDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        try
        {
            var updated = await _profileService.UpdatePatientMedicalAsync(userId, dto);
            return Ok(new
            {
                message = "Medical information updated successfully.",
                medical = updated
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // GET: api/profile/patient/emergency-contacts
    [HttpGet("patient/emergency-contacts")]
    [Authorize(Roles = "PATIENT,CAREGIVER,ADMIN")]
    public async Task<IActionResult> GetPatientEmergencyContacts([FromQuery] int? patientId)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentRole();

        try
        {
            var contacts = await _profileService.GetPatientEmergencyContactsAsync(userId, patientId, role);
            return Ok(contacts);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    // POST: api/profile/patient/emergency-contacts
    [HttpPost("patient/emergency-contacts")]
    [Authorize(Roles = "PATIENT")]
    public async Task<IActionResult> AddEmergencyContact([FromBody] CreateEmergencyContactDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        try
        {
            var contact = await _profileService.AddEmergencyContactAsync(userId, dto);
            return CreatedAtAction(nameof(GetPatientEmergencyContacts), new { id = contact.Id }, contact);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // PUT: api/profile/patient/emergency-contacts/{id}
    [HttpPut("patient/emergency-contacts/{id:int}")]
    [Authorize(Roles = "PATIENT")]
    public async Task<IActionResult> UpdateEmergencyContact(int id, [FromBody] UpdateEmergencyContactDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        try
        {
            var updated = await _profileService.UpdateEmergencyContactAsync(userId, id, dto);
            return Ok(new
            {
                message = "Emergency contact updated successfully.",
                contact = updated
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // DELETE: api/profile/patient/emergency-contacts/{id}
    [HttpDelete("patient/emergency-contacts/{id:int}")]
    [Authorize(Roles = "PATIENT")]
    public async Task<IActionResult> DeleteEmergencyContact(int id)
    {
        var userId = GetCurrentUserId();

        try
        {
            var deleted = await _profileService.DeleteEmergencyContactAsync(userId, id);
            if (!deleted)
                return NotFound(new { message = "Emergency contact not found." });

            return Ok(new { message = "Emergency contact deleted successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // GET: api/profile/patient/summary
    [HttpGet("patient/summary")]
    [Authorize(Roles = "PATIENT,CAREGIVER,ADMIN")]
    public async Task<IActionResult> GetPatientProfileSummary([FromQuery] int? patientId)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentRole();

        try
        {
            var summary = await _profileService.GetPatientProfileSummaryAsync(userId, patientId, role);
            if (summary == null)
                return NotFound(new { message = "Patient summary could not be loaded." });

            return Ok(summary);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    // GET: api/profile/patient/caregivers
    [HttpGet("patient/caregivers")]
    [Authorize(Roles = "PATIENT,ADMIN")]
    public async Task<IActionResult> GetPatientLinkedCaregivers()
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
            return Unauthorized();

        var caregivers = await _profileService.GetPatientLinkedCaregiversAsync(userId);
        return Ok(caregivers);
    }

    // ==========================================
    // CAREGIVER ENDPOINTS
    // ==========================================

    // GET: api/profile/caregiver
    [HttpGet("caregiver")]
    [Authorize(Roles = "CAREGIVER,ADMIN")]
    public async Task<IActionResult> GetCaregiverProfile()
    {
        var userId = GetCurrentUserId();

        var profile = await _profileService.GetCaregiverProfileAsync(userId);
        if (profile == null)
            return NotFound(new { message = "Caregiver profile not found." });

        return Ok(profile);
    }

    // PUT: api/profile/caregiver
    [HttpPut("caregiver")]
    [Authorize(Roles = "CAREGIVER")]
    public async Task<IActionResult> UpdateCaregiverProfile([FromBody] UpdateCaregiverProfileDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        try
        {
            var updated = await _profileService.UpdateCaregiverProfileAsync(userId, dto);
            return Ok(new
            {
                message = "Caregiver profile updated successfully.",
                profile = updated
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // GET: api/profile/caregiver/patients
    [HttpGet("caregiver/patients")]
    [Authorize(Roles = "CAREGIVER,ADMIN")]
    public async Task<IActionResult> GetCaregiverLinkedPatients()
    {
        var userId = GetCurrentUserId();

        var patients = await _profileService.GetCaregiverLinkedPatientsAsync(userId);
        return Ok(patients);
    }

    // PUT: api/profile/caregiver/patients/{caregiverPatientId}
    [HttpPut("caregiver/patients/{caregiverPatientId:int}")]
    [Authorize(Roles = "CAREGIVER")]
    public async Task<IActionResult> UpdateCaregiverPatientLink(int caregiverPatientId, [FromBody] UpdateCaregiverPatientLinkDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        try
        {
            var updated = await _profileService.UpdateCaregiverPatientLinkAsync(userId, caregiverPatientId, dto);
            return Ok(new
            {
                message = "Patient link updated successfully.",
                link = updated
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // GET: api/profile/caregiver/notification-preferences
    [HttpGet("caregiver/notification-preferences")]
    [Authorize(Roles = "CAREGIVER,PATIENT,ADMIN")]
    public async Task<IActionResult> GetNotificationPreferences()
    {
        var userId = GetCurrentUserId();

        var preferences = await _profileService.GetNotificationPreferencesAsync(userId);
        return Ok(preferences);
    }

    // PUT: api/profile/caregiver/notification-preferences
    [HttpPut("caregiver/notification-preferences")]
    [Authorize(Roles = "CAREGIVER,PATIENT,ADMIN")]
    public async Task<IActionResult> UpdateNotificationPreferences([FromBody] CaregiverNotificationPreferencesDto dto)
    {
        var userId = GetCurrentUserId();

        var updated = await _profileService.UpdateNotificationPreferencesAsync(userId, dto);
        return Ok(new
        {
            message = "Notification preferences updated successfully.",
            preferences = updated
        });
    }

    // ==========================================
    // PROFILE PHOTO UPLOAD (SECURE)
    // ==========================================

    // POST: api/profile/photo
    [HttpPost("photo")]
    public async Task<IActionResult> UploadProfilePhoto(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Please select an image file to upload." });

        if (file.Length > MaxFileSize)
            return BadRequest(new { message = "Image size exceeds the maximum limit of 5 MB." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !Array.Exists(AllowedExtensions, e => e == ext))
            return BadRequest(new { message = "Unsupported image format. Allowed formats: .jpg, .jpeg, .png, .webp" });

        // Validate MIME type
        var contentType = file.ContentType?.ToLowerInvariant() ?? "";
        if (!contentType.StartsWith("image/"))
            return BadRequest(new { message = "Invalid content type. Only images are permitted." });

        // Validate Magic Bytes (File Header) to prevent spoofing
        using (var stream = file.OpenReadStream())
        {
            var header = new byte[12];
            int bytesRead = await stream.ReadAsync(header.AsMemory(0, 12));
            if (bytesRead < 4)
                return BadRequest(new { message = "Invalid or corrupted image file." });

            bool isValidImageHeader = false;

            // JPEG magic bytes: FF D8 FF
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                isValidImageHeader = true;
            // PNG magic bytes: 89 50 4E 47 0D 0A 1A 0A
            else if (bytesRead >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                isValidImageHeader = true;
            // WebP magic bytes: RIFF .... WEBP
            else if (bytesRead >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                     header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
                isValidImageHeader = true;

            if (!isValidImageHeader)
                return BadRequest(new { message = "File header verification failed. The file is not a valid image." });
        }

        // Generate safe random server-side filename
        var safeFileName = $"profile_{Guid.NewGuid():N}{ext}";

        // Target directory: MediCare-Frontend/uploads/profiles/
        var uploadsDir = Path.Combine(_env.ContentRootPath, "MediCare-Frontend", "uploads", "profiles");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        var destinationPath = Path.Combine(uploadsDir, safeFileName);
        using (var outputStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
        {
            await file.CopyToAsync(outputStream);
        }

        var relativeUrl = $"/uploads/profiles/{safeFileName}";
        var userId = GetCurrentUserId();

        await _profileService.UpdateProfilePhotoAsync(userId, relativeUrl);

        return Ok(new
        {
            message = "Profile photo uploaded successfully.",
            photoUrl = relativeUrl
        });
    }
}

