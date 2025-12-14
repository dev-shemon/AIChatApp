using AIChatApp.Application.DTOs;
using AIChatApp.Application.Interfaces;
using AIChatApp.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AIChatApp.Web.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly IChatService _chatService;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(IChatService chatService, IHubContext<ChatHub> hubContext)
    {
        _chatService = chatService;
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromForm] SendMessageDto dto)
    {
        try
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdClaim)) return Unauthorized();

            var currentUserId = Guid.Parse(currentUserIdClaim);

            // 1. Save Message & File via Service
            var savedMessage = await _chatService.SendMessageAsync(currentUserId, dto);

            // 2. Broadcast to Receiver via SignalR
            await _hubContext.Clients.User(dto.ReceiverId.ToString()).SendAsync("ReceiveMessage", new
            {
                id = savedMessage.Id,
                senderId = savedMessage.SenderId.ToString(),
                senderName = savedMessage.SenderName,
                message = savedMessage.MessageContent,
                attachmentUrl = savedMessage.AttachmentUrl,
                attachmentType = savedMessage.AttachmentType,
                originalFileName = savedMessage.OriginalFileName,
                sentAt = savedMessage.SentAt.ToString("o")
            });

            // 3. Return result to Sender (so JS can update UI with final ID/URL)
            return Ok(savedMessage);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public async Task<IActionResult> Index(Guid userId)
    {
        if (userId == Guid.Empty)
            return RedirectToAction("Index", "User");

        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var currentUserId = Guid.Parse(currentUserIdClaim);
        var chatDto = await _chatService.GetChatAsync(currentUserId, userId);

        if (chatDto == null)
            return NotFound();

        return View(chatDto);
    }

    [HttpGet]
    public async Task<IActionResult> GetMessages(Guid userId, int page = 1)
    {
        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var currentUserId = Guid.Parse(currentUserIdClaim);
        var messages = await _chatService.GetMessagesAsync(currentUserId, userId, page);

        return Json(messages);
    }
}