using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShieldOps.Data;
using ShieldOps.Models;
using ShieldOps.Models.ViewModels;
using ShieldOps.Services;
namespace ShieldOps.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailMockService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailMockService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    IsVerified = false
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Customer");

                    // Generate custom 6-digit cryptographic registration token
                    var random = new Random();
                    string tokenString = random.Next(100000, 999999).ToString();

                    var verificationToken = new VerificationToken
                    {
                        UserId = user.Id,
                        Token = tokenString,
                        ExpiryDate = DateTime.UtcNow.AddMinutes(15),
                        IsUsed = false
                    };

                    _context.VerificationTokens.Add(verificationToken);
                    await _context.SaveChangesAsync();

                    // Dispatches background print pipeline to Visual Studio Output console
                    _emailService.SendVerificationToken(user.Email, tokenString);

                    return RedirectToAction("VerifyToken", new { email = user.Email });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    if (!user.IsVerified)
                    {
                        ModelState.AddModelError(string.Empty, "Your account profile has not been verified yet. Please enter your verification token.");
                        return RedirectToAction("VerifyToken", new { email = user.Email });
                    }

                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                ModelState.AddModelError(string.Empty, "Invalid login credentials detected.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult VerifyToken(string email)
        {
            var model = new VerifyTokenViewModel { Email = email };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyToken(VerifyTokenViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return NotFound("User profile matching this request doesn't exist.");
                }

                var tokenRecord = await _context.VerificationTokens
                    .Where(t => t.UserId == user.Id && t.Token == model.Token && !t.IsUsed && t.ExpiryDate > DateTime.UtcNow)
                    .FirstOrDefaultAsync();

                if (tokenRecord != null)
                {
                    tokenRecord.IsUsed = true;
                    user.IsVerified = true;

                    _context.VerificationTokens.Update(tokenRecord);
                    await _userManager.UpdateAsync(user);
                    await _context.SaveChangesAsync();

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Invalid or expired token security parameters submitted.");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}