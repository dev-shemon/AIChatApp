using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIChatApp.Domain.Entities;

public class ChatConversation
{
    public int Id { get; set; }
    public Guid User1Id { get; set; }
    public Guid User2Id { get; set; }
    public DateTime LastMessageAt { get; set; }
    public string? LastMessage { get; set; } = string.Empty;

    // Navigation properties
    public virtual User User1 { get; set; }
    public virtual User User2 { get; set; }
    public virtual ICollection<ChatMessage> Messages { get; set; }
}