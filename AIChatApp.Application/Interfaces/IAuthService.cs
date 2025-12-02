using AIChatApp.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIChatApp.Application.Interfaces;

public interface IAuthService
{
    Task<string> RegisterUserAsync(RegisterDto request, string baseUrl);
    Task<bool> VerifyEmailAsync(string token);
    Task<string> LoginUserAsync(LoginDto request);
    Task<bool> VerifyPasswordAsync(string email, string password);
    Task<string> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
}
