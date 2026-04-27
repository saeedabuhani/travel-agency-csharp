using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Travel_Agency.Models;

namespace Travel_Agency.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public AdminUsersController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        // ================= USERS LIST =================
        public IActionResult Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // ================= MAKE ADMIN =================
        [HttpPost]
        public async Task<IActionResult> MakeAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }

            TempData["Success"] = $"User {user.UserName} is now an Admin.";
            return RedirectToAction("Index");
        }

        // ================= REMOVE ADMIN =================
        [HttpPost]
        public async Task<IActionResult> RemoveAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // ⚠️ מניעת הסרת Admin אחרון
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count <= 1)
            {
                TempData["Error"] = "You cannot remove the last Admin.";
                return RedirectToAction("Index");
            }

            await _userManager.RemoveFromRoleAsync(user, "Admin");

            TempData["Success"] = $"Admin role removed from {user.UserName}.";
            return RedirectToAction("Index");
        }
    }
}
