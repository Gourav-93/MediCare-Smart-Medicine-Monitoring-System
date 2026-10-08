using MediCare.Data;
using MediCare.DTOs;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthController(
        AppDbContext context,
        IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = new PasswordHasher<User>();
    }

    // POST: api/Auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password) ||
            string.IsNullOrWhiteSpace(dto.Role))
        {
            return BadRequest(
                "Name, Email, Password and Role are required."
            );
        }

        if (dto.Password.Length < 6)
        {
            return BadRequest(
                "Password must be at least 6 characters long."
            );
        }

        var email = dto.Email.Trim().ToLower();

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == email);

        if (existingUser != null)
        {
            return BadRequest("Email already exists.");
        }

        var role = dto.Role.Trim().ToUpper();

        // ADMIN cannot register publicly
        if (role != "PATIENT" && role != "CAREGIVER")
        {
            return BadRequest(
                "Only PATIENT or CAREGIVER registration is allowed."
            );
        }

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = email,
            Role = role
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(
                user,
                dto.Password
            );

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "User registered successfully.",
            userId = user.Id,
            role = user.Role
        });
    }

    // POST: api/Auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(
                "Email and Password are required."
            );
        }

        var email = dto.Email.Trim().ToLower();

        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == email);

        if (user == null)
        {
            return Unauthorized(
                "Invalid email or password."
            );
        }

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.Password
        );

        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(
                "Invalid email or password."
            );
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
}