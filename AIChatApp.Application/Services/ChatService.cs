using AIChatApp.Application.DTOs;
using AIChatApp.Application.Interfaces;
using AIChatApp.Domain.Entities;
using AIChatApp.Domain.Interfaces;

namespace AIChatApp.Application.Services;

public class ChatService : IChatService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;

    public ChatService(IMessageRepository messageRepository, IUserRepository userRepository, IFileStorageService fileStorageService)
    {
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<ChatDto> GetChatAsync(Guid currentUserId, Guid chatUserId)
    {
        var chatUser = await _userRepository.GetByIdAsync(chatUserId);
        if (chatUser == null)
            return null;

        var messages = await _messageRepository.GetConversationMessagesAsync(currentUserId, chatUserId);
        messages.Reverse(); // Oldest first

        await _messageRepository.MarkMessagesAsReadAsync(chatUserId, currentUserId);

        return new ChatDto
        {
            CurrentUserId = currentUserId,
            ChatUserId = chatUserId,
            ChatUserName = chatUser.UserName,
            ChatUserFullName = chatUser.FullName, // ADD THIS
            ChatUserProfileImage = chatUser.ProfileImageUrl, // ADD THIS
            Messages = messages.Select(m => new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender?.UserName,
                ReceiverId = m.ReceiverId,
                MessageContent = m.MessageContent,
                SentAt = m.SentAt,
                IsRead = m.IsRead,
                IsSentByCurrentUser = m.SenderId == currentUserId
            }).ToList()
        };
    }

    public async Task<MessageDto> SendMessageAsync(Guid senderId, SendMessageDto dto)
    {
        var message = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = dto.ReceiverId,
            MessageContent = dto.MessageContent,
            IsRead = false,
            SentAt = DateTime.UtcNow // Ensure time is set
        };

        // Handle File Upload
        if (dto.File != null && dto.File.Length > 0)
        {
            // Simple content type check
            string fileType = dto.File.ContentType.StartsWith("image") ? "image" : "file";

            // Upload
            string fileUrl = await _fileStorageService.UploadFileAsync(dto.File.OpenReadStream(), dto.File.FileName, dto.File.ContentType);

            message.AttachmentUrl = fileUrl;
            message.AttachmentType = fileType;
            message.OriginalFileName = dto.File.FileName;
        }

        var savedMessage = await _messageRepository.AddMessageAsync(message);
        var sender = await _userRepository.GetByIdAsync(senderId);

        return new MessageDto
        {
            Id = savedMessage.Id,
            SenderId = senderId,
            SenderName = sender?.UserName,
            ReceiverId = dto.ReceiverId,
            MessageContent = savedMessage.MessageContent,
            AttachmentUrl = savedMessage.AttachmentUrl,      // Map new props
            AttachmentType = savedMessage.AttachmentType,    // Map new props
            OriginalFileName = savedMessage.OriginalFileName,// Map new props
            SentAt = savedMessage.SentAt,
            IsRead = false,
            IsSentByCurrentUser = true
        };
    }

    public async Task<List<MessageDto>> GetMessagesAsync(Guid currentUserId, Guid chatUserId, int page = 1)
    {
        var messages = await _messageRepository.GetConversationMessagesAsync(currentUserId, chatUserId, 50, page);

        return messages.Select(m => new MessageDto
        {
            Id = m.Id,
            SenderId = m.SenderId,
            SenderName = m.Sender?.UserName,
            ReceiverId = m.ReceiverId,
            MessageContent = m.MessageContent,
            SentAt = m.SentAt,
            IsRead = m.IsRead,
            IsSentByCurrentUser = m.SenderId == currentUserId
        }).ToList();
    }

    // ... inside ChatService class ...

    public async Task<MessageDto> EditMessageAsync(Guid currentUserId, int messageId, string newContent)
    {
        var message = await _messageRepository.GetMessageByIdAsync(messageId);

        if (message == null)
            throw new Exception("Message not found");

        // Authorization Check: Only sender can edit
        if (message.SenderId != currentUserId)
            throw new UnauthorizedAccessException("You can only edit your own messages.");

        message.MessageContent = newContent;
        // Optional: Add an 'IsEdited' flag or 'EditedAt' timestamp to your Domain entity if you want to show "(edited)" in UI

        await _messageRepository.UpdateMessageAsync(message);

        // Return DTO to update UI
        return new MessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderName = message.Sender?.UserName,
            ReceiverId = message.ReceiverId,
            MessageContent = message.MessageContent,
            SentAt = message.SentAt,
            IsRead = message.IsRead,
            IsSentByCurrentUser = true
        };
    }

    public async Task<Guid> DeleteMessageAsync(Guid currentUserId, int messageId)
    {
        var message = await _messageRepository.GetMessageByIdAsync(messageId);

        if (message == null)
            throw new Exception("Message not found");

        // Authorization Check
        if (message.SenderId != currentUserId)
            throw new UnauthorizedAccessException("You can only delete your own messages.");

        var receiverId = message.ReceiverId; // Capture this before deleting

        await _messageRepository.DeleteMessageAsync(message);

        return receiverId; // Return receiver ID so SignalR can notify them
    }

    public async Task MarkMessagesAsReadAsync(Guid senderId, Guid receiverId)
    {
        await _messageRepository.MarkMessagesAsReadAsync(senderId, receiverId);
    }
}