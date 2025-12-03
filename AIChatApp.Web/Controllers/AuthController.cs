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
    public async Task<IActionResult> Login(LoginDto model, string returnUrl = null)
    {
        if (User.Identity.IsAuthenticated)
        {
            TempData["ToastType"] = "warning";
            TempData["ToastMessage"] = "You are currently signed in. Redirecting you to the homepage.";
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid) return View(model);

        string result = await _authService.LoginUserAsync(model);

        if (result == "Invalid credentials." || result == "Email not verified. Please check your inbox.")
        {
            ModelState.AddModelError("", result);
            return View(model);
        }

        Guid userId = new Guid(result);

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

        TempData["SuccessMessage"] = "Login successful! Welcome back.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
    {
        if (!ModelState.IsValid)
            return View(model);

        string baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _authService.ForgotPasswordAsync(model, baseUrl);

        // Always return success message for security (don't reveal if email exists)
        TempData["SuccessMessage"] = "If an account exists with that email, a password reset link has been sent.";
        return RedirectToAction("ForgotPasswordSuccess");
    }

    [HttpGet]
    public IActionResult ForgotPasswordSuccess()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string token, string email)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            TempData["ErrorMessage"] = "Invalid reset link.";
            return RedirectToAction("Login");
        }

        // Validate the token
        bool isValid = await _authService.ValidateResetTokenAsync(email, token);
        if (!isValid)
        {
            TempData["ErrorMessage"] = "Invalid or expired reset link.";
            return RedirectToAction("Login");
        }

        var model = new ResetPasswordDto
        {
            Token = token,
            Email = email
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.ResetPasswordAsync(model);

        if (result == "Success")
        {
            TempData["SuccessMessage"] = "Password successfully reset! You can now log in with your new password.";
            return RedirectToAction("ResetPasswordSuccess");
        }

        TempData["ErrorMessage"] = result;
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult ResetPasswordSuccess()
    {
        return View();
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