namespace WorkPermitManager.Models
{
    public class PermissionUpdateModel
    {
        public int UserID { get; set; }
        //public string ShowAllData { get; set; }
        public List<PermissionFunctionModel> Permissions { get; set; }
    }
    public class PermissionFunctionModel
    {
        public string FunctionName { get; set; }
        public bool CanRead { get; set; }
        public bool CanCreate { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
    }
}
