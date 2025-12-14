using Microsoft.AspNetCore.Http;

namespace AIChatApp.Application.DTOs;

public class SendMessageDto
{
    public Guid ReceiverId { get; set; }
    public string MessageContent { get; set; }
    public IFormFile? File { get; set; }
}