using System.ComponentModel.DataAnnotations;

namespace SELENE_STUDIO.Models;

public enum OrderStatus {
    [Display(Name = "Processing")]
    Processing,

    [Display(Name = "Accepted")]
    Accepted,

    [Display(Name = "Shipped")]
    Shipped,
    
    [Display(Name = "Completed")]
    Completed,
    
    [Display(Name = "Cancelled")]
    Cancelled,

    [Display(Name = "Pending")]
    Pending,
}