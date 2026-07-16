using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIATenderSystem.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PIATenderSystem.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public CustomerController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Dashboard(string search, DateTime? startDate, DateTime? closingDate, string department, string status = "open")
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.FullName = user?.FullName ?? "Customer";
            ViewBag.CompanyName = user?.CompanyName ?? "";

            var today = DateTime.Today;
            var query = _context.Tenders.AsQueryable();

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

            ViewBag.Departments = _context.Tenders.Select(t => t.Department).Distinct().ToList();
            ViewBag.Status = status;
            ViewBag.Search = search;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.ClosingDate = closingDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedDepartment = department;

            return View(tenders);
        }

        public IActionResult DownloadPdf(int id)
        {
            var tender = _context.Tenders.Find(id);
            if (tender == null)
                return Content("Tender record not found in the database. ID: " + id);

            if (string.IsNullOrEmpty(tender.PdfFileName))
                return Content("The PdfFileName field is empty in the database for this tender.");

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tenders", tender.PdfFileName);

            if (!System.IO.File.Exists(path))
                return Content("File not found at this location: " + path);

            return PhysicalFile(path, "application/pdf", tender.PdfFileName);
        }
    }
}