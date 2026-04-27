using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Travel_Agency.Models;
using Travel_Agency.ViewModels;
using Travel_Agency.Data;
using Microsoft.EntityFrameworkCore;

namespace Travel_Agency.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly TravelAgencyDbContext _context;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            TravelAgencyDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // =========================================================
        // 📝 REGISTER
        // =========================================================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 🔴 Check: Email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                // 🚫 Check if the email is BLOCKED - cannot create new account!
                if (existingUser.IsBlocked)
                {
                    ModelState.AddModelError("Email", "This email address has been blocked. Please contact support.");
                    return View(model);
                }

                ModelState.AddModelError("Email", "This email is already registered");
                return View(model);
            }

            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                DateOfBirth = model.DateOfBirth,
                IsBlocked = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Auto-assign to User role
                await _userManager.AddToRoleAsync(user, "User");

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // =========================================================
        // 🔐 LOGIN
        // =========================================================
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 🔴 First check if the user exists and is blocked
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                // 🚫 Check if user is BLOCKED
                if (user.IsBlocked)
                {
                    ModelState.AddModelError("", "🚫 Your account has been blocked. Please contact support for assistance.");
                    return View(model);
                }
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            ModelState.AddModelError("", "Invalid email or password");
            return View(model);
        }

        // =========================================================
        // 🚪 LOGOUT
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // =========================================================
        // 🚫 BLOCKED PAGE (optional - for redirecting blocked users)
        // =========================================================
        [HttpGet]
        public IActionResult Blocked()
        {
            return View();
        }
    }
}
