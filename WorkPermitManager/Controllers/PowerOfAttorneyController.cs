using ContainerEvaluationSystem.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Reporting.NETCore;
using QRCoder;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using WorkPermitManager.Data;
using WorkPermitManager.Models;

namespace WorkPermitManager.Controllers
{
    [Authorize]
    public class PowerOfAttorneyController : Controller
    {
        private readonly Db_WorkPermitManagerModel _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PowerOfAttorneyController(Db_WorkPermitManagerModel db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult ListForm(string type)
        {
            if (type == null)
            {
                type = "รอการอนุมัติ";
            }

            ViewBag.TypeForm = type;

            var PowerOfAttorneyModel = _db.PowerOfAttorneys.Where(s => s.IsDeleted == false && s.Status == type)
                .Select(s => new
                {
                    s.PowerOfAttorneyID,
                    s.CodeForm,
                    GrantorName = s.User_GrantorID.FullName,
                    AttorneyName = s.User_AttorneyID.FullName,
                    CreationDate = s.CreationDate.ToString("dd-MM-yyyy"),
                    s.GrantorDateApprove,
                    s.Status
                })
                .ToList();

            if (User.GetRole_AdministratorIsActive() == "True")
            {
                ViewBag.PowerOfAttorneysList = PowerOfAttorneyModel;
            }
            else
            {
                ViewBag.PowerOfAttorneysList = PowerOfAttorneyModel
                    .Where(s => s.AttorneyName == User.GetLoggedInUserID())
                    .ToList();
            }

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


        #region ListApprovalForm
        public IActionResult ListApprovalForm(string type)
        {
            if (type == null)
            {
                type = "รอการอนุมัติ";
            }
            ViewBag.TypeForm = type;
            var PowerOfAttorneyModel = _db.PowerOfAttorneys.Where(s => s.IsDeleted == false && s.Status == type)
                .Select(s => new
                {
                    s.PowerOfAttorneyID,
                    s.CodeForm,
                    s.GrantorID,
                    GrantorName = s.User_GrantorID.FullName,
                    s.AttorneyID,
                    AttorneyName = s.User_AttorneyID.FullName,
                    CreationDate = s.CreationDate.ToString("dd-MM-yyyy"),
                    s.GrantorDateApprove,
                    s.Status
                })
                .ToList();

            if (User.GetRole_AdministratorIsActive() != "True")
            {
                PowerOfAttorneyModel = PowerOfAttorneyModel
                    .Where(s => s.GrantorID == int.Parse(User.GetLoggedInUserID()))
                    .ToList();
            }

            ViewBag.PowerOfAttorneysList = PowerOfAttorneyModel.ToList();

            return View();
        }
        #endregion



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
                    GrantorApprovalBy = GrantorID,
                    AttorneyApprovalBy = int.Parse(User.GetLoggedInUserID()),
                    WitnessApprovalBy1 = WitnessApprovalBy1,
                    WitnessApprovalStatus1 = "อนุมัติเรียบร้อย",
                    WitnessDateApprove1 = DateTime.Now,
                    WitnessApprovalBy2 = WitnessApprovalBy2,
                    WitnessApprovalStatus2 = "อนุมัติเรียบร้อย",
                    WitnessDateApprove2 = DateTime.Now,
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
                    PowerOfAttorney.GrantorApprovalBy = GrantorID;
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

        #region DocumentCountStatus 
        [HttpPost]
        public JsonResult DocumentCountStatus()
        {
            var model = _db.PowerOfAttorneys.Where(s => s.IsDeleted == false)
                .Select(s => new
                {
                    s.PowerOfAttorneyID,
                    s.CodeForm,
                    GrantorName = s.User_GrantorID.FullName,
                    s.AttorneyID,
                    AttorneyName = s.User_AttorneyID.FullName,
                    CreationDate = s.CreationDate.ToString("dd-MM-yyyy"),
                    s.Status,
                    s.IsDeleted
                })
                .ToList();

            if (User.GetRole_AdministratorIsActive() == "True")
            {
                var CountStatus = model
                    .ToList();

                return Json(new
                {
                    waitapproval = CountStatus.Where(s => s.Status == "รอการอนุมัติ").Count(),
                    approved = CountStatus.Where(s => s.Status == "อนุมัติเรียบร้อย").Count(),
                    notapproved = CountStatus.Where(s => s.Status == "ไม่อนุมัติ").Count(),
                    canceled = CountStatus.Where(s => s.IsDeleted == true).Count()
                });
            }
            else
            {
                var CountStatus = model
                    .Where(s => s.AttorneyID == int.Parse(User.GetLoggedInUserID()))
                    .ToList();
                return Json(new
                {
                    waitapproved = CountStatus.Where(s => s.Status == "รอการอนุมัติ").Count(),
                    approved = CountStatus.Where(s => s.Status == "อนุมัติเรียบร้อย").Count(),
                    notapproved = CountStatus.Where(s => s.Status == "ไม่อนุมัติ").Count(),
                    canceled = CountStatus.Where(s => s.IsDeleted == true).Count()
                });
            }
        }
        #endregion

        #region DocumentCountStatusApprove
        [HttpPost]
        public JsonResult DocumentCountStatusApprove()
        {
            var model = _db.PowerOfAttorneys.Where(s => s.IsDeleted == false)
                .Select(s => new
                {
                    s.PowerOfAttorneyID,
                    s.CodeForm,
                    GrantorName = s.User_GrantorID.FullName,
                    s.AttorneyID,
                    AttorneyName = s.User_AttorneyID.FullName,
                    CreationDate = s.CreationDate.ToString("dd-MM-yyyy"),
                    s.Status
                })
                .ToList();

            if (User.GetRole_AdministratorIsActive() == "True")
            {
                var CountStatus = model
                    .ToList();

                return Json(new
                {
                    waitapproval = CountStatus.Where(s => s.Status == "รอการอนุมัติ").Count(),
                    approved = CountStatus.Where(s => s.Status == "อนุมัติเรียบร้อย").Count(),
                    notapproved = CountStatus.Where(s => s.Status == "ไม่อนุมัติ").Count(),
                });
            }
            else
            {
                var CountStatus = model
                    .Where(s => s.AttorneyID == int.Parse(User.GetLoggedInUserID()))
                    .ToList();
                return Json(new
                {
                    waitapproved = CountStatus.Where(s => s.Status == "รอการอนุมัติ").Count(),
                    approved = CountStatus.Where(s => s.Status == "อนุมัติเรียบร้อย").Count(),
                    notapproved = CountStatus.Where(s => s.Status == "ไม่อนุมัติ").Count(),
                });
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
                        GrantorDateApprove = s.GrantorDateApprove.HasValue ? s.GrantorDateApprove.Value.ToString("dd/MM/yyyy") : null,
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

        #region Report V.1
        [HttpGet]
        public IActionResult ReportForm(string CodeForm)
        {
            var ModelPA = _db.PowerOfAttorneys.Where(s => s.CodeForm == CodeForm)
                .Select(s => new
                {
                    s.PowerOfAttorneyID,
                    s.CodeForm,
                    CreationDate = s.CreationDate,
                    Location = s.User_GrantorID.Company.CompanyAddress,
                    GrantorName = s.User_GrantorID.FullName,
                    GrantorCardID = s.User_GrantorID.CardID,
                    AttorneyName = s.User_AttorneyID.FullName,
                    AttorneyCardID = s.User_AttorneyID.CardID,
                    AttorneyLocation = s.User_AttorneyID.Company.CompanyAddress,
                    s.GrantorApprovalStatus,
                    s.AttorneyApprovalStatus,
                    Witness1Name = s.User_WitnessApprovalBy1.FullName,
                    Witness1Signature = s.User_WitnessApprovalBy1.CardID,
                    s.WitnessApprovalStatus1,
                    Witness2Name = s.User_WitnessApprovalBy2.FullName,
                    Witness2Signature = s.User_WitnessApprovalBy2.CardID,
                    s.WitnessApprovalStatus2,
                    GrantorDateApprove = s.GrantorDateApprove.HasValue ? s.GrantorDateApprove.Value.ToString("ddMMyyyy") : null,
                    AttorneyDateApprove = s.AttorneyDateApprove.HasValue ? s.AttorneyDateApprove.Value.ToString("ddMMyyyy") : null,
                    WitnessDateApprove1 = s.WitnessDateApprove1.HasValue ? s.WitnessDateApprove1.Value.ToString("ddMMyyyy") : null,
                    WitnessDateApprove2 = s.WitnessDateApprove2.HasValue ? s.WitnessDateApprove2.Value.ToString("ddMMyyyy") : null,
                    s.Status
                })
                .FirstOrDefault();

            // Generate QR Code
            string hostname = $"{Request.Scheme}://{Request.Host}";
            string qrCodeUrl = $"{hostname}/PowerOfAttorney/ReportForm?CodeForm={CodeForm}";
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrCodeUrl, QRCodeGenerator.ECCLevel.Q);

            // ใช้ PngByteQRCode แทน QRCode
            PngByteQRCode pngByteQRCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = pngByteQRCode.GetGraphic(20);

            // แปลงเป็น Base64
            string base64Image = Convert.ToBase64String(qrCodeImage);

            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "qr-code.png");
            System.IO.File.WriteAllBytes(filePath, qrCodeImage);

            string renderFormat = "PDF";
            string mimetype = "application/pdf";
            using var report = new LocalReport();
            report.ReportPath = $"{this._webHostEnvironment.WebRootPath}\\Report\\PowerOfAttorney\\PA-001.rdlc";
            report.EnableExternalImages = true;

            ReportParameter[] parameters = new ReportParameter[]
            {
                new ReportParameter("CodeForm", CodeForm),
                new ReportParameter("CreationDate", ModelPA.CreationDate.ToThaiDate()),
                new ReportParameter("Location", ModelPA.Location),
                new ReportParameter("GrantorName", ModelPA.GrantorName),
                new ReportParameter("GrantorCardID", ModelPA.GrantorCardID),
                new ReportParameter("GrantorLocation", ModelPA.Location),
                new ReportParameter("AttorneyName", ModelPA.AttorneyName),
                new ReportParameter("AttorneyCardID", ModelPA.AttorneyCardID),
                new ReportParameter("AttorneyLocation", ModelPA.AttorneyLocation),
                new ReportParameter("Witness1Name", ModelPA.Witness1Name),
                new ReportParameter("Witness2Name", ModelPA.Witness2Name),
                new ReportParameter("GrantorDateApprove", ModelPA.GrantorDateApprove),
                new ReportParameter("AttorneyDateApprove", ModelPA.AttorneyDateApprove),
                new ReportParameter("WitnessDateApprove1", ModelPA.WitnessDateApprove1),
                new ReportParameter("WitnessDateApprove2", ModelPA.WitnessDateApprove2),
                new ReportParameter("GrantorSignature", System.IO.File.Exists($"{this._webHostEnvironment.WebRootPath}\\assets\\SystemImages\\Signature\\" + ModelPA.GrantorCardID + ".jpg") == false ? "" : getImageFromPath($"{this._webHostEnvironment.WebRootPath}\\assets\\SystemImages\\Signature\\" + ModelPA.GrantorCardID + ".jpg")),
                new ReportParameter("AttorneySignature", System.IO.File.Exists($"{this._webHostEnvironment.WebRootPath}\\assets\\SystemImages\\Signature\\" + ModelPA.AttorneyCardID + ".jpg") == false ? "" : getImageFromPath($"{this._webHostEnvironment.WebRootPath}\\assets\\SystemImages\\Signature\\" + ModelPA.AttorneyCardID + ".jpg")),
                new ReportParameter("Witness1Signature", System.IO.File.Exists($"{this._webHostEnvironment.WebRootPath}\\assets\\SystemImages\\Signature\\" + ModelPA.Witness1Signature + ".jpg") == false ? "" : getImageFromPath($"{this._webHostEnvironment.WebRootPath}\\assets\\SystemImages\\Signature\\" + ModelPA.Witness1Signature + ".jpg")),
                new ReportParameter("Witness2Signature", System.IO.File.Exists($"{this._webHostEnvironment.WebRootPath}\\assets\\SystemImages\\Signature\\" + ModelPA.Witness2Signature + ".jpg") == false ? "" : getImageFromPath($"{this._webHostEnvironment.WebRootPath}\\assets\\SystemImages\\Signature\\" + ModelPA.Witness2Signature + ".jpg")),
                new ReportParameter("QRCode", new Uri(filePath).AbsoluteUri)
            };

            report.SetParameters(parameters);

            var pdf = report.Render(format: renderFormat);

            var contentDisposition = new System.Net.Mime.ContentDisposition
            {
                FileName = CodeForm + ".pdf",
                Inline = true // false = prompt the user for downloading; true = browser to try to show the content inline
            };

            return new FileContentResult(pdf, mimetype);
        }
        #endregion



        #region ApprovePowerOfAttorney
        [HttpPost]
        public async Task<IActionResult> ApprovePowerOfAttorney(int PowerOfAttorneyID)
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

                    if (PowerOfAttorney.GrantorApprovalBy == int.Parse(User.GetLoggedInUserID()))
                    {
                        PowerOfAttorney.GrantorApprovalStatus = "อนุมัติเรียบร้อย";
                        PowerOfAttorney.GrantorApprovalBy = int.Parse(User.GetLoggedInUserID());
                        PowerOfAttorney.GrantorDateApprove = DateTime.Now;
                    }
                    else if (PowerOfAttorney.AttorneyApprovalBy == int.Parse(User.GetLoggedInUserID()))
                    {
                        PowerOfAttorney.AttorneyApprovalStatus = "อนุมัติเรียบร้อย";
                        PowerOfAttorney.AttorneyApprovalBy = int.Parse(User.GetLoggedInUserID());
                        PowerOfAttorney.AttorneyDateApprove = DateTime.Now;
                    }
                    else if (PowerOfAttorney.WitnessApprovalBy1 == int.Parse(User.GetLoggedInUserID()))
                    {
                        PowerOfAttorney.WitnessApprovalStatus1 = "อนุมัติเรียบร้อย";
                        PowerOfAttorney.WitnessDateApprove1 = DateTime.Now;
                    }
                    else if (PowerOfAttorney.WitnessApprovalBy2 == int.Parse(User.GetLoggedInUserID()))
                    {
                        PowerOfAttorney.WitnessApprovalStatus2 = "อนุมัติเรียบร้อย";
                        PowerOfAttorney.WitnessDateApprove2 = DateTime.Now;
                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            message = "ไม่พบสถานะการอนุมัติ"
                        });
                    }
                }

