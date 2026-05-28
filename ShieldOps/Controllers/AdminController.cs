using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShieldOps.Data;
using ShieldOps.Models;

namespace ShieldOps.Controllers
{
    [Authorize(Roles = "Admin")] // Strict Gatekeeper: Only accessible to seeded Administration Operators
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var products = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            ViewBag.Products = products;
            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            var categories = await _context.Categories.ToListAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product, IFormFile? productImageFile)
        {
            if (ModelState.IsValid)
            {
                if (productImageFile != null && productImageFile.Length > 0)
                {
                    string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    string localizedUniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(productImageFile.FileName);
                    string targetFilePath = Path.Combine(uploadFolder, localizedUniqueFileName);

                    using (var targetFileStream = new FileStream(targetFilePath, FileMode.Create))
                    {
                        await productImageFile.CopyToAsync(targetFileStream);
                    }

                    product.ImageUrl = "/images/products/" + localizedUniqueFileName;
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // FIXED ROUTING DIRECTION: Redirects explicitly to the inventory tracking tab fragment hash
                return RedirectToRoute(new { controller = "Admin", action = "Index", fragment = "products-panel" });
            }

            var categories = await _context.Categories.ToListAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "Name");
            return View(product);
        }

        // GET: Admin/EditProduct/5
        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound("Target configuration entity missing from core stockpile database.");

            var categories = await _context.Categories.ToListAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Admin/EditProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Product product, IFormFile? productImageFile)
        {
            if (ModelState.IsValid)
            {
                var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);
                if (existingProduct == null) return NotFound("Entity database synchronization reference broken.");

                if (productImageFile != null && productImageFile.Length > 0)
                {
                    string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    string localizedUniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(productImageFile.FileName);
                    string targetFilePath = Path.Combine(uploadFolder, localizedUniqueFileName);

                    using (var targetFileStream = new FileStream(targetFilePath, FileMode.Create))
                    {
                        await productImageFile.CopyToAsync(targetFileStream);
                    }
                    product.ImageUrl = "/images/products/" + localizedUniqueFileName;
                }
                else
                {
                    product.ImageUrl = existingProduct.ImageUrl;
                }

                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                // FIXED ROUTING DIRECTION: Anchor directly back into the warehouse CRUD ledger workspace
                return RedirectToRoute(new { controller = "Admin", action = "Index", fragment = "products-panel" });
            }

            var categories = await _context.Categories.ToListAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Admin/OrderDetails/5 (Comprehensive Diagnostic Audit Manifest Workspace View)
        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound("Requested order workspace trace unlocalized.");

            var assignedProductIds = order.OrderItems.Select(oi => oi.ProductId).ToList();
            var matchedReviews = await _context.Reviews
                .Where(r => assignedProductIds.Contains(r.ProductId) && r.UserId == order.UserId)
                .ToListAsync();

            ViewBag.Reviews = matchedReviews;
            return View(order);
        }

        // POST: Admin/UpdateStatusDropdown (Advanced Dropdown Milestone Selection Logic)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatusDropdown(int orderId, OrderStatus newStatus)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound("Target framework logistical node trace missing.");

            order.Status = newStatus;

            if (order.Status == OrderStatus.Delivered)
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null && item.Product.Type == ProductType.Digital && string.IsNullOrEmpty(item.GeneratedLicenseKey))
                    {
                        item.GeneratedLicenseKey = $"SOPS-{Guid.NewGuid().ToString().ToUpper().Substring(0, 18)}-LIC";
                        _context.OrderItems.Update(item);
                    }
                }
            }

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(OrderDetails), new { id = orderId });
        }

        // POST: Admin/ProcessReturn (Handles Secure Reversals Pipelines)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReturn(int orderId, bool approve)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound("Target order record not localized.");

            TimeSpan durationCalculated = DateTime.UtcNow - order.OrderDate;
            if (durationCalculated.TotalDays > 7)
            {
                TempData["ErrorMessage"] = "Strategic Security breach abort. Target return transaction violates the strict 7-day operational duration validation logic.";
                return RedirectToAction(nameof(OrderDetails), new { id = orderId });
            }

            if (approve)
            {
                foreach (var lineItem in order.OrderItems)
                {
                    var activeProduct = await _context.Products.FindAsync(lineItem.ProductId);
                    if (activeProduct != null && activeProduct.Type == ProductType.Physical)
                    {
                        activeProduct.StockQuantity += lineItem.Quantity;
                        _context.Products.Update(activeProduct);
                    }
                }

                order.ReturnState = ReturnStatus.Approved;
            }
            else
            {
                order.ReturnState = ReturnStatus.Rejected;
            }

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(OrderDetails), new { id = orderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProduct(int id)
        {
            var targetedItem = _context.Products.Find(id);
            if (targetedItem != null)
            {
                _context.Products.Remove(targetedItem);
                _context.SaveChanges();
            }

            // FIXED ROUTING DIRECTION: Redirect here too, so deleting items keeps you on the warehouse tab!
            return RedirectToRoute(new { controller = "Admin", action = "Index", fragment = "products-panel" });
        }
    }
}