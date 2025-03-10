namespace WorkPermitManager.Models
{
    public class RequestEmployerAuthorizationConfigModel
    {
        public int EmployerAuthorizationConfigID { get; set; }
        public int EmployerID { get; set; }
        public int AuthorizedPersonImportID { get; set; }
        public int AuthorizedPersonMouID { get; set; }
        public int? AuthorizedWitness1ID { get; set; }
        public int? AuthorizedWitness2ID { get; set; }
        public int AuthorizedPersonAssignorID { get; set; }
    }
}
