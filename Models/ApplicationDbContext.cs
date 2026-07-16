using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PIATenderSystem.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tender> Tenders { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<TenderApplication> TenderApplications { get; set; }
        public DbSet<ApplicationDocument> ApplicationDocuments { get; set; }
    }
}