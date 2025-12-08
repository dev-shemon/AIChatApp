using AIChatApp.Domain.Entities;

namespace AIChatApp.Domain.Interfaces;

public interface IMessageRepository
{
    Task<ChatMessage> AddMessageAsync(ChatMessage message);
    Task<List<ChatMessage>> GetConversationMessagesAsync(Guid user1Id, Guid user2Id, int pageSize = 50, int page = 1);
    Task<ChatConversation> GetOrCreateConversationAsync(Guid user1Id, Guid user2Id);
    Task MarkMessagesAsReadAsync(Guid senderId, Guid receiverId);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<ChatMessage?> GetMessageByIdAsync(int messageId);
    Task UpdateMessageAsync(ChatMessage message);
    Task DeleteMessageAsync(ChatMessage message);
}