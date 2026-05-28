using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public CatalogController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(ProductFilterViewModel? filter = null)
        {
            if (filter == null)
            {
                filter = new ProductFilterViewModel();
            }

            var allCategories = await _context.Categories.ToListAsync();
            decimal highestPrice = await _context.Products.AnyAsync()
                ? await _context.Products.MaxAsync(p => p.Price)
                : 500.00m;

            IQueryable<Product> query = _context.Products.Include(p => p.Category).Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                query = query.Where(p => p.Name.Contains(filter.SearchQuery) || p.Description.Contains(filter.SearchQuery));
            }

            if (filter.SelectedCategories != null && filter.SelectedCategories.Any())
            {
                query = query.Where(p => filter.SelectedCategories.Contains(p.CategoryId));
            }

            if (filter.SelectedType.HasValue)
            {
                query = query.Where(p => p.Type == filter.SelectedType.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);
            }

            filter.Products = await query.ToListAsync();
            filter.Categories = allCategories;
            filter.AbsoluteMaxPrice = highestPrice;

            if (!filter.MaxPrice.HasValue)
            {
                filter.MaxPrice = highestPrice;
            }

            return View(filter);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound("Target countermeasure asset missing.");

            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // VERIFICATION SECURITY CHALLENGE ENGINE
            bool verifiedBuyer = false;
            bool hasExistingReview = false;
            int existingRating = 5;
            string existingComment = string.Empty;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUserId = _context.Users
                    .FirstOrDefault(u => u.UserName == User.Identity.Name)?.Id;

                if (!string.IsNullOrEmpty(currentUserId))
                {
                    // Strict Validation Logic: Check if user has an order containing this item with status 'Delivered'
                    verifiedBuyer = await _context.Orders
                        .Include(o => o.OrderItems)
                        .AnyAsync(o => o.UserId == currentUserId &&
                                       o.Status == OrderStatus.Delivered &&
                                       o.OrderItems.Any(oi => oi.ProductId == id));

                    // FIXED NODE: Check database if this specific user has already logged a review for this product
                    var existingReview = reviews.FirstOrDefault(r => r.UserId == currentUserId);
                    if (existingReview != null)
                    {
                        hasExistingReview = true;
                        existingRating = existingReview.Rating;
                        existingComment = existingReview.Comment;
                    }
                }
            }

            var model = new ProductDetailsViewModel
            {
                Product = product,
                Reviews = reviews,
                IsEligibleToReview = verifiedBuyer,
                // Hydrate existing review fields down into the presentation layers if available
                NewRating = existingRating,
                NewComment = existingComment
            };

            // Injecting view model states flags to toggle Edit/Write layouts at UI level
            ViewData["IsEditMode"] = hasExistingReview;

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(ProductDetailsViewModel inputModel)
        {
            var currentUserId = _context.Users
                .FirstOrDefault(u => u.UserName == User.Identity.Name)?.Id;

            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            // Double security crosscheck validation on server side before inserting/updating DB
            bool processedCheck = await _context.Orders
                .Include(o => o.OrderItems)
                .AnyAsync(o => o.UserId == currentUserId &&
                               o.Status == OrderStatus.Delivered &&
                               o.OrderItems.Any(oi => oi.ProductId == inputModel.Product.Id));

            if (!processedCheck)
            {
                TempData["ErrorMessage"] = "Review submission rejected. Profile does not match verified delivered invoice tracks for this asset.";
                return RedirectToAction("Details", new { id = inputModel.Product.Id });
            }

            if (inputModel.NewRating < 1 || inputModel.NewRating > 5 || string.IsNullOrWhiteSpace(inputModel.NewComment))
            {
                TempData["ErrorMessage"] = "Invalid payload constraints. Reviews require ratings between 1-5 stars and descriptive text content.";
                return RedirectToAction("Details", new { id = inputModel.Product.Id });
            }

            // FIXED SMART UPSERT LOGIC: Blocks clone logs, updates rows variables naturally
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == inputModel.Product.Id && r.UserId == currentUserId);

            if (existingReview != null)
            {
                // Update Path (Overwrites pre-existing entry keys)
                existingReview.Rating = inputModel.NewRating;
                existingReview.Comment = inputModel.NewComment;
                existingReview.CreatedAt = DateTime.UtcNow; // Refreshes timestamp timeline logs

                _context.Reviews.Update(existingReview);
                TempData["SuccessMessage"] = "Your configuration log evaluation has been successfully updated.";
            }
            else
            {
                // Insert Path (Creates brand new structural entry row)
                var review = new Review
                {
                    ProductId = inputModel.Product.Id,
                    UserId = currentUserId,
                    Rating = inputModel.NewRating,
                    Comment = inputModel.NewComment,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reviews.Add(review);
                TempData["SuccessMessage"] = "Review matrix logs authorized and published successfully.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = inputModel.Product.Id });
        }
    } // <-- Brackets match and close perfectly here!
}