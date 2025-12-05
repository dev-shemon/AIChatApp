using AIChatApp.Application.DTOs;
using AIChatApp.Application.Interfaces;
using AIChatApp.Domain.Entities;
using AIChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIChatApp.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly IFileStorageService _fileStorageService;

    public ProfileController(IUserRepository userRepository, IAuthService authService, IFileStorageService fileStorageService)
    {
        _userRepository = userRepository;
        _authService = authService;
        _fileStorageService = fileStorageService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) throw new UnauthorizedAccessException("User ID not found in claims.");
        return new Guid(userIdClaim.Value);
    }

    private async Task ReSignInUser(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.UserName ?? user.Email)
        };

        var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignOutAsync("CookieAuth");
        await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);
    }

    [HttpGet]
    public async Task<IActionResult> ProfileDetails()
    {
        var user = await _userRepository.GetByIdAsync(GetCurrentUserId());
        if (user == null) return NotFound();

        var profileDto = new ProfileDetailsDto
        {
            FullName = user.FullName,
            UserName = user.UserName,
            Email = user.Email,
            RegisteredDate = user.CreatedAt,
            IsVerified = user.IsVerified,
            ProfileImageUrl = user.ProfileImageUrl
        };

        return View(profileDto);
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        Guid userId = GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFound();

        var model = new EditProfileDto
        {
            UserId = userId,
            FullName = user.FullName,
            UserName = user.UserName,
            CurrentEmail = user.Email
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditProfileDto model)
    {
        if (!ModelState.IsValid)
        {
            var user = await _userRepository.GetByIdAsync(model.UserId);
            if (user != null) model.CurrentEmail = user.Email;
            return View(model);
        }

        var userToUpdate = await _userRepository.GetByIdAsync(model.UserId);
        if (userToUpdate == null) return NotFound();

        var existingUser = await _userRepository.GetByUsernameAsync(model.UserName);
        if (existingUser != null && existingUser.Id != model.UserId)
        {
            ModelState.AddModelError(nameof(model.UserName), "This username is already taken.");
            model.CurrentEmail = userToUpdate.Email;
            return View(model);
        }

        userToUpdate.FullName = model.FullName;
        userToUpdate.UserName = model.UserName;

        await _userRepository.UpdateAsync(userToUpdate);
        await _userRepository.SaveChangesAsync();

        await ReSignInUser(userToUpdate);

        TempData["SuccessMessage"] = "Profile successfully updated!";
        return RedirectToAction("ProfileDetails");
    }

    [HttpGet]
    public IActionResult ConfirmDelete()
    {
        return View(new DeleteAccountDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(DeleteAccountDto model)
    {
        Guid userId = GetCurrentUserId();

        var userToDelete = await _userRepository.GetByIdAsync(userId);
        if (!ModelState.IsValid || userToDelete == null)
        {
            if (userToDelete == null) return NotFound();
            return View("ConfirmDelete", model);
        }

        bool passwordMatches = await _authService.VerifyPasswordAsync(userToDelete.Email, model.Password);

        if (!passwordMatches)
        {
            ModelState.AddModelError(nameof(model.Password), "Invalid password. Account deletion failed.");
            return View("ConfirmDelete", model);
        }

        await _userRepository.DeleteAsync(userToDelete);
        await _userRepository.SaveChangesAsync();

        await HttpContext.SignOutAsync("CookieAuth");

        TempData["SuccessMessage"] = "Your account has been successfully deleted.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        Guid userId = GetCurrentUserId();

        var result = await _authService.ChangePasswordAsync(userId, model);

        if (result == "Success")
        {
            TempData["SuccessMessage"] = "Your password has been successfully updated!";
            return RedirectToAction(nameof(ProfileDetails));
        }

        ModelState.AddModelError(nameof(model.CurrentPassword), result);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ChangeProfilePicture()
    {
        // 1. Get current user ID
        Guid userId = GetCurrentUserId();

        // 2. Fetch the user from database
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFound();

        // 3. Create the DTO and populate with current picture URL
        var model = new UpdateProfilePictureDto
        {
            UserId = userId,
            CurrentProfilePictureUrl = user.ProfileImageUrl // ✅ Pass the current picture URL
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeProfilePicture(UpdateProfilePictureDto model)
    {
        // 1. Validate DTO
        if (!ModelState.IsValid)
        {
            // Reload current picture URL on validation error
            var user = await _userRepository.GetByIdAsync(model.UserId);
            if (user != null)
            {
                model.CurrentProfilePictureUrl = user.ProfileImageUrl;
            }
            return View(model);
        }

        // 2. Get User
        var userToUpdate = await _userRepository.GetByIdAsync(GetCurrentUserId());
        if (userToUpdate == null || userToUpdate.Id != model.UserId)
        {
            return NotFound();
        }

        // 3. Handle File Upload
        try
        {
            // Store the old image URL before deleting
            string? oldImageUrl = userToUpdate.ProfileImageUrl;

            // For file uploads, use a unique name based on user ID
            string fileExtension = Path.GetExtension(model.ProfileImageFile.FileName);
            string fileName = $"{model.UserId}{fileExtension}";

            // Upload the new file and get the new URL
            using (var stream = model.ProfileImageFile.OpenReadStream())
            {
                var newImageUrl = await _fileStorageService.UploadFileAsync(
                    stream,
                    fileName,
                    model.ProfileImageFile.ContentType);

                // 4. Update User Entity with new image URL
                userToUpdate.ProfileImageUrl = newImageUrl;
            }

            // 5. Save Changes to Database FIRST (before deleting old image)
            await _userRepository.UpdateAsync(userToUpdate);
            await _userRepository.SaveChangesAsync();

            // 6. DELETE THE OLD IMAGE from storage (after DB update succeeds)
            // Only delete if an old image existed and it's different from the new one
            if (!string.IsNullOrEmpty(oldImageUrl) && oldImageUrl != userToUpdate.ProfileImageUrl)
            {
                try
                {
                    await _fileStorageService.DeleteFileAsync(oldImageUrl);
                }
                catch (Exception deleteEx)
                {
                    // Log the error but don't fail the operation
                    // The new image is already saved, so we can continue
                    System.Diagnostics.Debug.WriteLine($"Error deleting old image: {deleteEx.Message}");
                }
            }

            TempData["SuccessMessage"] = "Profile picture successfully updated!";
            return RedirectToAction("ProfileDetails");
        }
        catch (Exception ex)
        {
            // Log the exception
            ModelState.AddModelError(string.Empty, $"An error occurred during file upload: {ex.Message}");

            // Reload current picture URL on error
            var user = await _userRepository.GetByIdAsync(model.UserId);
            if (user != null)
            {
                model.CurrentProfilePictureUrl = user.ProfileImageUrl;
            }

            return View(model);
        }
    }
}