using System;

namespace PIATenderSystem.Models
{
    public class TenderApplication
    {
        public int Id { get; set; }
        public int TenderId { get; set; }
        public string UserId { get; set; }
        public string CompanyName { get; set; }
        public DateTime AppliedOn { get; set; } = DateTime.Now;
    }
}