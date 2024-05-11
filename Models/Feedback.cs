using Microsoft.AspNetCore.Mvc;
using SELENE_STUDIO.Data;
using System.ComponentModel.DataAnnotations;

namespace SELENE_STUDIO.Models
{
    public class Feedback
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string UserId { get; set; }
        public RegUser User { get; set; }

        [Required(ErrorMessage = "Please provide feedback.")]
        [StringLength(300, ErrorMessage = "Feedback must be less than 500 characters.")]
        public string Content { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        public DateTime Date { get; set; }

        public bool IsFeatured { get; set; }
    }

}
