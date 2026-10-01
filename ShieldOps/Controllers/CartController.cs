using Microsoft.AspNetCore.Http;
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
    public class CartController : Controller
    {
        private const string CART_SESSION_KEY = "ShieldOps_Session_Cart";
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        private List<CartItemViewModel> GetCartFromSession()
        {
            var sessionData = HttpContext.Session.GetString(CART_SESSION_KEY);
            return sessionData == null ? new List<CartItemViewModel>() : JsonSerializer.Deserialize<List<CartItemViewModel>>(sessionData)!;
        }

        private void SaveCartToSession(List<CartItemViewModel> cart)
        {
            HttpContext.Session.SetString(CART_SESSION_KEY, JsonSerializer.Serialize(cart));
        }

        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCartFromSession();
            return View(cart);
        }

        // DUAL VERIFICATION ROUTE: Supports both analytical links (GET) and custom submissions (POST)
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            // REAL SQLITE DATA QUERY: Pulling inventory data record from the db file directly
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Target defensive countermeasure asset does not exist in registry database.";
                return RedirectToAction("Index", "Catalog");
            }

            var cart = GetCartFromSession();
            var existingItem = cart.FirstOrDefault(i => i.ProductId == productId);

            int currentRequestedQuantity = (existingItem?.Quantity ?? 0) + quantity;

            // REAL WAREHOUSE VALIDATION GATE: Enforces actual dynamic constraints from db column
            if (product.Type == ProductType.Physical && currentRequestedQuantity > product.StockQuantity)
            {
                TempData["ErrorMessage"] = $"Allocation failed. Cannot request {currentRequestedQuantity} units of '{product.Name}'. Only {product.StockQuantity} remaining in infrastructure database stores.";
                return RedirectToAction("Index", "Catalog");
            }

            if (existingItem != null)
            {
                existingItem.Quantity = currentRequestedQuantity;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    ProductType = product.Type.ToString(),
                    Quantity = quantity
                });
            }

            SaveCartToSession(cart);
            TempData["SuccessMessage"] = $"'{product.Name}' allocated to secure session cart tracking log matrix.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCartFromSession();
            var targetItem = cart.FirstOrDefault(i => i.ProductId == productId);

            if (targetItem != null)
            {
                cart.Remove(targetItem);
                SaveCartToSession(cart);
                TempData["SuccessMessage"] = "Asset deallocated from secure session ledger tracks successfully.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int productId, string actionType)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item == null) return RedirectToAction("Index");

            // Fetch dynamic asset state limits natively
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound("Core product entity reference missing from context stores.");

            if (actionType == "increase")
            {
                // Dynamic threshold validation check over actual physical hardware stock capacity
                if (product.Type == ProductType.Physical && item.Quantity >= product.StockQuantity)
                {
                    TempData["ErrorMessage"] = $"Cannot exceed available warehouse logistics threshold limits ({product.StockQuantity} units) for '{product.Name}'.";
                    return RedirectToAction("Index");
                }
                item.Quantity++;
            }
            else if (actionType == "decrease")
            {
                item.Quantity--;
                if (item.Quantity <= 0)
                {
                    cart.Remove(item);
                    SaveCartToSession(cart);
                    TempData["SuccessMessage"] = $"'{product.Name}' completely dropped from active operations allocation logs.";
                    return RedirectToAction("Index");
                }
            }

            SaveCartToSession(cart);
            return RedirectToAction("Index");
        }
    }
}