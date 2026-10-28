using System.Security.Claims;
using System.Security.Cryptography;
using MediCare.Data;
using MediCare.DTOs;
using MediCare.Models;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Controllers;

[ApiController]
[Route("api/auth")]
[Route("api/AuthController")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthController(
        AppDbContext context,
        IJwtService jwtService,
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _emailService = emailService;
        _logger = logger;
        _passwordHasher = new PasswordHasher<User>();
    }

    // POST: api/Auth/register
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password) ||
            string.IsNullOrWhiteSpace(dto.Role))
        {
            return BadRequest(new { message = "Name, Email, Password and Role are required." });
        }

        if (dto.Password.Length < 6)
        {
            return BadRequest(new { message = "Password must be at least 6 characters long." });
        }


        var email = dto.Email.Trim().ToLower();

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == email);

        if (existingUser != null)
        {
            return BadRequest(new { message = "Email already exists." });
        }

        var role = dto.Role.Trim().ToUpper();

        // ADMIN cannot register publicly
        if (role != "PATIENT" && role != "CAREGIVER")
        {
            return BadRequest(new { message = "Only PATIENT or CAREGIVER registration is allowed." });
        }

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = email,
            Role = role,
            PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(
                user,
                dto.Password
            );

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        if (role == "PATIENT")
        {
            var patient = new Patient
            {
                UserId = user.Id,
                DateOfBirth = DateTime.Today,
                EmergencyContact = dto.PhoneNumber?.Trim() ?? string.Empty
            };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
        }
        else if (role == "CAREGIVER")
        {
            var caregiver = new Caregiver
            {
                UserId = user.Id,
                Relationship = ""
            };
            _context.Caregivers.Add(caregiver);
            await _context.SaveChangesAsync();
        }

        var token = _jwtService.GenerateToken(
            user.Id,
            user.Email,
            user.Role
        );

        return Ok(new
        {
            message = "User registered successfully.",
            token = token,
            userId = user.Id,
            name = user.Name,
            email = user.Email,
            role = user.Role
        });
    }

    // POST: api/Auth/login
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Email and Password are required." });
        }

        var email = dto.Email.Trim().ToLower();

        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == email);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.Password
        );

        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var token = _jwtService.GenerateToken(
            user.Id,
            user.Email,
            user.Role
        );

        return Ok(new
        {
            message = "Login successful.",
            token = token,
            userId = user.Id,
            name = user.Name,
            email = user.Email,
            role = user.Role
        });
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }

    // POST: api/auth/change-password
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return Unauthorized(new { message = "User not identified." });
        }

        if (string.IsNullOrWhiteSpace(dto.CurrentPassword) ||
            string.IsNullOrWhiteSpace(dto.NewPassword) ||
            string.IsNullOrWhiteSpace(dto.ConfirmPassword))
        {
            return BadRequest(new { message = "All password fields are required." });
        }

        if (dto.NewPassword != dto.ConfirmPassword)
        {
            return BadRequest(new { message = "New password and confirmation password do not match." });
        }

        if (dto.NewPassword.Length < 6)
        {
            return BadRequest(new { message = "New password must be at least 6 characters long." });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User record not found." });
        }

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.CurrentPassword
        );

        if (verification == PasswordVerificationResult.Failed)
        {
            return BadRequest(new { message = "Current password is incorrect." });
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password successfully changed for user {UserId}", user.Id);

        return Ok(new { message = "Password changed successfully." });
    }

    // POST: api/auth/forgot-password
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest(new { message = "Email address is required." });
        }

        var email = dto.Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user != null)
        {
            var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            try
            {
                var emailBody = $@"Hello {user.Name},

We received a request to reset your MediCare account password.
Your password reset token is:

{resetToken}

This token will expire in 1 hour. If you did not request this, please ignore this email.

Best regards,
The MediCare Health Team";

                await _emailService.SendEmailAsync(
                    user.Email,
                    "MediCare Password Reset Request",
                    emailBody
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            }
        }

        return Ok(new
        {
            message = "If your email is registered, you will receive password reset instructions shortly."
        });
    }

    // POST: api/auth/reset-password
    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Token) ||
            string.IsNullOrWhiteSpace(dto.NewPassword) ||
            string.IsNullOrWhiteSpace(dto.ConfirmPassword))
        {
            return BadRequest(new { message = "Email, token, and both password fields are required." });
        }

        if (dto.NewPassword != dto.ConfirmPassword)
        {
            return BadRequest(new { message = "New password and confirmation do not match." });
        }

        if (dto.NewPassword.Length < 6)
        {
            return BadRequest(new { message = "New password must be at least 6 characters long." });
        }

        var email = dto.Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == email &&
            u.PasswordResetToken == dto.Token &&
            u.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
        {
            return BadRequest(new { message = "Invalid or expired password reset token." });
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset successfully for user {UserId}", user.Id);

        return Ok(new
        {
            message = "Password has been reset successfully. You can now log in with your new password."
        });
    }
}
