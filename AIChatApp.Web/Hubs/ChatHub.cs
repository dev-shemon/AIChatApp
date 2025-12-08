using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using AIChatApp.Application.Interfaces;
using AIChatApp.Application.DTOs;
using System.Security.Claims;

namespace AIChatApp.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task SendMessage(string receiverId, string message)
    {
        try
        {
            _logger.LogInformation("SendMessage called. ReceiverId: {ReceiverId}, Message: {Message}", receiverId, message);

            // Get sender ID from claims
            var senderIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(senderIdClaim))
            {
                _logger.LogError("Sender ID not found in claims");
                throw new HubException("User not authenticated");
            }

            _logger.LogInformation("Sender ID: {SenderId}", senderIdClaim);

            var senderId = Guid.Parse(senderIdClaim);
            var receiverGuid = Guid.Parse(receiverId);

            var dto = new SendMessageDto
            {
                ReceiverId = receiverGuid,
                MessageContent = message
            };

            _logger.LogInformation("Calling ChatService.SendMessageAsync");
            var savedMessage = await _chatService.SendMessageAsync(senderId, dto);
            _logger.LogInformation("Message saved with ID: {MessageId}", savedMessage.Id);

            // Send to receiver
            _logger.LogInformation("Sending to receiver: {ReceiverId}", receiverId);
            await Clients.User(receiverId).SendAsync("ReceiveMessage", new
            {
                id = savedMessage.Id,
                senderId = savedMessage.SenderId.ToString(),
                senderName = savedMessage.SenderName,
                message = savedMessage.MessageContent,
                sentAt = savedMessage.SentAt.ToString("o")
            });

            // Confirm to sender
            _logger.LogInformation("Confirming to sender");
            await Clients.Caller.SendAsync("MessageSent", new
            {
                id = savedMessage.Id,
                receiverId = receiverId,
                message = savedMessage.MessageContent,
                sentAt = savedMessage.SentAt.ToString("o")
            });

            _logger.LogInformation("SendMessage completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendMessage: {ErrorMessage}", ex.Message);

            // Send detailed error to client for debugging
            await Clients.Caller.SendAsync("Error", new
            {
                message = ex.Message,
                stackTrace = ex.StackTrace
            });

            throw new HubException($"Failed to send message: {ex.Message}", ex);
        }
    }

    // ... inside ChatHub class ...

    public async Task EditMessage(int messageId, string newContent)
    {
        try
        {
            var senderIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(senderIdClaim)) return;
            var senderId = Guid.Parse(senderIdClaim);

            // 1. Call Service to persist change
            var updatedMessage = await _chatService.EditMessageAsync(senderId, messageId, newContent);

            // 2. Notify Receiver (Real-time update)
            await Clients.User(updatedMessage.ReceiverId.ToString())
                .SendAsync("MessageEdited", messageId, newContent);

            // 3. Notify Sender (Update their own UI)
            await Clients.Caller.SendAsync("MessageEdited", messageId, newContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing message");
            // Optionally send error to caller
        }
    }

    public async Task DeleteMessage(int messageId)
    {
        try
        {
            var senderIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(senderIdClaim)) return;
            var senderId = Guid.Parse(senderIdClaim);

            // 1. Call Service to delete
            var receiverId = await _chatService.DeleteMessageAsync(senderId, messageId);

            // 2. Notify Receiver
            await Clients.User(receiverId.ToString()).SendAsync("MessageDeleted", messageId);

            // 3. Notify Sender
            await Clients.Caller.SendAsync("MessageDeleted", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message");
        }
    }

    public async Task MarkAsRead(string senderId)
    {
        try
        {
            var receiverIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(receiverIdClaim))
            {
                _logger.LogWarning("Receiver ID not found in MarkAsRead");
                return;
            }

            var receiverId = Guid.Parse(receiverIdClaim);
            await _chatService.MarkMessagesAsReadAsync(Guid.Parse(senderId), receiverId);

            await Clients.User(senderId).SendAsync("MessagesRead", receiverId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MarkAsRead");
        }
    }

    public async Task UserTyping(string receiverId)
    {
        try
        {
            var senderIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(senderIdClaim))
            {
                await Clients.User(receiverId).SendAsync("UserTyping", senderIdClaim);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UserTyping");
        }
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User connected: {UserId}, ConnectionId: {ConnectionId}", userIdClaim, Context.ConnectionId);

            if (!string.IsNullOrEmpty(userIdClaim))
            {
                await Clients.Others.SendAsync("UserOnline", userIdClaim);
            }
            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnConnectedAsync");
        }
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        try
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User disconnected: {UserId}, ConnectionId: {ConnectionId}", userIdClaim, Context.ConnectionId);

            if (!string.IsNullOrEmpty(userIdClaim))
            {
                await Clients.Others.SendAsync("UserOffline", userIdClaim);
            }
            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnDisconnectedAsync");
        }
    }
}