                PowerOfAttorney.Status = "อนุมัติเรียบร้อย";
                PowerOfAttorney.UpdatedAt = DateTime.Now;
                PowerOfAttorney.UserManageID = int.Parse(User.GetLoggedInUserID());
                await _db.SaveChangesAsync();
                // Log the approval of the PowerOfAttorney
                var logEntry = new LogSystemData
                {
                    TableName = "PowerOfAttorneys",
                    Action = "Approve",
                    RecordID = PowerOfAttorney.PowerOfAttorneyID, // Assuming PowerOfAttorneyID is the primary key
                    UserManageID = int.Parse(User.GetLoggedInUserID()), // Retrieve the logged user's ID
                    ActionTime = DateTime.Now,
                    IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                    OldValue = $"CreationDate: {PowerOfAttorney.CreationDate}, CompanyID: {PowerOfAttorney.CompanyID}, GrantorID: {PowerOfAttorney.GrantorID}, WitnessApprovalBy1: {PowerOfAttorney.WitnessApprovalBy1}, WitnessApprovalBy2: {PowerOfAttorney.WitnessApprovalBy2}", // Previous value before approval
                    NewValue = $"CreationDate: {PowerOfAttorney.CreationDate}, CompanyID: {PowerOfAttorney.CompanyID}, GrantorID: {PowerOfAttorney.GrantorID}, WitnessApprovalBy1: {PowerOfAttorney.WitnessApprovalBy1}, WitnessApprovalBy2: {PowerOfAttorney.WitnessApprovalBy2}", // New value after approval
                    Description = $"Approved PowerOfAttorney with CreationDate: {PowerOfAttorney.CreationDate}, CompanyID: {PowerOfAttorney.CompanyID}, GrantorID: {PowerOfAttorney.GrantorID}, WitnessApprovalBy1: {PowerOfAttorney.WitnessApprovalBy1}, WitnessApprovalBy2: {PowerOfAttorney.WitnessApprovalBy2}"
                };
                // Save the log entry
                _db.LogSystemDatas.Add(logEntry);
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "อนุมัติคำขอเรียบร้อย" });
            }
        }
        #endregion

        #region NotApprovePowerOfAttorney
        [HttpPost]
        public async Task<IActionResult> NotApprovePowerOfAttorney(int PowerOfAttorneyID)
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
                    if (PowerOfAttorney.GrantorApprovalBy == int.Parse(User.GetLoggedInUserID()))
                    {
                        PowerOfAttorney.GrantorApprovalStatus = "ไม่อนุมัติ";
                        PowerOfAttorney.GrantorApprovalBy = int.Parse(User.GetLoggedInUserID());
                        PowerOfAttorney.GrantorDateApprove = DateTime.Now;
                    }
                    else if (PowerOfAttorney.AttorneyApprovalBy == int.Parse(User.GetLoggedInUserID()))
                    {
                        PowerOfAttorney.AttorneyApprovalStatus = "ไม่อนุมัติ";
                        PowerOfAttorney.AttorneyApprovalBy = int.Parse(User.GetLoggedInUserID());
                        PowerOfAttorney.AttorneyDateApprove = DateTime.Now;
                    }
                    else if (PowerOfAttorney.WitnessApprovalBy1 == int.Parse(User.GetLoggedInUserID()))
                    {
                        PowerOfAttorney.WitnessApprovalStatus1 = "ไม่อนุมัติ";
                        PowerOfAttorney.WitnessDateApprove1 = DateTime.Now;
                    }
                    else if (PowerOfAttorney.WitnessApprovalBy2 == int.Parse(User.GetLoggedInUserID()))
                    {
                        PowerOfAttorney.WitnessApprovalStatus2 = "ไม่อนุมัติ";
                        PowerOfAttorney.WitnessDateApprove2 = DateTime.Now;
                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            message = "ไม่พบสถานะการอนุมัติ"
                        });
                    }

                    PowerOfAttorney.Status = "ไม่อนุมัติ";
                    PowerOfAttorney.UpdatedAt = DateTime.Now;
                    PowerOfAttorney.UserManageID = int.Parse(User.GetLoggedInUserID());
                    await _db.SaveChangesAsync();
                    // Log the not approval of the
                    var logEntry = new LogSystemData
                    {
                        TableName = "PowerOfAttorneys",
                        Action = "NotApprove",
                        RecordID = PowerOfAttorney.PowerOfAttorneyID, // Assuming PowerOfAttorneyID is the primary key
                        UserManageID = int.Parse(User.GetLoggedInUserID()), // Retrieve the logged user's ID
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"CreationDate: {PowerOfAttorney.CreationDate}, CompanyID: {PowerOfAttorney.CompanyID}, GrantorID: {PowerOfAttorney.GrantorID}, WitnessApprovalBy1: {PowerOfAttorney.WitnessApprovalBy1}, WitnessApprovalBy2: {PowerOfAttorney.WitnessApprovalBy2}", // Previous value before not approval
                        NewValue = $"CreationDate: {PowerOfAttorney.CreationDate}, CompanyID: {PowerOfAttorney.CompanyID}, GrantorID: {PowerOfAttorney.GrantorID}, WitnessApprovalBy1: {PowerOfAttorney.WitnessApprovalBy1}, WitnessApprovalBy2: {PowerOfAttorney.WitnessApprovalBy2}", // New value after not approval
                        Description = $"Not Approved PowerOfAttorney with CreationDate: {PowerOfAttorney.CreationDate}, CompanyID: {PowerOfAttorney.CompanyID}, GrantorID: {PowerOfAttorney.GrantorID}, WitnessApprovalBy1: {PowerOfAttorney.WitnessApprovalBy1}, WitnessApprovalBy2: {PowerOfAttorney.WitnessApprovalBy2}"
                    };
                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();
                    return Json(new { success = true, message = "ไม่อนุมัติคำขอเรียบร้อย" });
                }
            }
        }
        #endregion


        public string getImageFromPath(string imagePath)
        {
            string imgFile = "";
#pragma warning disable CA1416 // Validate platform compatibility
            using (var b = new Bitmap(imagePath))
            {
                using (var ms = new MemoryStream())
                {
                    b.Save(ms, ImageFormat.Bmp);
                    imgFile = Convert.ToBase64String(ms.ToArray());
                }
            }
#pragma warning restore CA1416 // Validate platform compatibility
            return imgFile;
        }

    }
}
