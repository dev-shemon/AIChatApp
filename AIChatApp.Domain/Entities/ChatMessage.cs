using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIChatApp.Domain.Entities;

public class ChatMessage
{
    public int Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string MessageContent { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; } // e.g., "image", "pdf"
    public string? OriginalFileName { get; set; }

    // Navigation properties
    public virtual User Sender { get; set; }
    public virtual User Receiver { get; set; }
}