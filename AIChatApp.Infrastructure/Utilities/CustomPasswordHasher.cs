using AIChatApp.Application.Interfaces;
// You need to install the BCrypt.Net NuGet package for this code to work.

namespace AIChatApp.Infrastructure.Utilities; // Adjust namespace as needed

public class CustomPasswordHasher : IPasswordHasher
{
    // 💡 REQUIRED: Implements the hashing method for new passwords
    public string HashPassword(string password)
    {
        // Using BCrypt with a default work factor
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    // 💡 REQUIRED: Implements the verification method for existing passwords
    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        // BCrypt.Verify handles comparing the plain text password to the hash safely
        return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
    }
}