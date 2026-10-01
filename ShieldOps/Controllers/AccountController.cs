using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShieldOps.Models;
using ShieldOps.Models.ViewModels;
using System;
using System.Threading.Tasks;

namespace ShieldOps.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        // Constructor handles native Identity Services coupled with SQLite context automatically
        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                string cleanEmail = model.Email.Trim().ToLower();

                var user = new ApplicationUser
                {
                    UserName = model.Email.Trim(),
                    Email = model.Email.Trim(),
                    FullName = model.FullName,
                    IsVerified = true // Auto verification override context flag
                };

                // REAL SQLITE INSERTION: Generates cryptographically secure password hash inside database file
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (cleanEmail == "admin@shieldops.com")
                    {
                        HttpContext.Session.SetString("VerifiedUserEmail", "admin@shieldops.com");
                        HttpContext.Session.SetString("VerifiedUserRole", "Admin");

                        await _userManager.AddToRoleAsync(user, "Admin");

                        await _signInManager.SignInAsync(user, isPersistent: false);
                        TempData["SuccessMessage"] = "System Administrator identity authenticated and provisioned successfully!";
                        return RedirectToAction("Index", "Admin");
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "Customer");
                        TempData["SuccessMessage"] = "Account provisioned successfully in SQLite ledger! Please log in.";
                        return RedirectToAction("Login", "Account");
                    }
                }

                // If password rules or unique email validation checks fail, append warnings
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                string cleanEmail = model.Email.Trim().ToLower();
                string targetAdminPassword = "AdminShieldOps123!"; // YOUR SINGLE PERMANENT ADMIN PASSWORD

                // =========================================================================
                // ON-THE-FLY PASSWORD SYNC MATRIX FOR EXISTING LIVE ADMIN ACCOUNT
                // =========================================================================
                if (cleanEmail == "admin@shieldops.com" && model.Password == targetAdminPassword)
                {
                    var adminUser = await _userManager.FindByEmailAsync(model.Email.Trim());
                    if (adminUser != null)
                    {
                        // Check if the input password matches the database hash. If it fails, force sync it!
                        var passwordCheck = await _userManager.CheckPasswordAsync(adminUser, targetAdminPassword);
                        if (!passwordCheck)
                        {
                            // Reset and overwrite the old forgotten password hash with your unified password
                            await _userManager.RemovePasswordAsync(adminUser);
                            await _userManager.AddPasswordAsync(adminUser, targetAdminPassword);
                        }

                        // Ensure user is bound to the Admin role identity tracking table
                        if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
                        {
                            await _userManager.AddToRoleAsync(adminUser, "Admin");
                        }
                    }
                }
                // =========================================================================

                // STRICT STANDARD IDENTITY AUTHENTICATION GATE
                var result = await _signInManager.PasswordSignInAsync(model.Email.Trim(), model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // Setup operational session parameters used by system layouts seamlessly
                    HttpContext.Session.SetString("VerifiedUserEmail", model.Email.Trim());

                    if (cleanEmail == "admin@shieldops.com")
                    {
                        HttpContext.Session.SetString("VerifiedUserRole", "Admin");
                        return RedirectToAction("Index", "Admin");
                    }

                    HttpContext.Session.SetString("VerifiedUserRole", "Customer");
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Access Denied: Invalid database security tokens.");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}