using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PIATenderSystem.Models;
using System.Threading.Tasks;

namespace PIATenderSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string companyName, string email, string password)
        {
            if (string.IsNullOrEmpty(companyName) || !AllowedCompanies.Companies.ContainsKey(companyName))
            {
                ModelState.AddModelError("", "Please select a valid registered company.");
                return View();
            }

            string allowedDomain = AllowedCompanies.Companies[companyName];
            string emailDomain = email.Contains("@") ? email.Split('@')[1].ToLower() : "";

            if (emailDomain != allowedDomain.ToLower())
            {
                ModelState.AddModelError("", $"Email must belong to the domain '{allowedDomain}' for {companyName}.");
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                CompanyName = companyName,
                Role = "Customer"
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Tender");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(email, password, false, false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null && user.Role == "Admin")
                    return RedirectToAction("Dashboard", "Admin");
                else
                    return RedirectToAction("Index", "Tender");
            }

            ModelState.AddModelError("", "Invalid login attempt");
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}