using ShieldOps.Models;

namespace ShieldOps.Models.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = new Product();
        public List<Review> Reviews { get; set; } = new List<Review>();

        // Policy flags evaluated inside the controller
        public bool IsEligibleToReview { get; set; }

        // Input binding fields for the review submission form
        public int NewRating { get; set; }
        public string NewComment { get; set; } = string.Empty;
    }
}