using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SELENE_STUDIO.Data;

namespace SELENE_STUDIO.Models;

public class Order {
    [Key]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int OrderNumber {get; set;}
    
    public DateTime Date {get; set;}
    public RegUser Customer {get; set;}
    public string DeliveryAddress {get; set;}
    public string PaymentMethod {get; set;}
    public string DeliveryMode { get; set;}

    public PaymentStatus? OldPaymentStatus { get; set;}

    public OrderStatus? OldOrderStatus { get; set;}
    public PaymentStatus PaymentStatus {get; set;}
    public OrderStatus OrderStatus {get; set;}
    public List<CartProduct> Products {get; set;}
    public decimal TotalPrice {get; set;}

    public string? UploadedImagePath { get; set; }

    public bool IsDeleted { get; set;}

}