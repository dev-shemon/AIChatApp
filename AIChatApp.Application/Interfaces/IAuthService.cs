using AIChatApp.Application.DTOs;

namespace AIChatApp.Application.Interfaces;

public interface IAuthService
{
    Task<string> RegisterUserAsync(RegisterDto request, string baseUrl);
    Task<bool> VerifyEmailAsync(string token);
    Task<string> LoginUserAsync(LoginDto request);
    Task<bool> VerifyPasswordAsync(string email, string password);
    Task<string> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task<string> ForgotPasswordAsync(ForgotPasswordDto request, string baseUrl);
    Task<string> ResetPasswordAsync(ResetPasswordDto request);
    Task<bool> ValidateResetTokenAsync(string email, string token);
}