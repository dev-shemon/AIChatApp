using AIChatApp.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIChatApp.Application.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password);

    // Verification method you already had
    bool VerifyPassword(string hashedPassword, string providedPassword);
}
