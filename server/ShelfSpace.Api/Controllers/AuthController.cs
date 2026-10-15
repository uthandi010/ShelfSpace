using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSpace.Api.Auth;
using ShelfSpace.Api.Data;
using ShelfSpace.Api.Dtos;
using ShelfSpace.Api.Models;

namespace ShelfSpace.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ShelfSpaceContext _context;
    private readonly TokenService _tokenService;

    public AuthController(ShelfSpaceContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    private const int MaxNameLength = 100;
    private const int MaxEmailLength = 254;
    private const int MaxPasswordLength = 128;

    private static bool IsValidEmail(string email)
    {
        if (email.Length > MaxEmailLength)
        {
            return false;
        }
        return System.Net.Mail.MailAddress.TryCreate(email, out var parsed) && parsed.Address == email;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Name, email, and password are all required." });
        }

        if (request.Password.Length < 8)
        {
            return BadRequest(new { message = "Password must be at least 8 characters." });
        }
        if (request.Password.Length > MaxPasswordLength)
        {
            return BadRequest(new { message = $"Password must be at most {MaxPasswordLength} characters." });
        }
        if (request.Name.Trim().Length > MaxNameLength)
        {
            return BadRequest(new { message = $"Name must be at most {MaxNameLength} characters." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (!IsValidEmail(normalizedEmail))
        {
            return BadRequest(new { message = "Email address is not valid." });
        }
        var exists = await _context.Users.AnyAsync(u => u.Email == normalizedEmail);
        if (exists)
        {
            return Conflict(new { message = "An account with that email already exists." });
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = PasswordHasher.Hash(request.Password),
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _tokenService.CreateToken(user);
        return Ok(new AuthResponse(token, user.Id, user.Name, user.Email));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password) ||
            request.Email.Length > MaxEmailLength || request.Password.Length > MaxPasswordLength)
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        if (PasswordHasher.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = PasswordHasher.Hash(request.Password);
            await _context.SaveChangesAsync();
        }

        var token = _tokenService.CreateToken(user);
        return Ok(new AuthResponse(token, user.Id, user.Name, user.Email));
    }
}
