using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShieldOps.Data;
using ShieldOps.Models;
using ShieldOps.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShieldOps.Controllers
{
    public class OrderController : Controller
    {
        private const string CART_SESSION_KEY = "ShieldOps_Session_Cart";
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Constructor injects both SQLite Database Context and Native Identity User Manager
        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private List<CartItemViewModel> GetCartFromSession()
        {
            var sessionData = HttpContext.Session.GetString(CART_SESSION_KEY);
            return sessionData == null ? new List<CartItemViewModel>() : JsonSerializer.Deserialize<List<CartItemViewModel>>(sessionData)!;
        }

        // ==========================================
        // 1. GET: CHECKOUT PROVISIONING VIEW
        // ==========================================
        [HttpGet]
        public IActionResult Checkout()
        {
            // Validates that the client security matrix contains an active authenticated user cookie
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            var cart = GetCartFromSession();
            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Cannot initialize checkout on an empty asset manifest.";
                return RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutViewModel
            {
                CartItems = cart,
                TotalAmount = cart.Sum(item => item.TotalPrice),
                RequiresShipping = cart.Any(item => item.ProductType == "Physical")
            };

            return View(model);
        }

        // ==========================================
        // 2. POST: PROCESS TRANSACTION CHECKOUT PIPELINE
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            var cart = GetCartFromSession();
            if (!cart.Any()) return RedirectToAction("Index", "Cart");

            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            // AUTOMATIC FULFILLMENT ROUTER: Evaluates if physical components exist inside the manifest
            bool containsPhysicalHardware = cart.Any(item => item.ProductType == "Physical");

            // Conditional workflow routing status assignment
            OrderStatus initialOrderStatus = containsPhysicalHardware ? OrderStatus.Ordered : OrderStatus.Delivered;

            // REAL SQLITE INSERTION TRACE PIPELINE
            var order = new Order
            {
                UserId = currentUserId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = cart.Sum(i => i.TotalPrice),
                Status = initialOrderStatus,
                ShippingAddress = containsPhysicalHardware ? model.ShippingAddress : "Digital Cryptographic Vault Node",
                City = containsPhysicalHardware ? model.City : "Digital Matrix Network",
                PostalCode = containsPhysicalHardware ? model.PostalCode : "00000"
            };

            // Saves Order Header Record into Database
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Loop parses through session cart entries and logs them into persistent order item rows
            foreach (var cartItem in cart)
            {
                string? generatedLicenseKey = null;

                // INSTANT GENERATION MECHANISM: Auto-inject activation pins immediately for stand-alone digital orders
                if (cartItem.ProductType == "Digital" && !containsPhysicalHardware)
                {
                    generatedLicenseKey = $"SHIELD-OPS-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}-{Guid.NewGuid().ToString().Substring(9, 4).ToUpper()}";
                }

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    PriceAtPurchase = cartItem.Price,
                    GeneratedLicenseKey = generatedLicenseKey
                };

                // REAL TIME STOCK DEDUCTION
                var product = await _context.Products.FindAsync(cartItem.ProductId);
                if (product != null && product.Type == ProductType.Physical)
                {
                    if (product.StockQuantity >= cartItem.Quantity)
                    {
                        product.StockQuantity -= cartItem.Quantity;
                        _context.Products.Update(product);
                    }
                }

                _context.OrderItems.Add(orderItem);
            }

            // Flushes order details rows into database storage layers
            await _context.SaveChangesAsync();

            // Purges secure session cache cart keys upon successful data ledger write operations
            HttpContext.Session.Remove(CART_SESSION_KEY);

            TempData["SuccessMessage"] = containsPhysicalHardware
                ? "Order authorized successfully. Logistics deployment timeline tracking initialized."
                : "Digital items checkout finalized. Cryptographic access activation license keys provisioned.";

            return RedirectToAction("Track", new { id = order.Id });
        }

        // ==========================================
        // 3. GET: TELEMETRY TRACK TIMELINE VIEW
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Track(int id)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            var currentUserId = _userManager.GetUserId(User);

            // Fetch order natively from SQLite storage file using relational table joins
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.Id == id && o.UserId == currentUserId)
                .FirstOrDefaultAsync();

            if (order == null) return NotFound("Requested dispatch configuration log was missing or unauthorized.");

            return View(order);
        }

        // ==========================================
        // 4. GET: CLIENT HISTORICAL DISPATCH TRANSACTIONS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            var currentUserId = _userManager.GetUserId(User);

            // REAL SQLITE DATA EXTRACTION: Queries live historical orders mapping to current identity key
            var userOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == currentUserId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(userOrders);
        }

        // ==========================================
        // 5. POST: INITIALIZE COMPLIANCE RETURN MANIFEST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestAssetReturn(int orderId, string returnReason)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(returnReason))
            {
                TempData["ErrorMessage"] = "Validation Failure: Rationale statement overview notes cannot be empty.";
                return RedirectToAction("Track", new { id = orderId });
            }

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound("Target dispatch tracking configuration node was missing from repository logs.");
            }

            // Transition asset tracker status fields to initialize review tracks
            order.ReturnState = ReturnStatus.Pending;
            order.ReturnReason = returnReason.Trim();

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Compliance return manifest payload successfully dispatched to administrative console.";
            return RedirectToAction("Track", new { id = orderId });
        }

        // ==========================================
        // 6. POST: ADMINISTRATIVE COMPLIANCE REVERSAL OPERATIONS
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReturn(int orderId, bool approve)
        {
            // Fallback structural role verification checking for active session admin descriptors
            var sessionEmail = HttpContext.Session.GetString("VerifiedUserEmail");
            var sessionRole = HttpContext.Session.GetString("VerifiedUserRole");
            bool isAuthorizedAdmin = !string.IsNullOrEmpty(sessionEmail) &&
                                     sessionEmail.Trim().ToLower() == "admin@shieldops.com" &&
                                     sessionRole == "Admin";

            if (!isAuthorizedAdmin)
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound("Target reverse manifest tracking reference sequence could not be resolved.");
            }

            // Update parameters based on approval choices matrix
            order.ReturnState = approve ? ReturnStatus.Approved : ReturnStatus.Rejected;

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = approve
                ? $"Reverse asset compliance loop for order #{orderId} has been successfully authorized."
                : $"Reverse asset compliance loop for order #{orderId} has been explicitly rejected.";

            return RedirectToAction("OrderDetails", "Admin", new { id = orderId });
        }
    }
}