namespace WorkPermitManager.Models
{
    public class RequestEmployerAuthorizedPersonDetailsModel
    {
        public int EmployerID { get; set; }
        public int? EmployerAuthorizedPersonID { get; set; }
        public string NameTh { get; set; }
        public string NameEng { get; set; }
        public string LicenseNumber { get; set; }
        public string CompanyName { get; set; }
        public string CardID { get; set; }
        public string HouseNo { get; set; }
        public string VillageNo { get; set; }
        public string Soi { get; set; }
        public string Road { get; set; }
        public string SubdistrictTh { get; set; }
        public string DistrictTh { get; set; }
        public string ProvinceTh { get; set; }
        public string Phone { get; set; }
    }
}
