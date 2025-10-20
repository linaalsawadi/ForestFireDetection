using ForestFireDetection.Data;
using ForestFireDetection.Models;
using ForestFireDetection.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForestFireDetection.Controllers
{
    public class UsersController : Controller
    {
        private readonly ForestFireDetectionDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(ForestFireDetectionDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> ListFireStations()
        {
            var users = await _userManager.Users.ToListAsync();
            var fireStations = new List<ApplicationUser>();

            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, UserRoles.User))
                {
                    fireStations.Add(user);
                }
            }
            return View(fireStations);
        }

        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> ListAdmin()
        {
            var users = await _userManager.Users.ToListAsync();
            var patients = new List<ApplicationUser>();

            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, UserRoles.Admin))
                {
                    patients.Add(user);
                }
            }

            return View(patients);
        }

        // GET: User/Create
        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Create(RegisterViewModel user)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Email.Equals(user.EmailAddress)))
                {
                    TempData["Error"] = "This email address is already in use";
                    return View(user);
                }
                var newUser = new ApplicationUser
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserName = user.EmailAddress,
                    Email = user.EmailAddress,
                    EmailConfirmed = true 
                };

                var result = await _userManager.CreateAsync(newUser, user.Password);

                
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(newUser, UserRoles.User);
                }

                return RedirectToAction(nameof(ListFireStations));
            }
            TempData["Error"] = "entered information is not correct";
            return View(user);
        }

        //GET Edit
        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Edit(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patientUser = await _context.Users.FindAsync(id);
            if (patientUser == null)
            {
                return NotFound();
            }

            return View(patientUser);
        }

        //POST Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Edit(ApplicationUser model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
               
                return NotFound();
            }
           
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
          
            user.FirstName = model.FirstName;
            user.LastName = model.LastName; 
            user.UserName = model.Email;
            user.Email = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
				return RedirectToAction(nameof(ListFireStations));
			}
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }
        }

        // GET: User/Delete
        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> DeleteAsync(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var patientsFromDb = await _context.Users.FindAsync(id);
            if (patientsFromDb == null)
            {
                return NotFound();
            }

            return View(patientsFromDb);

        }

        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Delete(ApplicationUser User)
        {
            var user1 = await _userManager.FindByIdAsync(User.Id);
            _context.Remove(user1);
            var result = _context.SaveChanges();
			return RedirectToAction(nameof(ListFireStations));
		}

        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> EditAdmin(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patientUser = await _context.Users.FindAsync(id);
            if (patientUser == null)
            {
                return NotFound();
            }

            return View(patientUser);
        }

        //POST Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> EditAdmin(ApplicationUser model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName; 
            user.UserName = model.Email;
            user.Email = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
				return RedirectToAction(nameof(ListAdmin));
			}
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }
        }

        // GET: User/Delete
        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> DeleteAdmin(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var patientsFromDb = await _context.Users.FindAsync(id);
            if (patientsFromDb == null)
            {
                return NotFound();
            }

            return View(patientsFromDb);

        }
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> DeleteAdmin(ApplicationUser User)
        {
            var user1 = await _userManager.FindByIdAsync(User.Id);
            _context.Remove(user1);
            var result = _context.SaveChanges();
			return RedirectToAction(nameof(ListAdmin));
		}

        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)]
        public IActionResult CreateAdmin()
        {
            return View();
        }

        [Authorize(Roles = UserRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> CreateAdmin(RegisterViewModel user)
        {

            if (ModelState.IsValid)
            {

                if (await _context.Users.AnyAsync(u => u.Email.Equals(user.EmailAddress)))
                {
                    TempData["Error"] = "This email address is already in use";
                    return View(user);
                }
                var newUser = new ApplicationUser
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName, 
                    Email = user.EmailAddress,
                    UserName = user.EmailAddress,
                    EmailConfirmed = true 
                };

                var result = await _userManager.CreateAsync(newUser, user.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(newUser, UserRoles.Admin); 
                }

                return RedirectToAction(nameof(ListAdmin));
            }
            TempData["Error"] = "entered information is not correct";
            return View(user);
        }
    }
}
