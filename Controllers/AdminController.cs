using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PIATenderSystem.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PIATenderSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _env = env;
        }

        public IActionResult Dashboard()
        {
            ViewBag.TotalTenders = _context.Tenders.Count();
            ViewBag.PublishedCount = _context.Tenders.Count(t => t.IsPublished);
            ViewBag.DraftCount = _context.Tenders.Count(t => !t.IsPublished);
            ViewBag.TotalCustomers = _userManager.Users.Count();
            return View();
        }

        [HttpGet]
        public IActionResult CreateCustomer()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustomer(string fullName, string companyName, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email and Password are both required.";
                return View();
            }

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                ViewBag.Error = "An account already exists with this email.";
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                CompanyName = companyName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));
                return View();
            }

            if (!await _roleManager.RoleExistsAsync("Customer"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Customer"));
            }

            await _userManager.AddToRoleAsync(user, "Customer");

            ViewBag.Success = $"Customer account created successfully: {email}";
            return View();
        }

        // ---------------- DEPARTMENTS ----------------

        [HttpGet]
        public IActionResult AddDepartment()
        {
            ViewBag.Departments = _context.Departments.OrderBy(d => d.Name).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddDepartment(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ViewBag.Error = "Please enter a department name.";
                ViewBag.Departments = _context.Departments.OrderBy(d => d.Name).ToList();
                return View();
            }

            bool exists = _context.Departments.Any(d => d.Name.ToLower() == name.Trim().ToLower());
            if (exists)
            {
                ViewBag.Error = "This department already exists.";
                ViewBag.Departments = _context.Departments.OrderBy(d => d.Name).ToList();
                return View();
            }

            _context.Departments.Add(new Department { Name = name.Trim() });
            _context.SaveChanges();

            ViewBag.Success = $"Department added successfully: {name.Trim()}";
            ViewBag.Departments = _context.Departments.OrderBy(d => d.Name).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteDepartment(int id)
        {
            var dept = _context.Departments.Find(id);
            if (dept != null)
            {
                _context.Departments.Remove(dept);
                _context.SaveChanges();
            }
            return RedirectToAction("AddDepartment");
        }

        // ---------------- TENDERS ----------------

        [HttpGet]
        public IActionResult UploadTender()
        {
            ViewBag.Departments = _context.Departments.OrderBy(d => d.Name).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTender(string title, string department, DateTime startDate, DateTime closingDate, IFormFile pdfFile)
        {
            ViewBag.Departments = _context.Departments.OrderBy(d => d.Name).ToList();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(department))
            {
                ViewBag.Error = "Title and Department are both required.";
                return View();
            }

            if (pdfFile == null || pdfFile.Length == 0)
            {
                ViewBag.Error = "Please upload a Tender PDF.";
                return View();
            }

            var tendersFolder = Path.Combine(_env.WebRootPath, "tenders");
            if (!Directory.Exists(tendersFolder))
                Directory.CreateDirectory(tendersFolder);

            var savedFileName = Guid.NewGuid().ToString("N") + Path.GetExtension(pdfFile.FileName);
            var fullPath = Path.Combine(tendersFolder, savedFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await pdfFile.CopyToAsync(stream);
            }

            var tender = new Tender
            {
                Title = title,
                Department = department,
                StartDate = startDate,
                ClosingDate = closingDate,
                PdfFileName = savedFileName,
                IsPublished = false
            };

            _context.Tenders.Add(tender);
            await _context.SaveChangesAsync();

            tender.TenderRefNo = tender.Id;
            await _context.SaveChangesAsync();

            ViewBag.Success = $"Tender saved as draft. Tender ID: {tender.TenderRefNo}";
            return View();
        }

        public IActionResult ManageTenders()
        {
            var tenders = _context.Tenders.OrderByDescending(t => t.Id).ToList();
            return View(tenders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Publish(int id)
        {
            var tender = _context.Tenders.Find(id);
            if (tender == null) return NotFound();

            tender.IsPublished = true;
            _context.SaveChanges();

            return RedirectToAction("ManageTenders");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Unpublish(int id)
        {
            var tender = _context.Tenders.Find(id);
            if (tender == null) return NotFound();

            tender.IsPublished = false;
            _context.SaveChanges();

            return RedirectToAction("ManageTenders");
        }
    }
}