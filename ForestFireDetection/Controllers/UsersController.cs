using ForestFireDetection.Data;
using ForestFireDetection.Models;
using ForestFireDetection.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

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
            return View(fireStations); // Make sure you have a corresponding view to display the list of patients
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

            return View(patients); // Make sure you have a corresponding view to display the list of patients
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
                    EmailConfirmed = true  // or set based on your application logic
                };

                var result = await _userManager.CreateAsync(newUser, user.Password);

                // Optionally add user to a role
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(newUser, UserRoles.User);  // Assign a default role or based on model
                }

                return RedirectToAction(nameof(ListFireStations));
            }
            TempData["Error"] = "entered information is not correct";
            // If we reach here, something went wrong, re-show form
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

            // Pass the PatientUser model to the view
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
                // Handle the case where the user isn't found
                return NotFound();
            }
            //getting the user role
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            // Update the user's properties
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
                // Handle errors
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
			//checking the user`s role to redirect to its own list page
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
                // Handle the case where the user isn't found
                return NotFound();
            }

            // Update the user's properties
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
                // Handle errors
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
                    EmailConfirmed = true  // or set based on your application logic
                };

                var result = await _userManager.CreateAsync(newUser, user.Password);

                // Optionally add user to a role
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(newUser, UserRoles.Admin);  // Assign a default role or based on model
                }

                return RedirectToAction(nameof(ListAdmin));
            }
            TempData["Error"] = "entered information is not correct";
            // If we reach here, something went wrong, re-show form
            return View(user);
        }
    }
}
