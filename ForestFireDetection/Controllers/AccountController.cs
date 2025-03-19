using ForestFireDetection.Data;
using ForestFireDetection.Models;
using Hospital_appointment_system.Data;
using Hospital_appointment_system.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Hospital_appointment_system.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManeger;
        private readonly SignInManager<ApplicationUser> _signInManeger;
        private readonly ForestFireDetectionDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManeger,
            SignInManager<ApplicationUser> signInManeger,
            ForestFireDetectionDbContext context)
        {
            _userManeger = userManeger;
            _signInManeger = signInManeger;
            _context= context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            var response = new LoginViewModel();
            return View(response);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel) 
        {
            if(!ModelState.IsValid ) 
            {
                return View(loginViewModel);
            }
            var user = await _userManeger.FindByEmailAsync(loginViewModel.Email);
            if (user != null) 
            {
                var passCheck = await _userManeger.CheckPasswordAsync(user, loginViewModel.Password);
                if(passCheck)
                {
                    var result = await _signInManeger.PasswordSignInAsync(user, loginViewModel.Password, false, false);
                    if (result.Succeeded) 
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                TempData["Error"] = "Wrong credentials.Please  Try again ";
                return View(loginViewModel);
            }
            TempData["Error"] = "Wrong credentials.Please  Try again 2";
                return View(loginViewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManeger.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

    }
}
