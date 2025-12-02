using AIChatApp.Application.DTOs;
using AIChatApp.Application.Interfaces;
using AIChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIChatApp.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        if (!ModelState.IsValid) return View(model);

        string baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _authService.RegisterUserAsync(model, baseUrl);

        if (result == "Success")
        {
            return RedirectToAction("RegisterSuccess");
        }

        ModelState.AddModelError("", result);
        return View(model);
    }

    [HttpGet]
    public IActionResult RegisterSuccess()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Verify(string token)
    {
        var isVerified = await _authService.VerifyEmailAsync(token);
        if (isVerified)
        {
            // Set Success message for the user after verification
            TempData["SuccessMessage"] = "Email successfully verified! You can now log in.";
            return View("VerifySuccess");
        }
        TempData["ErrorMessage"] = "Invalid verification link or token has expired.";
        return View("VerifyError");
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto model)
    {
        if (!ModelState.IsValid) return View(model);

        string result = await _authService.LoginUserAsync(model);

        if (result == "Invalid credentials." || result == "Email not verified. Please check your inbox.")
        {
            ModelState.AddModelError("", result);
            return View(model);
        }

        Guid userId = new Guid(result);

        // --- Create Session Claims (Simplified) ---
        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, model.Email),
                new Claim(ClaimTypes.Name, model.Email)
            };

        var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

        // 💡 ADDED: Set Success message
        TempData["SuccessMessage"] = "Login successful! Welcome back.";

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("CookieAuth");
        TempData["SuccessMessage"] = "You have been logged out successfully.";
        return RedirectToAction("Index", "Home");
    }
}