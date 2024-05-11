using System.ComponentModel.DataAnnotations;

namespace SELENE_STUDIO.ViewModels;

public class MessageViewModel {
    public string Name { get; set; }
    public string Email { get; set; }
    public string Number { get; set; }
    [StringLength(300, ErrorMessage = "Message cannot exceed 300 characters")]
    public string Message { get; set; }
}