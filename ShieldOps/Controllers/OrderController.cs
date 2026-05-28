using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ShieldOps.Data;
using ShieldOps.Models;
using ShieldOps.Models.ViewModels;

namespace ShieldOps.Controllers
{
    [Authorize] // Enforces that a user must be logged in to checkout and manage orders
    public class OrderController : Controller
    {
        private const string CART_SESSION_KEY = "ShieldOps_Session_Cart";
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

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

        [HttpGet]
        public IActionResult Checkout()
        {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
        {
            var cart = GetCartFromSession();
            if (!cart.Any())
            {
                ModelState.AddModelError(string.Empty, "Your session cart has expired or is unallocated.");
                return RedirectToAction("Index", "Cart");
            }

            bool checkRequiresShipping = cart.Any(item => item.ProductType == "Physical");

            // CONDITIONAL BACKEND VALIDATION SECURITY GATE
            if (checkRequiresShipping)
            {
                if (string.IsNullOrWhiteSpace(model.ShippingAddress))
                    ModelState.AddModelError(nameof(model.ShippingAddress), "Logistical Shipping Address is required for physical hardware assets.");
                if (string.IsNullOrWhiteSpace(model.City))
                    ModelState.AddModelError(nameof(model.City), "Shipping City parameter is required.");
                if (string.IsNullOrWhiteSpace(model.PostalCode))
                    ModelState.AddModelError(nameof(model.PostalCode), "Postal Security Code is required.");
            }

            if (!ModelState.IsValid)
            {
                model.CartItems = cart;
                model.TotalAmount = cart.Sum(i => i.TotalPrice);
                model.RequiresShipping = checkRequiresShipping;
                return View("Checkout", model);
            }

            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            // Isolated Database Transaction logic block
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // FIXED OPTION 1 MATRIX: Check if the transaction contains NO physical hardware dependencies
                bool isPureDigitalOrder = !checkRequiresShipping;

                var order = new Order
                {
                    UserId = currentUserId,
                    OrderDate = DateTime.UtcNow, // Matches column binding naming constraints precisely
                    TotalAmount = cart.Sum(i => i.TotalPrice),

                    // FIXED: Skips logistical queues instantly for software assets, locks straight into Delivered state!
                    Status = isPureDigitalOrder ? OrderStatus.Delivered : OrderStatus.Ordered,

                    ShippingAddress = checkRequiresShipping ? model.ShippingAddress : "Cloud Cryptographic Vault Node",
                    City = checkRequiresShipping ? model.City : "Digital Matrix Network",
                    PostalCode = checkRequiresShipping ? model.PostalCode : "00000"
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var cartItem in cart)
                {
                    var product = await _context.Products.FindAsync(cartItem.ProductId);
                    if (product == null) throw new Exception($"Asset ID {cartItem.ProductId} missing from DB context store.");

                    string? generatedKey = null;

                    if (product.Type == ProductType.Physical)
                    {
                        // Deduct physical inventory stocks dynamically
                        if (product.StockQuantity < cartItem.Quantity)
                        {
                            throw new Exception($"Stock exhaustion error for '{product.Name}'.");
                        }
                        product.StockQuantity -= cartItem.Quantity;
                        _context.Products.Update(product);
                    }
                    else if (product.Type == ProductType.Digital)
                    {
                        // FIXED HYBRID CART GATEWAY: 
                        // Token key will ONLY generate instantly if the whole cart is purely digital software (Instant payment clearance).
                        // For hybrid/hardware orders (COD), it stays null until the package is securely delivered!
                        if (isPureDigitalOrder)
                        {
                            generatedKey = $"SOPS-{Guid.NewGuid().ToString().ToUpper().Substring(0, 18)}-LIC";
                        }
                    }

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = product.Id,
                        Quantity = cartItem.Quantity,
                        PriceAtPurchase = product.Price,
                        GeneratedLicenseKey = generatedKey // Remains null for pending COD orders
                    };

                    _context.OrderItems.Add(orderItem);
                }

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                // Clear temporary session cart tracking data on checkout completion success
                HttpContext.Session.Remove(CART_SESSION_KEY);

                // Setup clear notification system alerts messages
                if (isPureDigitalOrder)
                {
                    TempData["SuccessMessage"] = "Digital deployment authorized! Payment captured and license keys activated instantly.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Order successfully routed via Cash on Delivery logistics pipelines. Digital licenses will unlock automatically upon courier settlement verification.";
                }

                return RedirectToAction("Track", new { id = order.Id });
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, $"System Transaction Failure: {ex.Message}");
                model.CartItems = cart;
                model.TotalAmount = cart.Sum(i => i.TotalPrice);
                model.RequiresShipping = checkRequiresShipping;
                return View("Checkout", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Track(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.Id == id && o.UserId == currentUserId)
                .FirstOrDefaultAsync();

            if (order == null) return NotFound("Requested order record was not found or unauthorized.");

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            // Fetch all orders belonging strictly to this user query matrix sorting by OrderDate
            var userOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == currentUserId)
                .OrderByDescending(o => o.OrderDate) // Matched perfectly to avoid multi-mapping exceptions
                .ToListAsync();

            return View(userOrders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestAssetReturn(int orderId, string returnReason)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == currentUserId);

            if (order == null) return NotFound("Target architecture dispatch unit tracking missing.");

            // Verification boundary gates constraints check
            if (order.Status != OrderStatus.Delivered)
            {
                TempData["ErrorMessage"] = "Reversal operation unauthorized. Reversals evaluate once system nodes click completely to delivered status.";
                return RedirectToAction("MyOrders");
            }

            if (string.IsNullOrWhiteSpace(returnReason))
            {
                TempData["ErrorMessage"] = "Validation mismatch. Justification description notes require character payload details.";
                return RedirectToAction("MyOrders");
            }

            // Bind transaction metadata values properties
            order.ReturnState = ReturnStatus.Pending;
            order.ReturnReason = returnReason;
            order.ReturnRequestedAt = DateTime.UtcNow;

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reverse pipeline return log payload successfully dispatched. Awaiting central administrator verification checks.";
            return RedirectToAction("MyOrders");
        }
    }
}