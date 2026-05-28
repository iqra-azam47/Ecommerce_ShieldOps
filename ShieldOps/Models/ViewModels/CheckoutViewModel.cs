using System.ComponentModel.DataAnnotations;

namespace ShieldOps.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();

        public decimal TotalAmount { get; set; }

        public bool RequiresShipping { get; set; }

        // Shipping Fields (Validated conditionally in the Controller backend logic)
        [StringLength(200)]
        public string? ShippingAddress { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }
    }
}