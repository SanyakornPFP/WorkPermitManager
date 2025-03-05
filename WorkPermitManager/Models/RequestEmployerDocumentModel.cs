namespace WorkPermitManager.Models
{
    public class RequestEmployerDocumentModel
    {
        public int DocumentID { get; set; }
        public int EmployerID { get; set; }
        public string DocumentTypeName { get; set; }
        public string Discription { get; set; }
        public string ExpiryDate { get; set; }
        public IFormFile File { get; set; } // For file upload
    }
}
