using Microsoft.AspNetCore.Mvc;
using SELENE_STUDIO.Data;

namespace SELENE_STUDIO.Models
{
    public class Conversation
    {
        public int? ConversationId { get; set; }
        public string? InitiatorId { get; set; } // User who initiated the conversation
        public string? ReceiverId { get; set; } // User who received the initial message
        public bool IsActive { get; set; } // Indicates if the conversation is active
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public RegUser? Initiator { get; set; }
        public RegUser? Receiver { get; set; }
        public ICollection<Message>? Messages { get; set; }
    }
}
