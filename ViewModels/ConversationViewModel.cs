using Microsoft.AspNetCore.Mvc;
using SELENE_STUDIO.Models;

namespace SELENE_STUDIO.ViewModels
{
    public class ConversationViewModel
    {
        public Conversation Conversation { get; set; }
        public List<Message>? Messages { get; set; }
    }
}
