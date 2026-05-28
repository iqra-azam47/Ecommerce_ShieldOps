using ShieldOps.Models;

namespace ShieldOps.Models.ViewModels
{
    public class ProductFilterViewModel
    {
        // Inputs from user searching/filtering
        public string? SearchQuery { get; set; }
        public List<int> SelectedCategories { get; set; } = new List<int>();
        public ProductType? SelectedType { get; set; }
        public decimal? MaxPrice { get; set; }

        // Outputs populated by the database to build the front-end layout dynamically
        public List<Product> Products { get; set; } = new List<Product>();
        public List<Category> Categories { get; set; } = new List<Category>();
        public decimal AbsoluteMaxPrice { get; set; }
    }
}