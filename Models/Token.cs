using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SELENE_STUDIO.Models
{
    public class Token
    {
        [Key]
        public int Id { get; set; } // Primary key

        public string? UserId { get; set; }
        public string? TokenValue { get; set; }
        public DateTimeOffset ExpirationTime { get; set; }
    }
}