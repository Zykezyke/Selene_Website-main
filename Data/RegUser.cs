using Microsoft.AspNetCore.Identity;
using SELENE_STUDIO.Models;
namespace SELENE_STUDIO.Data
{
    public class RegUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Role { get; set; }
        
        public string? Address { get; set; }
        public DateTime? SessionStartTime { get; set; }
        public DateTime? SessionEndTime { get; set; }
        public string? _Faker_Password { get; set; }

        // Navigation property for messages sent by the user
        public virtual ICollection<Message>? SentMessages { get; set; }

        // Navigation property for messages received by the user
        public virtual ICollection<Message>? ReceivedMessages { get; set; }
    }
}
