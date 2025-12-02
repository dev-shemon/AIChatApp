using AIChatApp.Application.DTOs;
using AIChatApp.Application.Interfaces;
using AIChatApp.Domain.Entities;
using AIChatApp.Domain.Interfaces;
using System.Security.Cryptography;
// Note: Removed the unused 'using BCrypt.Net.BCrypt;' dependency implicit in old code.

namespace AIChatApp.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher _passwordHasher; // Already correctly injected

    public AuthService(IUserRepository userRepository, IEmailService emailService, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
    }

    public async Task<string> RegisterUserAsync(RegisterDto request, string baseUrl)
    {
        // 1. Check if user exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null) return "Email already registered.";

        // 2. Hash Password (💡 FIXED: Use injected IPasswordHasher for consistency)
        string passwordHash = _passwordHasher.HashPassword(request.Password); // Assuming IPasswordHasher has a HashPassword method

        // 3. Create Verification Token
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

        // 4. Create Entity
        var user = new User
        {
            FullName = request.FullName,
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = passwordHash,
            VerificationToken = token,
            ProfileImageUrl = "default.png"
        };

        // 5. Save to DB
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // 6. Send Email
        string verifyLink = $"{baseUrl}/Auth/Verify?token={token}";
        await _emailService.SendVerificationEmailAsync(user.Email, verifyLink);

        return "Success";
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var user = await _userRepository.GetByTokenAsync(token);
        if (user == null) return false;

        user.VerifiedAt = DateTime.UtcNow;
        user.VerificationToken = null;

        await _userRepository.SaveChangesAsync();
        return true;
    }

    public async Task<string> LoginUserAsync(LoginDto request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        // 1. User Not Found
        if (user == null)
        {
            return "Invalid credentials.";
        }

        // 2. Password Verification (💡 FIXED: Use injected IPasswordHasher for consistency)
        bool isPasswordValid = _passwordHasher.VerifyPassword(user.PasswordHash, request.Password);

        if (!isPasswordValid)
        {
            return "Invalid credentials.";
        }

        // 3. Email Verification Check
        if (!user.IsVerified)
        {
            return "Email not verified. Please check your inbox.";
        }

        // 4. Success
        return user.Id.ToString();
    }

    // 5. Password Verification for Deletion (Logic remains correct)
    public async Task<bool> VerifyPasswordAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            return false;
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(user.PasswordHash, password);

        return isPasswordValid;
    }

    public async Task<string> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return "User not found.";
        }

        // 1. Verify Current Password
        if (!_passwordHasher.VerifyPassword(user.PasswordHash, dto.CurrentPassword))
        {
            return "Incorrect current password.";
        }

        // 2. Hash and Update New Password
        user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return "Success";
    }
}