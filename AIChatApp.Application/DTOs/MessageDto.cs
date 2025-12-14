namespace AIChatApp.Application.DTOs;

public class MessageDto
{
    public int Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; }
    public Guid ReceiverId { get; set; }
    public string MessageContent { get; set; }
    public string AttachmentUrl { get; set; }
    public string AttachmentType { get; set; }
    public string OriginalFileName { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsSentByCurrentUser { get; set; }
}