using System;

namespace PIATenderSystem.Models
{
    public class Tender
    {
        public int Id { get; set; }
        public int TenderRefNo { get; set; }
        public string Title { get; set; }
        public string Department { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ClosingDate { get; set; }
        public string? PdfFileName { get; set; }
        public bool IsPublished { get; set; } = false;
    }
}