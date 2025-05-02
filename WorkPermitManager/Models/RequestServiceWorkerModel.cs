namespace WorkPermitManager.Models
{
    public class RequestServiceWorkerModel
    {
        public int ServiceWorkerID { get; set; }
        public int ServiceID { get; set; }
        public string Title { get; set; }
        public string FirstNameEN { get; set; }
        public string FirstNameTH { get; set; }
        public string LastNameEN { get; set; }
        public string LastNameTH { get; set; }
        public int ServiceItemID { get; set; }
        public string Country { get; set; }
        public string Nationality { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? Expiry90Days { get; set; }
        public decimal? ServiceFee { get; set; }
        public string Note { get; set; }
        public string PassportNumber { get; set; }
        public DateTime? PassportDateOfIssue { get; set; }
        public DateTime? PassportExpiryDate { get; set; }
        public string VisaNumber { get; set; }
        public string TypeVisa { get; set; }
        public DateTime? VisaDateOfIssue { get; set; }
        public DateTime? VisaExpiryDate { get; set; }
        public string VisaIssuedAt { get; set; }
        public string PlaceOfBirth { get; set; }
        public string PassportIssuedAt { get; set; }
        public DateTime? DateOfArrival { get; set; }
        public string ImmigrationCheckpoint { get; set; }
        public DateTime? PermittedUntil { get; set; }
        public string ResidenceNo { get; set; }
        public string ResidenceIssuedAt { get; set; }
        public string ResidenceProvince { get; set; }
        public DateTime? ResidenceDateOfIssue { get; set; }
        public DateTime? ResidenceExpiryDate { get; set; }
        public string AlienNo { get; set; }
        public string AlienIssuedAt { get; set; }
        public string AlienProvince { get; set; }
        public DateTime? AlienDateOfIssue { get; set; }
        public DateTime? AlienExpiryDate { get; set; }
        public string WorkPermitNumber { get; set; }
        public string WorkPermitIssuedAt { get; set; }
        public DateTime? WorkPermitDateOfIssue { get; set; }
        public DateTime? WorkPermitExpiryDate { get; set; }
        public string WorkPermitIssuedAtProvince { get; set; }
        public string WorkPermitActionType { get; set; }

    }
}