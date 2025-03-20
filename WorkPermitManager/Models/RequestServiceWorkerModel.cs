namespace WorkPermitManager.Models
{
    public class RequestServiceWorkerModel
    {
        public int ServiceWorkerID { get; set; }
        public int ServiceID { get; set; }
        public string PassportNumber { get; set; }
        public string Nationality { get; set; }
        public string Title { get; set; }
        public string FirstNameEN { get; set; }
        public string FirstNameTH { get; set; }
        public string LastNameEN { get; set; }
        public string LastNameTH { get; set; }
        public int ServiceItemID { get; set; }
        public decimal? ServiceFee { get; set; }
        public DateTime? Expiry90Days { get; set; }
        public string Note { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? PassportIssueDate { get; set; }
        public DateTime? PassportExpiryDate { get; set; }
        public string WorkPermitNumber { get; set; }
        public string EntryVisaNumber { get; set; }
        public string PlaceOfBirth { get; set; }
        public string PassportIssuedAt { get; set; }
        public string Country { get; set; }
    }
}
