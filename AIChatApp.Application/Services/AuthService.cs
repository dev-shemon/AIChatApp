using AIChatApp.Application.DTOs;
using AIChatApp.Application.Interfaces;
using AIChatApp.Domain.Entities;
using AIChatApp.Domain.Interfaces;
using System.Security.Cryptography;

namespace AIChatApp.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUserRepository userRepository, IEmailService emailService, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
    }

    public async Task<string> RegisterUserAsync(RegisterDto request, string baseUrl)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null) return "Email already registered.";

        string passwordHash = _passwordHasher.HashPassword(request.Password);
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

        var user = new User
        {
            FullName = request.FullName,
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = passwordHash,
            VerificationToken = token,
            ProfileImageUrl = "default.png"
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

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

        if (user == null)
        {
            return "Invalid credentials.";
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(user.PasswordHash, request.Password);

        if (!isPasswordValid)
        {
            return "Invalid credentials.";
        }

        if (!user.IsVerified)
        {
            return "Email not verified. Please check your inbox.";
        }

        return user.Id.ToString();
    }

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

        if (!_passwordHasher.VerifyPassword(user.PasswordHash, dto.CurrentPassword))
        {
            return "Incorrect current password.";
        }

        user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return "Success";
    }

    public async Task<string> ForgotPasswordAsync(ForgotPasswordDto request, string baseUrl)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        // For security, don't reveal if email exists or not
        if (user == null)
        {
            return "Success";
        }

        // Generate reset token (valid for 1 hour)
        string resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
        user.PasswordResetToken = resetToken;
        user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        // Send email with reset link
        string resetLink = $"{baseUrl}/Auth/ResetPassword?token={resetToken}&email={Uri.EscapeDataString(user.Email)}";
        await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

        return "Success";
    }

    public async Task<bool> ValidateResetTokenAsync(string email, string token)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            return false;
        }

        // Check if token matches and hasn't expired
        if (user.PasswordResetToken != token || user.ResetTokenExpires == null || user.ResetTokenExpires < DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }

    public async Task<string> ResetPasswordAsync(ResetPasswordDto request)
    {
        // Validate the token first
        if (!await ValidateResetTokenAsync(request.Email, request.Token))
        {
            return "Invalid or expired reset link.";
        }

        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null)
        {
            return "User not found.";
        }

        // Hash and update password
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.ResetTokenExpires = null;

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return "Success";
    }
}