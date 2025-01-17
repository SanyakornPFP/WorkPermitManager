using ContainerEvaluationSystem.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkPermitManager.Data;
using WorkPermitManager.Models;

namespace WorkPermitManager.Controllers
{
    [Authorize]
    public class PowerOfAttorneyController : Controller
    {
        private readonly Db_WorkPermitManagerModel _db;

        public PowerOfAttorneyController(Db_WorkPermitManagerModel db)
        {
            _db = db;
        }

        public IActionResult ListForm()
        {
            // Fetching the list of PowerOfAttorneys
            ViewBag.PowerOfAttorneysList = _db.PowerOfAttorneys.Where(s => s.IsDeleted == false)
                .Select(s => new
                {
                    s.PowerOfAttorneyID,
                    s.CodeForm,
                    GrantorName = s.User_GrantorID.FullName,
                    AttorneyName = s.User_AttorneyID.FullName,
                    CreationDate = s.CreationDate.ToString("dd-MM-yyyy"),
                    s.Status
                })
                .ToList();

            // Fetching the list of Companies
            ViewBag.CompanyList = _db.Companies
                .Select(c => new
                {
                    c.CompanyID,
                    c.CompanyName
                })
                .ToList();

            // Fetching the list of Users
            ViewBag.UserList = _db.Users
                .Select(u => new
                {
                    u.UserID,
                    u.FullName
                })
                .ToList();

            return View();
        }

