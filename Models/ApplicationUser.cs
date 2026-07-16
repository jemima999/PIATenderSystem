using Microsoft.AspNetCore.Identity;

namespace PIATenderSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public string CompanyName { get; set; } = "";
    }
}