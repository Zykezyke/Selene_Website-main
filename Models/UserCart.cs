using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SELENE_STUDIO.Data;

namespace SELENE_STUDIO.Models;

public class UserCart {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id {get; set;}
    public RegUser Customer {get; set;}
    public List<CartProduct> Products {get; set;}
    public int TotalPrice {get; set;}
    
    public CartProduct this[CartProduct cartProduct] {
        get {
            return Products[Products.IndexOf(cartProduct)];
        }
        set {
            Products[Products.IndexOf(cartProduct)] = value;
        }
    }
}