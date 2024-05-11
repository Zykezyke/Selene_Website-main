using Microsoft.AspNetCore.Mvc;
using SELENE_STUDIO.Data;
using System.ComponentModel.DataAnnotations;

namespace SELENE_STUDIO.Models
{
    public class Message
    {
        public int MessageId { get; set; }
        [StringLength(300, ErrorMessage = "Message cannot exceed 300 characters")]
        public string? Content { get; set; }
        public string? SenderId { get; set; }
        public string? ReceiverId { get; set; }
        public int? ConversationId { get; set; }
        public string? ImageUrl { get; set; }

        public string? SenderName { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public RegUser? Sender { get; set; }
        public RegUser? Receiver { get; set; }
        public Conversation? Conversation { get; set; }

        public bool IsRead { get; set; }
    }
}
