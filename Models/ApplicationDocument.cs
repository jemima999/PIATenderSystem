using System;

namespace PIATenderSystem.Models
{
    public class ApplicationDocument
    {
        public int Id { get; set; }
        public int TenderApplicationId { get; set; }
        public string OriginalFileName { get; set; }
        public string StoredFileName { get; set; }
        public DateTime UploadedOn { get; set; } = DateTime.Now;
    }
}