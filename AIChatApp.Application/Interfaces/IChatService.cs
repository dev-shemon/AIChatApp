using AIChatApp.Application.DTOs;

namespace AIChatApp.Application.Interfaces;

public interface IChatService
{
    Task<ChatDto> GetChatAsync(Guid currentUserId, Guid chatUserId);
    Task<MessageDto> SendMessageAsync(Guid senderId, SendMessageDto dto);
    Task<List<MessageDto>> GetMessagesAsync(Guid currentUserId, Guid chatUserId, int page = 1);
    Task MarkMessagesAsReadAsync(Guid senderId, Guid receiverId);
    Task<MessageDto> EditMessageAsync(Guid currentUserId, int messageId, string newContent);
    Task<Guid> DeleteMessageAsync(Guid currentUserId, int messageId);
}