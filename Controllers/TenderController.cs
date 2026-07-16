//updated version
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PIATenderSystem.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PIATenderSystem.Controllers
{
    public class TenderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public TenderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        public IActionResult Index(string search, DateTime? startDate, DateTime? closingDate, string department, string status = "open")
        {
            var today = DateTime.Today;
            var query = _context.Tenders.Where(t => t.IsPublished).AsQueryable();

            if (status == "open")
                query = query.Where(t => t.ClosingDate >= today);
            else if (status == "closed")
                query = query.Where(t => t.ClosingDate < today);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.Title.Contains(search));

            if (startDate.HasValue)
                query = query.Where(t => t.StartDate >= startDate.Value);

            if (closingDate.HasValue)
                query = query.Where(t => t.ClosingDate <= closingDate.Value);

            if (!string.IsNullOrWhiteSpace(department) && department != "All")
                query = query.Where(t => t.Department == department);

            var tenders = query.OrderByDescending(t => t.ClosingDate).ToList();

            ViewBag.Departments = _context.Tenders.Where(t => t.IsPublished).Select(t => t.Department).Distinct().ToList();
            ViewBag.Status = status;
            ViewBag.Search = search;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.ClosingDate = closingDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedDepartment = department;

            return View(tenders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var tender = _context.Tenders.Find(id);
            if (tender == null)
                return NotFound();

            ViewBag.AlreadyApplied = false;
            ViewBag.MyDocuments = new List<ApplicationDocument>();

            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Customer"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var companyName = user.CompanyName ?? "";

                    var existingApplication = _context.TenderApplications
                        .FirstOrDefault(a => a.TenderId == id && a.CompanyName.ToLower() == companyName.ToLower());

                    if (existingApplication != null)
                    {
                        ViewBag.AlreadyApplied = true;
                        ViewBag.MyDocuments = _context.ApplicationDocuments
                            .Where(d => d.TenderApplicationId == existingApplication.Id)
                            .ToList();
                    }
                }
            }

            return View(tender);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int id, List<IFormFile> bidDocuments)
        {
            var tender = _context.Tenders.Find(id);
            if (tender == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var companyName = user.CompanyName ?? "";

            bool alreadyApplied = _context.TenderApplications
                .Any(a => a.TenderId == id && a.CompanyName.ToLower() == companyName.ToLower());

            if (!alreadyApplied)
            {
                var application = new TenderApplication
                {
                    TenderId = id,
                    UserId = user.Id,
                    CompanyName = companyName
                };

                _context.TenderApplications.Add(application);
                _context.SaveChanges();

                if (bidDocuments != null && bidDocuments.Count > 0)
                {
                    
                    var appFolder = Path.Combine(_env.WebRootPath, "applications", $"tender_{id}", $"app_{application.Id}");
                    if (!Directory.Exists(appFolder))
                        Directory.CreateDirectory(appFolder);

                    foreach (var file in bidDocuments)
                    {
                        if (file.Length == 0) continue;

                        var originalBaseName = Path.GetFileNameWithoutExtension(file.FileName).Replace(" ", "_");
var shortCode = Guid.NewGuid().ToString("N").Substring(0, 6);
var storedName = $"{originalBaseName}_{shortCode}{Path.GetExtension(file.FileName)}";
                        var fullPath = Path.Combine(appFolder, storedName);

                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        _context.ApplicationDocuments.Add(new ApplicationDocument
                        {
                            TenderApplicationId = application.Id,
                            OriginalFileName = file.FileName,
                            StoredFileName = storedName
                        });
                    }
                    _context.SaveChanges();
                }

                TempData["ApplySuccess"] = "You have successfully applied for this tender.";
            }
            else
            {
                TempData["ApplySuccess"] = "Your company has already applied for this tender.";
            }

            return RedirectToAction("Details", new { id });
        }

        public IActionResult DownloadPdf(int id)
        {
            var tender = _context.Tenders.Find(id);
            if (tender == null)
                return Content("Tender record not found in database. ID: " + id);

            if (string.IsNullOrEmpty(tender.PdfFileName))
                return Content("No PDF file is associated with this tender.");

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tenders", tender.PdfFileName);

            if (!System.IO.File.Exists(path))
                return Content("File not found at this location: " + path);

            return PhysicalFile(path, "application/pdf", tender.PdfFileName);
        }

        public IActionResult DownloadApplicationDocument(int docId)
        {
            var doc = _context.ApplicationDocuments.Find(docId);
            if (doc == null)
                return Content("Document not found.");

            var application = _context.TenderApplications.Find(doc.TenderApplicationId);
            if (application == null)
                return Content("Application record not found.");

            var path = Path.Combine(_env.WebRootPath, "applications", $"tender_{application.TenderId}", $"app_{application.Id}", doc.StoredFileName);

            if (!System.IO.File.Exists(path))
                return Content("File not found at this location: " + path);

            return PhysicalFile(path, "application/octet-stream", doc.OriginalFileName);
        }
    }
}
