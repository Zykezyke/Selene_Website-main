using System.ComponentModel.DataAnnotations;

namespace SELENE_STUDIO.Models;

public enum PaymentStatus {
    [Display(Name = "Pending")]
    Pending,
    
    [Display(Name = "Paid")]
    Paid,

    [Display(Name = "Unpaid")]
    Unpaid
}