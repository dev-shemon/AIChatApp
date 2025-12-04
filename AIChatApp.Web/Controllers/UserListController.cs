using AIChatApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIChatApp.Web.Controllers;

[Authorize] // Ensures only authenticated users can access
public class UserListController : Controller
{
    private readonly IUserListService _userListService;

    public UserListController(IUserListService userListService)
    {
        _userListService = userListService;
    }

    [HttpGet]
    public async Task<IActionResult> UsersList(string? search)
    {
        // 1. Get Current User ID safely
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdString, out Guid currentUserId))
        {
            // Fallback if ID is missing (shouldn't happen with [Authorize])
            return RedirectToAction("Login", "Auth");
        }

        // 2. If searching
        if (!string.IsNullOrWhiteSpace(search))
        {
            // Pass currentUserId
            var searchResults = await _userListService.SearchUsersAsync(search, currentUserId);

            ViewBag.SearchQuery = search;
            ViewBag.IsSearchMode = true;
            return View(new UserListViewModel { RecentUsers = searchResults });
        }

        // 3. If dashboard
        // Pass currentUserId
        var viewModel = await _userListService.GetDashboardAsync(currentUserId);

        ViewBag.IsSearchMode = false;
        return View(viewModel);
    }
}