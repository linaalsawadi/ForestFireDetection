using ForestFireDetection.Data;
using ForestFireDetection.Models;
using ForestFireDetection.Models.Enums;
using ForestFireDetection.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForestFireDetection.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class UsersController : Controller
    {
        private readonly ForestFireDetectionDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(ForestFireDetectionDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ─── Unified List ───
        public async Task<IActionResult> ListFireStations() => View(await GetUsersInRole(UserRoles.User));
        public async Task<IActionResult> ListAdmin() => View(await GetUsersInRole(UserRoles.Admin));

        // ─── Create ───
        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterViewModel model)
            => await CreateUserInternal(model, UserRoles.User, nameof(ListFireStations));

        [HttpGet]
        public IActionResult CreateAdmin() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(RegisterViewModel model)
            => await CreateUserInternal(model, UserRoles.Admin, nameof(ListAdmin));

        // ─── Edit ───
        [HttpGet]
        public async Task<IActionResult> Edit(string? id) => await GetUserView(id);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser model)
            => await UpdateUserInternal(model, nameof(ListFireStations));

        [HttpGet]
        public async Task<IActionResult> EditAdmin(string? id) => await GetUserView(id);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdmin(ApplicationUser model)
            => await UpdateUserInternal(model, nameof(ListAdmin));

        // ─── Delete ───
        [HttpGet]
        public async Task<IActionResult> DeleteAsync(string? id) => await GetUserView(id);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(ApplicationUser model)
            => await DeleteUserInternal(model.Id, nameof(ListFireStations));

        [HttpGet]
        public async Task<IActionResult> DeleteAdmin(string? id) => await GetUserView(id);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAdmin(ApplicationUser model)
            => await DeleteUserInternal(model.Id, nameof(ListAdmin));

        // ─── Private Helpers ───
        private async Task<List<ApplicationUser>> GetUsersInRole(string role)
        {
            var users = await _userManager.Users.ToListAsync();
            var result = new List<ApplicationUser>();
            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, role))
                    result.Add(user);
            }
            return result;
        }

        private async Task<IActionResult> GetUserView(string? id)
        {
            if (id == null) return NotFound();
            var user = await _context.Users.FindAsync(id);
            return user == null ? NotFound() : View(user);
        }

        private async Task<IActionResult> CreateUserInternal(RegisterViewModel model, string role, string redirectAction)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Entered information is not correct";
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.EmailAddress))
            {
                TempData["Error"] = "This email address is already in use";
                return View(model);
            }

            var newUser = new ApplicationUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.EmailAddress,
                Email = model.EmailAddress,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(newUser, model.Password);
            if (result.Succeeded)
                await _userManager.AddToRoleAsync(newUser, role);

            return RedirectToAction(redirectAction);
        }

        private async Task<IActionResult> UpdateUserInternal(ApplicationUser model, string redirectAction)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.UserName = model.Email;
            user.Email = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                return RedirectToAction(redirectAction);

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        private async Task<IActionResult> DeleteUserInternal(string userId, string redirectAction)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);
            return RedirectToAction(redirectAction);
        }
    }
}