        #region CreatePowerOfAttorney 
        [HttpPost]
        public async Task<IActionResult> CreatePowerOfAttorney(string CreationDate, int CompanyID, int GrantorID, int WitnessApprovalBy1, int WitnessApprovalBy2)
        {

            if (string.IsNullOrEmpty(CreationDate) || GrantorID == 0 || WitnessApprovalBy1 == 0 || WitnessApprovalBy2 == 0)
            {
                return NotFound();
            }
            else
            {
                var today = DateTime.Today;
                var formCountToday = _db.PowerOfAttorneys.Count(p => p.CreationDate >= today && p.CreationDate < today.AddDays(1)) + 1;
                var formNumber = formCountToday.ToString("D3"); // Format as 3 digits with leading zeros

                var PowerOfAttorney = new PowerOfAttorney
                {
                    CreationDate = Convert.ToDateTime(CreationDate),
                    CodeForm = "PA00" + DateTime.Now.ToString("yyyyMMdd") + formNumber,
                    CompanyID = CompanyID,
                    GrantorID = GrantorID,
                    AttorneyID = int.Parse(User.GetLoggedInUserID()),
                    WitnessApprovalBy1 = WitnessApprovalBy1,
                    WitnessApprovalBy2 = WitnessApprovalBy2,
                    CreatedAt = DateTime.Now,
                    UserManageID = int.Parse(User.GetLoggedInUserID())
                };

                // Processing the PowerOfAttorney creation
                _db.PowerOfAttorneys.Add(PowerOfAttorney);
                await _db.SaveChangesAsync();

                // Log the creation of the PowerOfAttorney
                var logEntry = new LogSystemData
                {
                    TableName = "PowerOfAttorneys",
                    Action = "Create",
                    RecordID = PowerOfAttorney.PowerOfAttorneyID, // Assuming PowerOfAttorneyID is the primary key
                    UserManageID = int.Parse(User.GetLoggedInUserID()), // Retrieve the logged user's ID
                    ActionTime = DateTime.Now,
                    IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                    OldValue = null, // No previous value since it's a new record
                    NewValue = $"CreationDate: {PowerOfAttorney.CreationDate}, CompanyID: {PowerOfAttorney.CompanyID}, GrantorID: {PowerOfAttorney.GrantorID}, WitnessApprovalBy1: {PowerOfAttorney.WitnessApprovalBy1}, WitnessApprovalBy2: {PowerOfAttorney.WitnessApprovalBy2}", // New record's details
                    Description = $"Created new PowerOfAttorney with CreationDate: {PowerOfAttorney.CreationDate}, GrantorID: {PowerOfAttorney.GrantorID}, WitnessApprovalBy1: {PowerOfAttorney.WitnessApprovalBy1}, WitnessApprovalBy2: {PowerOfAttorney.WitnessApprovalBy2}"
                };
                // Save the log entry
                _db.LogSystemDatas.Add(logEntry);
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "บันทึกข้อมูลผู้ใช้งานเรียบร้อย" });
            }
        }
        #endregion

        #region GetPowerOfAttorneyEdit  
        [HttpPost]
        public JsonResult GetPowerOfAttorneyEdit(int PowerOfAttorneyID)
        {

            if (PowerOfAttorneyID == 0)
            {
                return Json(new { success = false, message = "ไม่พบรหัสคำขอหนังสือมอบอำนาจ" });
            }
            else
            {
                var model = _db.PowerOfAttorneys
                    .Where(u => u.PowerOfAttorneyID == PowerOfAttorneyID)
                    .Select(s => new
                    {
                        s.PowerOfAttorneyID,
                        s.CodeForm,
                        CreationDate = s.CreationDate.ToString("yyyy-MM-dd"),
                        s.CompanyID,
                        s.GrantorID,
                        s.WitnessApprovalBy1,
                        s.WitnessApprovalBy2
                    })
                    .FirstOrDefault();
                return Json(new
                {
                    success = true,
                    data = model
                });
            }
        }
        #endregion

        #region UpdatePowerOfAttorney
        [HttpPost]
        public async Task<IActionResult> UpdatePowerOfAttorney(int PowerOfAttorneyID, string CreationDate, int CompanyID, int GrantorID, int WitnessApprovalBy1, int WitnessApprovalBy2)
        {
            if (PowerOfAttorneyID == 0)
            {
                return Json(new { success = false, message = "ไม่พบรหัสคำขอหนังสือมอบอำนาจ" });
            }
            else
            {
                var PowerOfAttorney = _db.PowerOfAttorneys.FirstOrDefault(u => u.PowerOfAttorneyID == PowerOfAttorneyID);
                if (PowerOfAttorney == null)
                {
                    return Json(new { success = false, message = "ไม่พบรหัสคำขอหนังสือมอบอำนาจ" });
                }
                else
                {
                    PowerOfAttorney.CreationDate = Convert.ToDateTime(CreationDate);
                    PowerOfAttorney.CompanyID = CompanyID;
                    PowerOfAttorney.GrantorID = GrantorID;
                    PowerOfAttorney.WitnessApprovalBy1 = WitnessApprovalBy1;
                    PowerOfAttorney.WitnessApprovalBy2 = WitnessApprovalBy2;
                    PowerOfAttorney.UpdatedAt = DateTime.Now;
                    PowerOfAttorney.UserManageID = int.Parse(User.GetLoggedInUserID());
                    await _db.SaveChangesAsync();
                    // Log the update of the PowerOfAttorney
                    var logEntry = new LogSystemData
                    {
                        TableName = "PowerOfAttorneys",
                        Action = "Update",
                        RecordID = PowerOfAttorney.PowerOfAttorneyID, // Assuming PowerOfAttorneyID is the primary key
                        UserManageID = int.Parse(User.GetLoggedInUserID()), // Retrieve the logged user's ID
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"CreationDate: {PowerOfAttorney.CreationDate}, CompanyID: {PowerOfAttorney.CompanyID}, GrantorID: {PowerOfAttorney.GrantorID}, WitnessApprovalBy1: {PowerOfAttorney.WitnessApprovalBy1}, WitnessApprovalBy2: {PowerOfAttorney.WitnessApprovalBy2}", // Previous value before update
                        NewValue = $"CreationDate: {CreationDate}, CompanyID: {CompanyID}, GrantorID: {GrantorID}, WitnessApprovalBy1: {WitnessApprovalBy1}, WitnessApprovalBy2: {WitnessApprovalBy2}", // New value after update
                        Description = $"Updated PowerOfAttorney with CreationDate: {PowerOfAttorney.CreationDate}, CompanyID: {PowerOfAttorney.CompanyID}, GrantorID: {PowerOfAttorney.GrantorID}, WitnessApprovalBy1: {PowerOfAttorney.WitnessApprovalBy1}, WitnessApprovalBy2: {PowerOfAttorney.WitnessApprovalBy2}"
                    };
                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();
                    return Json(new { success = true, message = "บันทึกข้อมูลเรียบร้อย" });
                }
            }
        }
        #endregion

        #region Delete PowerOfAttorneyForm
        [HttpPost]
        public async Task<IActionResult> DeletePowerOfAttorneyForm(int PowerOfAttorneyID)
        {

            if (PowerOfAttorneyID == 0)
            {
                return Json(new { success = false, message = "ไม่พบรหัสคำขอหนังสือมอบอำนาจ" });
            }
            else
            {
                var PowerOfAttorney = _db.PowerOfAttorneys.FirstOrDefault(u => u.PowerOfAttorneyID == PowerOfAttorneyID);
                if (PowerOfAttorney == null)
                {
                    return Json(new { success = false, message = "ไม่พบรหัสคำขอหนังสือมอบอำนาจ" });
                }
                else
                {
                    PowerOfAttorney.IsDeleted = true;
                    PowerOfAttorney.UpdatedAt = DateTime.Now;
                    PowerOfAttorney.UserManageID = int.Parse(User.GetLoggedInUserID());
                    await _db.SaveChangesAsync();
                    // Log the deletion of the PowerOfAttorney
                    var logEntry = new LogSystemData
                    {
                        TableName = "PowerOfAttorneys",
                        Action = "Delete",
                        RecordID = PowerOfAttorney.PowerOfAttorneyID, // Assuming PowerOfAttorneyID is the primary key
                        UserManageID = int.Parse(User.GetLoggedInUserID()), // Retrieve the logged user's ID
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"CreationDate: {PowerOfAttorney.CreationDate}, CompanyID: {PowerOfAttorney.CompanyID}, GrantorID: {PowerOfAttorney.GrantorID}, WitnessApprovalBy1: {PowerOfAttorney.WitnessApprovalBy1}, WitnessApprovalBy2: {PowerOfAttorney.WitnessApprovalBy2}", // Previous value before deletion
                        NewValue = null, // No new value since it's deleted
                        Description = $"Deleted PowerOfAttorney with CreationDate: {PowerOfAttorney.CreationDate}, CompanyID: {PowerOfAttorney.CompanyID}, GrantorID: {PowerOfAttorney.GrantorID}, WitnessApprovalBy1: {PowerOfAttorney.WitnessApprovalBy1}, WitnessApprovalBy2: {PowerOfAttorney.WitnessApprovalBy2}"
                    };
                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();
                    return Json(new { success = true, message = "ยกเลิกคำขอมอบอำนาจเรียบร้อย" });
                }
            }
        }

        #endregion

        #region GetPowerOfAttorney DetailModdel
        [HttpPost]
        public JsonResult GetPowerOfAttorneyModel(int PowerOfAttorneyID)
        {
            if (PowerOfAttorneyID == 0)
            {
                return Json(new { success = false, message = "ไม่พบรหัสคำขอหนังสือมอบอำนาจ" });
            }
            else
            {
                var model = _db.PowerOfAttorneys
                    .Where(u => u.PowerOfAttorneyID == PowerOfAttorneyID)
                    .Select(s => new
                    {
                        s.CodeForm,
                        CreationDate = s.CreationDate.ToString("dd-MM-yyyy"),
                        GrantorName = s.User_GrantorID.FullName,
                        GrantorStatus = s.GrantorApprovalStatus,
                        GrantorDateApprove = s.GrantorDateApprove,
                        AttorneyName = s.User_AttorneyID.FullName,
                        AttorneyStatus = s.AttorneyApprovalStatus,
                        AttorneyDateApprove = s.AttorneyDateApprove,
                        Witness1Name = s.User_WitnessApprovalBy1.FullName,
                        Witness1Status = s.WitnessApprovalStatus1,
                        WitnessDateApprove1 = s.WitnessDateApprove1,
                        Witness2Name = s.User_WitnessApprovalBy2.FullName,
                        Witness2Status = s.WitnessApprovalStatus2,
                        WitnessDateApprove2 = s.WitnessDateApprove2,
                        s.Status
                    })
                    .FirstOrDefault();


                return Json(new
                {
                    success = true,
                    data = model

                });
            }
        }
        #endregion

    }
}
