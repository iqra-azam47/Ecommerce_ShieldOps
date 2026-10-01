using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShieldOps.Data;
using ShieldOps.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShieldOps.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // HYBRID SECURE GATEWAY: Cross-verifies native Identity tokens and custom fallback session tags for live server stability
        private bool IsAuthorizedAdmin()
        {
            var sessionEmail = HttpContext.Session.GetString("VerifiedUserEmail");
            var sessionRole = HttpContext.Session.GetString("VerifiedUserRole");

            bool isSessionValidAdmin = !string.IsNullOrEmpty(sessionEmail) &&
                                       sessionEmail.Trim().ToLower() == "admin@shieldops.com" &&
                                       sessionRole == "Admin";

            bool isNativeIdentityAdmin = User.Identity != null &&
                                         User.Identity.IsAuthenticated &&
                                         User.Identity.Name?.ToLower() == "admin@shieldops.com";

            return isSessionValidAdmin || isNativeIdentityAdmin;
        }

        // ==========================================
        // 1. GET: ADMIN DASHBOARD / ORDER PIPELINE
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsAuthorizedAdmin()) return RedirectToAction("Login", "Account");

            // REAL SQLITE DATA STREAM: Pulling all registered dynamic order pipelines from live database file
            var liveOrders = await _context.Orders
                .Include(o => o.User)           // Fetch identity owner records
                .Include(o => o.OrderItems)     // Fetch mapping cart tracks manifest
                .ThenInclude(oi => oi.Product)  // Bind core products properties catalog
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Loading system warehouse products seamlessly to render warehouse CRUD panel metrics
            ViewBag.Products = await _context.Products.Include(p => p.Category).ToListAsync();

            return View(liveOrders);
        }

        // ==========================================
        // 2. GET: AUDIT ORDER DETAILS (Fixes 404 on Audit Click)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            if (!IsAuthorizedAdmin()) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["ErrorMessage"] = $"Order tracking node #{id} could not be resolved in system logs.";
                return RedirectToAction("Index");
            }

            return View(order);
        }

        // ==========================================
        // 3. POST: UPDATE ORDER DISPATCH STATUS DROPDOWN
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatusDropdown(int orderId, OrderStatus newStatus)
        {
            if (!IsAuthorizedAdmin()) return RedirectToAction("Login", "Account");

            // Pull the order explicitly along with its item parameters to inspect mixed components matrix
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order != null)
            {
                order.Status = newStatus;

                // AUTOMATIC HYBRID FULFILLMENT PROVISIONING CHECKPOINT
                // If a mixed order state changes to Delivered, generate cryptographic security keys for any digital assets
                if (newStatus == OrderStatus.Delivered)
                {
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product != null && item.Product.Type == ProductType.Digital && string.IsNullOrEmpty(item.GeneratedLicenseKey))
                        {
                            item.GeneratedLicenseKey = $"SHIELD-OPS-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}-{Guid.NewGuid().ToString().Substring(9, 4).ToUpper()}";
                            _context.OrderItems.Update(item);
                        }
                    }
                }

                _context.Orders.Update(order);
                await _context.SaveChangesAsync(); // Dynamic state synchronization completed!

                TempData["SuccessMessage"] = $"Order #{orderId} infrastructure status successfully updated to {newStatus} in SQLite storage ledger.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Target dispatch tracking node #{orderId} not found in repository records.";
            }

            return RedirectToAction("Index");
        }

        // ==========================================
        // 4. GET: LOAD EDIT PRODUCT INTERFACE (Fixes 404 on Edit Click)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            if (!IsAuthorizedAdmin()) return RedirectToAction("Login", "Account");

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Target countermeasure asset could not be found in the database layer.";
                return RedirectToAction("Index");
            }

            var categories = await _context.Categories.ToListAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "Name", product.CategoryId);

            return View(product);
        }

        // ==========================================
        // 5. POST: SAVE UPDATED PRODUCT CHANGES
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Product product)
        {
            if (!IsAuthorizedAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Products.Update(product);
                    await _context.SaveChangesAsync(); // SQLite file data sync complete!

                    TempData["SuccessMessage"] = $"Asset configuration for '{product.Name}' has been successfully modified.";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(p => p.Id == product.Id)) return NotFound();
                    else throw;
                }
            }

            var categories = await _context.Categories.ToListAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // ==========================================
        // 6. POST: WREAK (DELETE) PRODUCT FROM DATABASE
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!IsAuthorizedAdmin()) return RedirectToAction("Login", "Account");

            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Strategic asset '{product.Name}' has been permanently decommissioned from the inventory ledger.";
            }
            else
            {
                TempData["ErrorMessage"] = "Target asset deletion failed: Element reference missing from registry logs.";
            }

            return RedirectToAction("Index");
        }

        // ==========================================
        // 7. GET: RENDER CREATE COUNTERMEASURE FORM
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            if (!IsAuthorizedAdmin()) return RedirectToAction("Login", "Account");

            var trueCategories = await _context.Categories.ToListAsync();
            ViewBag.CategoryList = new SelectList(trueCategories, "Id", "Name");

            return View();
        }

        // ==========================================
        // 8. POST: EXECUTE CREATE COUNTERMEASURE ASSET
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product)
        {
            if (!IsAuthorizedAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"New defensive countermeasure '{product.Name}' successfully provisioned inside system architecture ledger.";
                return RedirectToAction("Index");
            }

            var trueCategories = await _context.Categories.ToListAsync();
            ViewBag.CategoryList = new SelectList(trueCategories, "Id", "Name");
            return View(product);
        }
    }
}