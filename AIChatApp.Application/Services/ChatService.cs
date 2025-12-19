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
            ChatUserFullName = chatUser.FullName,
            ChatUserProfileImage = chatUser.ProfileImageUrl,
            Messages = messages.Select(m => new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender?.UserName,
                ReceiverId = m.ReceiverId,
                MessageContent = m.MessageContent,
                SentAt = m.SentAt,
                IsRead = m.IsRead,
                IsSentByCurrentUser = m.SenderId == currentUserId,

                // ✅ ADDED: Map Attachment details so they load in the history
                AttachmentUrl = m.AttachmentUrl,
                AttachmentType = m.AttachmentType,
                OriginalFileName = m.OriginalFileName
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
            SentAt = DateTime.UtcNow
        };

        // ✅ UPDATED: Cloudinary File Upload Logic
        if (dto.File != null && dto.File.Length > 0)
        {
            // 1. Determine File Type
            string contentType = dto.File.ContentType.ToLower();
            string fileType = "file"; // Default to generic file (pdf, doc, etc)

            if (contentType.StartsWith("image"))
                fileType = "image";
            else if (contentType.StartsWith("audio"))
                fileType = "audio";
            else if (contentType.StartsWith("video"))
                fileType = "video";

            // 2. Upload to Cloudinary
            // We pass the whole 'IFormFile' so the service can handle streams and headers
            string fileUrl = await _fileStorageService.SaveFileAsync(dto.File);

            // 3. Set Message Properties
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

            // Map new props so the UI updates immediately via SignalR
            AttachmentUrl = savedMessage.AttachmentUrl,
            AttachmentType = savedMessage.AttachmentType,
            OriginalFileName = savedMessage.OriginalFileName,

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
            IsSentByCurrentUser = m.SenderId == currentUserId,

            // ✅ ADDED: Map Attachment details for pagination/scrolling
            AttachmentUrl = m.AttachmentUrl,
            AttachmentType = m.AttachmentType,
            OriginalFileName = m.OriginalFileName
        }).ToList();
    }

    public async Task<MessageDto> EditMessageAsync(Guid currentUserId, int messageId, string newContent)
    {
        var message = await _messageRepository.GetMessageByIdAsync(messageId);

        if (message == null)
            throw new Exception("Message not found");

        // Authorization Check
        if (message.SenderId != currentUserId)
            throw new UnauthorizedAccessException("You can only edit your own messages.");

        message.MessageContent = newContent;

        await _messageRepository.UpdateMessageAsync(message);

        return new MessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderName = message.Sender?.UserName,
            ReceiverId = message.ReceiverId,
            MessageContent = message.MessageContent,
            SentAt = message.SentAt,
            IsRead = message.IsRead,
            IsSentByCurrentUser = true,

            // ADDED: Ensure attachments persist in UI after editing text
            AttachmentUrl = message.AttachmentUrl,
            AttachmentType = message.AttachmentType,
            OriginalFileName = message.OriginalFileName
        };
    }

    public async Task<Guid> DeleteMessageAsync(Guid currentUserId, int messageId)
    {
        var message = await _messageRepository.GetMessageByIdAsync(messageId);

        if (message == null)
            throw new Exception("Message not found");

        if (message.SenderId != currentUserId)
            throw new UnauthorizedAccessException("You can only delete your own messages.");

        var receiverId = message.ReceiverId;

        // Optional: If you want to delete the file from Cloudinary 
        // if(!string.IsNullOrEmpty(message.AttachmentUrl)) 
        //      await _fileStorageService.DeleteFileAsync(message.AttachmentUrl);

        await _messageRepository.DeleteMessageAsync(message);

        return receiverId;
    }

    public async Task MarkMessagesAsReadAsync(Guid senderId, Guid receiverId)
    {
        await _messageRepository.MarkMessagesAsReadAsync(senderId, receiverId);
    }
}