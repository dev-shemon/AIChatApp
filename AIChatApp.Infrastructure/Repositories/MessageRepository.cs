using AIChatApp.Domain.Entities;
using AIChatApp.Domain.Interfaces;
using AIChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIChatApp.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _context;

    public MessageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
    {
        message.SentAt = DateTime.UtcNow;
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        // Update conversation
        var conversation = await GetOrCreateConversationAsync(message.SenderId, message.ReceiverId);

        // --- FIX START: Handle Null MessageContent ---
        string previewText;

        if (!string.IsNullOrEmpty(message.MessageContent))
        {
            // If text exists, use it (truncated)
            previewText = message.MessageContent.Length > 50
                ? message.MessageContent.Substring(0, 50) + "..."
                : message.MessageContent;
        }
        else if (!string.IsNullOrEmpty(message.AttachmentUrl))
        {
            // If no text but has attachment, show generic text
            previewText = message.AttachmentType == "image" ? "📷 Sent a photo" : "📎 Sent a file";
        }
        else
        {
            // Fallback
            previewText = "New message";
        }

        conversation.LastMessage = previewText;
        // --- FIX END ---

        conversation.LastMessageAt = message.SentAt;
        await _context.SaveChangesAsync();

        return message;
    }

    public async Task<List<ChatMessage>> GetConversationMessagesAsync(
        Guid user1Id, Guid user2Id, int pageSize = 50, int page = 1)
    {
        return await _context.ChatMessages
            .Where(m => (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                       (m.SenderId == user2Id && m.ReceiverId == user1Id))
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .ToListAsync();
    }

    public async Task<ChatConversation> GetOrCreateConversationAsync(Guid user1Id, Guid user2Id)
    {
        var conversation = await _context.ChatConversations
            .FirstOrDefaultAsync(c =>
                (c.User1Id == user1Id && c.User2Id == user2Id) ||
                (c.User1Id == user2Id && c.User2Id == user1Id));

        if (conversation == null)
        {
            conversation = new ChatConversation
            {
                User1Id = user1Id,
                User2Id = user2Id,
                LastMessageAt = DateTime.UtcNow,
                LastMessage = "" // ADD THIS - set empty string instead of null
            };
            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();
        }

        return conversation;
    }

    public async Task<ChatMessage?> GetMessageByIdAsync(int messageId)
    {
        return await _context.ChatMessages
            .Include(m => m.Sender) // Include sender to map DTOs later if needed
            .FirstOrDefaultAsync(m => m.Id == messageId);
    }

    public async Task UpdateMessageAsync(ChatMessage message)
    {
        _context.ChatMessages.Update(message);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteMessageAsync(ChatMessage message)
    {
        _context.ChatMessages.Remove(message);
        await _context.SaveChangesAsync();

        // Note: If this message was the "LastMessage" in a Conversation, 
        // you might want to update the conversation logic here, 
        // but for now, we will keep it simple as per instructions.
    }

    public async Task MarkMessagesAsReadAsync(Guid senderId, Guid receiverId)
    {
        var unreadMessages = await _context.ChatMessages
            .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId && !m.IsRead)
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.ChatMessages
            .CountAsync(m => m.ReceiverId == userId && !m.IsRead);
    }
}