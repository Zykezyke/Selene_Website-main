using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SELENE_STUDIO.Models;

public class CartProduct
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int Quantity { get; set; }
    public string SelectedSize { get; set; }
    public decimal TotalPrice { get; set; } // Add TotalPrice property
    public string? CustomDescription { get; set; } // Property for custom description
    public string? CustomImagePath { get; set; }

}
