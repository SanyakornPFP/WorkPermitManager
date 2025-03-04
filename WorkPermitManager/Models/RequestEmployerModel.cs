namespace WorkPermitManager.Models
{
    public class RequestEmployerModel
    {
        public int? EmployerID { get; set; }
        public string NameTh { get; set; }
        public string NameEng { get; set; }
        public int BusinesstypeID { get; set; }
        public string CardID { get; set; }
        public string RegistrationNumber { get; set; }
        public string RegistrationDate { get; set; }
        public decimal RegisteredCapital { get; set; }
        public string JobTypeName { get; set; }
        public string JobDiscription { get; set; }
        public string DirectorNameTh { get; set; }
        public string DirectorNameEng { get; set; }
        public string DirectorPositionTh { get; set; }
        public string DirectorPositionEng { get; set; }
        public string OfficerNameOne { get; set; }
        public string OfficerPhoneOne { get; set; }
        public string OfficerNameTwo { get; set; }
        public string OfficerPhoneTwo { get; set; }
        public string HouseRecordNumber { get; set; }
        public string HouseNo { get; set; }
        public string Soi { get; set; }
        public string Road { get; set; }
        public string SubdistrictTh { get; set; }
        public string DistrictTh { get; set; }
        public string ProvinceTh { get; set; }
        public string Postcode { get; set; }
        public string SubdistrictEng { get; set; }
        public string DistrictEng { get; set; }
        public string ProvinceEng { get; set; }
        public string Phone { get; set; }
    }
}