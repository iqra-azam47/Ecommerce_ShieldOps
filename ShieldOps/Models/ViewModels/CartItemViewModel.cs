namespace ShieldOps.Models.ViewModels
{
    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ProductType { get; set; } = string.Empty; // Physical or Digital
        public decimal TotalPrice => Price * Quantity;
    }
}