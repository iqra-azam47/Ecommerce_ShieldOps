using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShieldOps.Data;
using ShieldOps.Models;
using ShieldOps.Models.ViewModels;

namespace ShieldOps.Controllers
{
    public class CatalogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Constructor injects both Database Context and Native UserManager tracking systems
        public CatalogController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // 1. GET: DEFENSIVE CATALOG WORKSPACE (PUBLIC)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(ProductFilterViewModel? filter = null)
        {
            if (filter == null)
            {
                filter = new ProductFilterViewModel();
            }

            // REAL SQLITE DATA QUERY: Read all seeded categories from dynamic database file
            var dbCategories = await _context.Categories.ToListAsync();

            // FIX: Computes the actual maximum price constraint ONLY from visible active items
            decimal highestPrice = await _context.Products.AnyAsync(p => p.IsActive)
                ? await _context.Products.Where(p => p.IsActive).MaxAsync(p => p.Price)
                : 500.00M;

            // FIX: Prepares an open relational Queryable matrix that pre-filters only active architectural deployment vectors
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive) // Intercepts and excludes hidden/inactive modules immediately
                .AsQueryable();

            // Dynamic search parsing over actual SQL query level text criteria
            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                string search = filter.SearchQuery.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(search) || p.Description.ToLower().Contains(search));
            }

            // Multiple categories filtration assignment tracking loop
            if (filter.SelectedCategories != null && filter.SelectedCategories.Any())
            {
                query = query.Where(p => filter.SelectedCategories.Contains(p.CategoryId));
            }

            // Filtering based on product hardware types enums criteria
            if (filter.SelectedType.HasValue)
            {
                query = query.Where(p => p.Type == filter.SelectedType.Value);
            }

            // Enforcing upper boundary threshold pricing limits parameters
            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);
            }

            // Hydrating the view model presentation layer with absolute real data lists
            filter.Products = await query.ToListAsync();
            filter.Categories = dbCategories;
            filter.AbsoluteMaxPrice = highestPrice;

            if (!filter.MaxPrice.HasValue)
            {
                filter.MaxPrice = highestPrice;
            }

            return View(filter);
        }

        // ==========================================
        // 2. GET: SPECIFICATIONS LEDGER DETAILS View
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // REAL RELATION QUERIES: Fetch true target physical product specs using primary key identity index
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Target defensive countermeasure asset missing from infrastructure database stores.";
                return RedirectToAction("Index");
            }

            // Fetches all matching historical user review rows assigned to this specific ProductId
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == id)
                .ToListAsync();

            var model = new ProductDetailsViewModel
            {
                Product = product,
                Reviews = reviews,
                IsEligibleToReview = User.Identity != null && User.Identity.IsAuthenticated, // Restored core session status gate check
                NewRating = 5,
                NewComment = string.Empty
            };

            ViewData["IsEditMode"] = false;
            return View(model);
        }

        // ==========================================
        // 3. POST: AUTHORIZE AND LOG FEEDBACK
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(ProductDetailsViewModel inputModel, int? productId)
        {
            // Gate check ensuring unauthorized telemetry drops are blocked from writing rows
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Challenge();
            }

            // Step 1: Extract tracking index identifier through context parameters
            int targetProductId = 0;

            if (productId.HasValue && productId.Value > 0)
            {
                targetProductId = productId.Value;
            }
            else if (inputModel?.Product != null && inputModel.Product.Id > 0)
            {
                targetProductId = inputModel.Product.Id;
            }
            else if (Request.Form.ContainsKey("productId"))
            {
                int.TryParse(Request.Form["productId"], out targetProductId);
            }
            else if (Request.Form.ContainsKey("Product.Id"))
            {
                int.TryParse(Request.Form["Product.Id"], out targetProductId);
            }

            // Verify if product exists inside SQLite rows
            bool productExists = await _context.Products.AnyAsync(p => p.Id == targetProductId);
            if (targetProductId <= 0 || !productExists)
            {
                TempData["ErrorMessage"] = "Payload validation failed: Relational key target sequence does not match any authenticated infrastructure nodes.";
                return RedirectToAction("Index");
            }

            // Step 2: Extract active login user identification hash string to satisfy database constraint
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                TempData["ErrorMessage"] = "Identity validation failed: Active profile signature could not be resolved.";
                return RedirectToAction("Details", new { id = targetProductId });
            }

            // =========================================================================
            // CRITICAL SECURITY GUARD: PREVENT DUPLICATE REVIEWS BY THE SAME PROFILE
            // =========================================================================
            bool hasAlreadyReviewed = await _context.Reviews.AnyAsync(r => r.ProductId == targetProductId && r.UserId == currentUserId);

            if (hasAlreadyReviewed)
            {
                TempData["ErrorMessage"] = "Evaluation Rejected: An active compliance review for this product node has already been logged by your profile security token.";
                return RedirectToAction("Details", new { id = targetProductId });
            }
            // =========================================================================

            if (!string.IsNullOrWhiteSpace(inputModel?.NewComment))
            {
                // REAL INSERTION: Population fields mapping precisely with your exact Review model specifications
                var review = new Review
                {
                    ProductId = targetProductId,
                    UserId = currentUserId, // Explicitly binds the user security index to avoid foreign key crash loops
                    Rating = inputModel.NewRating,
                    Comment = inputModel.NewComment.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync(); // SQL insert execution statement sync complete

                TempData["SuccessMessage"] = "Review matrix logs authorized and published inside database registry successfully.";
                return RedirectToAction("Details", new { id = targetProductId });
            }
            else
            {
                TempData["ErrorMessage"] = "Payload verification failed: Comment description payload notes require text parameters detail.";
                return RedirectToAction("Details", new { id = targetProductId });
            }
        }
    }
}