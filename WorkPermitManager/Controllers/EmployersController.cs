using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using WorkPermitManager.Data;
using WorkPermitManager.Helpers;
using WorkPermitManager.Models;

namespace WorkPermitManager.Controllers
{
    public class EmployersController : Controller
    {
        private readonly Db_WorkPermitManagerModel _db;

        public EmployersController(Db_WorkPermitManagerModel db)
        {
            _db = db;
        }

        #region Employers
        public IActionResult EmployersPage(string SearchData)
        {
            var employers = _db.Employers.Where(s => s.IsActive).ToList();

            if (!string.IsNullOrEmpty(SearchData))
            {
                employers = employers.Where(s => s.NameTh.Contains(SearchData) || s.NameEng.Contains(SearchData)).ToList();
            }

            ViewBag.SearchData = SearchData;
            ViewBag.EmployerList = employers;
            ViewBag.BusinessTypeList = _db.BusinessTypes.Where(s => s.IsActive).ToList();

            return View();
        }

        #region Create Employer
        [HttpPost]
        public async Task<IActionResult> CreateEmployer(RequestEmployerModel model)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("CreateEmployers"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (string.IsNullOrEmpty(model.NameTh) || string.IsNullOrEmpty(model.NameEng) || model.BusinesstypeID == 0)
            {
                return NotFound();
            }
            else
            {

                Employer createModel = new Employer
                {
                    NameTh = model.NameTh,
                    NameEng = model.NameEng,
                    BusinesstypeID = model.BusinesstypeID,
                    CardID = model.CardID,
                    RegistrationNumber = model.RegistrationNumber,
                    RegistrationDate = model.RegistrationDate,
                    RegisteredCapital = model.RegisteredCapital,
                    JobTypeName = model.JobTypeName,
                    JobDiscription = model.JobDiscription,
                    DirectorNameTh = model.DirectorNameTh,
                    DirectorNameEng = model.DirectorNameEng,
                    DirectorPositionTh = model.DirectorPositionTh,
                    DirectorPositionEng = model.DirectorPositionEng,
                    OfficerNameOne = model.OfficerNameOne,
                    OfficerPhoneOne = model.OfficerNameOne,
                    OfficerNameTwo = model.OfficerNameTwo,
                    OfficerPhoneTwo = model.OfficerNameTwo,
                    HouseRecordNumber = model.HouseRecordNumber,
                    HouseNo = model.HouseNo,
                    Soi = model.Soi,
                    Road = model.Road,
                    SubdistrictTh = model.SubdistrictTh,
                    DistrictTh = model.DistrictTh,
                    ProvinceTh = model.ProvinceTh,
                    Postcode = model.Postcode,
                    SubdistrictEng = model.SubdistrictEng,
                    DistrictEng = model.DistrictEng,
                    ProvinceEng = model.ProvinceEng,
                    Phone = model.Phone,
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    UserManageID = int.Parse(User.GetLoggedInUserID())
                };

                // Processing the Employer creation
                _db.Employers.Add(createModel);
                await _db.SaveChangesAsync();

                // Log the creation of the Employer
                var logEntry = new LogSystemData
                {
                    TableName = "Employers",
                    Action = "Create",
                    RecordID = createModel.EmployerID,
                    UserManageID = int.Parse(User.GetLoggedInUserID()),
                    ActionTime = DateTime.Now,
                    IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                    OldValue = null,
                    NewValue = $"EmployerID: {createModel.EmployerID}, NameTh: {createModel.NameTh}, NameEng: {createModel.NameEng}",
                    Description = $"Created new employer with ID: {createModel.EmployerID}"
                };

                // Save the log entry
                _db.LogSystemDatas.Add(logEntry);
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
        }
        #endregion

        #region Delete Employer
        [HttpPost]
        public async Task<IActionResult> DeleteEmployer(int EmployerID)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("DeleteEmployers"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (EmployerID == 0)
            {
                return NotFound();
            }
            else
            {
                var model = _db.Employers.FirstOrDefault(p => p.EmployerID == EmployerID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    model.IsActive = false;
                    model.UpdatedAt = DateTime.Now;
                    model.UserManageID = int.Parse(User.GetLoggedInUserID());

                    // Processing the Employer deletion
                    _db.Employers.Update(model);
                    await _db.SaveChangesAsync();

                    // Log the deletion of the Employer
                    var logEntry = new LogSystemData
                    {
                        TableName = "Employers",
                        Action = "Delete",
                        RecordID = model.EmployerID,
                        UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"EmployerID: {model.EmployerID}, NameTh: {model.NameTh}, NameEng: {model.NameEng}",
                        NewValue = null,
                        Description = $"Deleted employer with Code: {model.EmployerID}"
                    };

                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();

                    return Json(new { success = true });
                }
            }
        }
        #endregion

        #region Update Employer
        [HttpPost]
        public async Task<IActionResult> UpdateEmployer(RequestEmployerModel model)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("UpdateEmployers"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (model.EmployerID == 0 || string.IsNullOrEmpty(model.NameTh) || string.IsNullOrEmpty(model.NameEng) || model.BusinesstypeID == 0 || string.IsNullOrEmpty(model.RegistrationNumber) || model.RegisteredCapital <= 0)
            {
                return NotFound();
            }
            else
            {
                var data = _db.Employers.FirstOrDefault(p => p.EmployerID == model.EmployerID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    var oldValues = new
                    {
                        data.NameTh,
                        data.NameEng,
                        data.BusinesstypeID,
                        data.RegistrationNumber,
                        data.RegistrationDate,
                        data.RegisteredCapital,
                        data.JobTypeName,
                        data.JobDiscription,
                        data.DirectorNameTh,
                        data.DirectorNameEng,
                        data.DirectorPositionTh,
                        data.DirectorPositionEng,
                        data.OfficerNameOne,
                        data.OfficerPhoneOne,
                        data.OfficerNameTwo,
                        data.OfficerPhoneTwo,
                        data.HouseRecordNumber,
                        data.HouseNo,
                        data.Soi,
                        data.Road,
                        data.SubdistrictTh,
                        data.DistrictTh,
                        data.ProvinceTh,
                        data.Postcode,
                        data.SubdistrictEng,
                        data.DistrictEng,
                        data.ProvinceEng,
                        data.Phone
                    };

                    data.NameTh = model.NameTh;
                    data.NameEng = model.NameEng;
                    data.BusinesstypeID = model.BusinesstypeID;
                    data.CardID = model.CardID;
                    data.RegistrationNumber = model.RegistrationNumber;
                    data.RegistrationDate = model.RegistrationDate;
                    data.RegisteredCapital = model.RegisteredCapital;
                    data.JobTypeName = model.JobTypeName;
                    data.JobDiscription = model.JobDiscription;
                    data.DirectorNameTh = model.DirectorNameTh;
                    data.DirectorNameEng = model.DirectorNameEng;
                    data.DirectorPositionTh = model.DirectorPositionTh;
                    data.DirectorPositionEng = model.DirectorPositionEng;
                    data.OfficerNameOne = model.OfficerNameOne;
                    data.OfficerPhoneOne = model.OfficerNameOne;
                    data.OfficerNameTwo = model.OfficerNameTwo;
                    data.OfficerPhoneTwo = model.OfficerPhoneTwo;
                    data.HouseRecordNumber = model.HouseRecordNumber;
                    data.HouseNo = model.HouseNo;
                    data.Soi = model.Soi;
                    data.Road = model.Road;
                    data.SubdistrictTh = model.SubdistrictTh;
                    data.DistrictTh = model.DistrictTh;
                    data.ProvinceTh = model.ProvinceTh;
                    data.Postcode = model.Postcode;
                    data.SubdistrictEng = model.SubdistrictEng;
                    data.DistrictEng = model.DistrictEng;
                    data.ProvinceEng = model.ProvinceEng;
                    data.Phone = model.Phone;
                    data.UpdatedAt = DateTime.Now;
                    data.UserManageID = int.Parse(User.GetLoggedInUserID());

                    // Processing the Employer update
                    _db.Employers.Update(data);
                    await _db.SaveChangesAsync();

                    // Log the update of the Employer
                    var logEntry = new LogSystemData
                    {
                        TableName = "Employers",
                        Action = "Update",
                        RecordID = data.EmployerID,
                        UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"NameTh: {oldValues.NameTh}, NameEng: {oldValues.NameEng}, BusinesstypeID: {oldValues.BusinesstypeID}, RegistrationNumber: {oldValues.RegistrationNumber}, RegistrationDate: {oldValues.RegistrationDate}, RegisteredCapital: {oldValues.RegisteredCapital}, DirectorNameTh: {oldValues.DirectorNameTh}, DirectorNameEng: {oldValues.DirectorNameEng}, DirectorPositionTh: {oldValues.DirectorPositionTh}, DirectorPositionEng: {oldValues.DirectorPositionEng}, JobTypeName: {oldValues.JobTypeName}, JobDiscription: {oldValues.JobDiscription}, HouseNo: {oldValues.HouseNo}, Soi: {oldValues.Soi}, Road: {oldValues.Road}, SubdistrictTh: {oldValues.SubdistrictTh}, DistrictTh: {oldValues.DistrictTh}, ProvinceTh: {oldValues.ProvinceTh}, Postcode: {oldValues.Postcode}, SubdistrictEng: {oldValues.SubdistrictEng}, DistrictEng: {oldValues.DistrictEng}, ProvinceEng: {oldValues.ProvinceEng}, Phone: {oldValues.Phone}",
                        NewValue = $"NameTh: {model.NameTh}, NameEng: {model.NameEng}, BusinesstypeID: {model.BusinesstypeID}, RegistrationNumber: {model.RegistrationNumber}, RegistrationDate: {model.RegistrationDate}, RegisteredCapital: {model.RegisteredCapital}, DirectorNameTh: {model.DirectorNameTh}, DirectorNameEng: {model.DirectorNameEng}, DirectorPositionTh: {model.DirectorPositionTh}, DirectorPositionEng: {model.DirectorPositionEng}, JobTypeName: {model.JobTypeName}, JobDiscription: {model.JobDiscription}, HouseNo: {model.HouseNo}, Soi: {model.Soi}, Road: {model.Road}, SubdistrictTh: {model.SubdistrictTh}, DistrictTh: {model.DistrictTh}, ProvinceTh: {model.ProvinceTh}, Postcode: {model.Postcode}, SubdistrictEng: {model.SubdistrictEng}, DistrictEng: {model.DistrictEng}, ProvinceEng: {model.ProvinceEng}, Phone: {model.Phone}",
                        Description = $"Updated employer with ID: {model.EmployerID}"
                    };

                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();

                    return Json(new { success = true });
                }
            }
        }
        #endregion

        #region Check Employer Name
        [HttpPost]
        public JsonResult CheckEmployerName(string EmployerName)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("ReadEmployers"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            var model = _db.Employers.FirstOrDefault(p => p.NameTh == EmployerName);
            if (model != null)
            {
                return Json(new { success = false, message = "ไม่สามารถบันทึกข้อมูลเนื่องจากมีข้อมูลนี้อยู่แล้ว" });
            }
            else
            {
                return Json(new { success = true });
            }
        }
        #endregion

        #region UploadExcel
        public IActionResult DownloadSampleExcel()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Set the license context

            var stream = new MemoryStream();

            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("Sample");

                // Add headers
                worksheet.Cells[1, 1].Value = "ชื่อนายจ้าง (ไทย)";
                worksheet.Cells[1, 2].Value = "ชื่อนายจ้าง (อังกฤษ)";
                worksheet.Cells[1, 3].Value = "ประเภทธุรกิจ";
                worksheet.Cells[1, 4].Value = "เลขประจำตัวประชาชน";
                worksheet.Cells[1, 5].Value = "เลขทะเบียนนิติบุคคล";
                worksheet.Cells[1, 6].Value = "วันที่จดทะเบียน";
                worksheet.Cells[1, 7].Value = "ทุนจดทะเบียน";
                worksheet.Cells[1, 8].Value = "ชื่อกรรมการผู้มีอำนาจลงนาม (ไทย)";
                worksheet.Cells[1, 9].Value = "ชื่อกรรมการผู้มีอำนาจลงนาม (อังกฤษ)";
                worksheet.Cells[1, 10].Value = "ตำแหน่งผู้มีอำนาจลงนาม (ไทย)";
                worksheet.Cells[1, 11].Value = "ตำแหน่งผู้มีอำนาจลงนาม (อังกฤษ)";
                worksheet.Cells[1, 12].Value = "ประเภทงาน";
                worksheet.Cells[1, 13].Value = "รายละเอียดงาน";
                worksheet.Cells[1, 14].Value = "เจ้าหน้าที่คนที่ 1";
                worksheet.Cells[1, 15].Value = "เบอร์โทรศัพท์เจ้าหน้าที่คนที่ 1";
                worksheet.Cells[1, 16].Value = "เจ้าหน้าที่คนที่ 2";
                worksheet.Cells[1, 17].Value = "เบอร์โทรศัพท์เจ้าหน้าที่คนที่ 2";
                worksheet.Cells[1, 18].Value = "เลขรหัสประจำบ้าน";
                worksheet.Cells[1, 19].Value = "บ้านเลขที่";
                worksheet.Cells[1, 20].Value = "ซอย";
                worksheet.Cells[1, 21].Value = "ถนน";
                worksheet.Cells[1, 22].Value = "ตำบล (ไทย)";
                worksheet.Cells[1, 23].Value = "อำเภอ (ไทย)";
                worksheet.Cells[1, 24].Value = "จังหวัด (ไทย)";
                worksheet.Cells[1, 25].Value = "รหัสไปรษณีย์";
                worksheet.Cells[1, 26].Value = "ตำบล (อังกฤษ)";
                worksheet.Cells[1, 27].Value = "อำเภอ (อังกฤษ)";
                worksheet.Cells[1, 28].Value = "จังหวัด (อังกฤษ)";
                worksheet.Cells[1, 29].Value = "เบอร์โทรศัพท์";

                // Format header
                using (var range = worksheet.Cells[1, 1, 1, 29])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                package.Save();
            }

            stream.Position = 0;
            var fileName = "SampleEmployerData.xlsx";
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(stream, contentType, fileName);
        }

        [HttpPost]
        public async Task<IActionResult> UploadExcel(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                return Json(new { success = false, message = "กรุณาเลือกไฟล์ Excel" });
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await excelFile.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            return Json(new { success = false, message = "ไม่พบข้อมูลในไฟล์ Excel" });
                        }

                        var rowCount = worksheet.Dimension.Rows;
                        var employers = new List<Employer>();

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var employer = new Employer
                            {
                                NameTh = worksheet.Cells[row, 1].Text,
                                NameEng = worksheet.Cells[row, 2].Text,
                                BusinesstypeID = int.Parse(worksheet.Cells[row, 3].Text),
                                CardID = worksheet.Cells[row, 4].Text,
                                RegistrationNumber = worksheet.Cells[row, 5].Text,
                                RegistrationDate = worksheet.Cells[row, 6].Text,
                                RegisteredCapital = decimal.Parse(worksheet.Cells[row, 7].Text),
                                DirectorNameTh = worksheet.Cells[row, 8].Text,
                                DirectorNameEng = worksheet.Cells[row, 9].Text,
                                DirectorPositionTh = worksheet.Cells[row, 10].Text,
                                DirectorPositionEng = worksheet.Cells[row, 11].Text,
                                JobTypeName = worksheet.Cells[row, 12].Text,
                                JobDiscription = worksheet.Cells[row, 13].Text,
                                OfficerNameOne = worksheet.Cells[row, 14].Text,
                                OfficerPhoneOne = worksheet.Cells[row, 15].Text,
                                OfficerNameTwo = worksheet.Cells[row, 16].Text,
                                OfficerPhoneTwo = worksheet.Cells[row, 17].Text,
                                HouseRecordNumber = worksheet.Cells[row, 18].Text,
                                HouseNo = worksheet.Cells[row, 19].Text,
                                Soi = worksheet.Cells[row, 20].Text,
                                Road = worksheet.Cells[row, 21].Text,
                                SubdistrictTh = worksheet.Cells[row, 22].Text,
                                DistrictTh = worksheet.Cells[row, 23].Text,
                                ProvinceTh = worksheet.Cells[row, 24].Text,
                                Postcode = worksheet.Cells[row, 25].Text,
                                SubdistrictEng = worksheet.Cells[row, 26].Text,
                                DistrictEng = worksheet.Cells[row, 27].Text,
                                ProvinceEng = worksheet.Cells[row, 28].Text,
                                Phone = worksheet.Cells[row, 29].Text,
                                CreatedAt = DateTime.Now,
                                IsActive = true,
                                UserManageID = int.Parse(User.GetLoggedInUserID())
                            };

                            employers.Add(employer);
                        }

                        _db.Employers.AddRange(employers);
                        await _db.SaveChangesAsync();
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการอัพโหลดไฟล์: " + ex.Message });
            }
        }
        #endregion

        #region Get Employer Details
        [HttpPost]
        public JsonResult GetEmployerDetails(int EmployerID)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("ReadEmployers"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (EmployerID == 0)
            {
                return Json(new { success = false, message = "Employer ID is required" });
            }
            else
            {
                var model = _db.Employers
                    .Where(p => p.EmployerID == EmployerID)
                    .Select(s => new
                    {
                        s.EmployerID,
                        s.NameTh,
                        s.NameEng,
                        BusinesstypeTh = s.BusinessType.BusinesstypeTh,
                        s.CardID,
                        s.RegistrationNumber,
                        s.RegistrationDate,
                        s.RegisteredCapital,
                        s.JobTypeName,
                        s.JobDiscription,
                        s.DirectorNameTh,
                        s.DirectorNameEng,
                        s.DirectorPositionTh,
                        s.DirectorPositionEng,
                        s.OfficerNameOne,
                        s.OfficerPhoneOne,
                        s.OfficerNameTwo,
                        s.OfficerPhoneTwo,
                        s.HouseRecordNumber,
                        s.HouseNo,
                        s.Soi,
                        s.Road,
                        s.SubdistrictTh,
                        s.DistrictTh,
                        s.ProvinceTh,
                        s.Postcode,
                        s.SubdistrictEng,
                        s.DistrictEng,
                        s.ProvinceEng,
                        s.Phone

                    })
                    .FirstOrDefault();
                if (model == null)
                {
                    return Json(new { success = false, message = "Employer not found" });
                }
                else
                {
                    return Json(new { success = true, data = model });
                }
            }
        }
        #endregion
        #endregion Employers

        #region BusinessTypes Function
        public IActionResult BusinessTypes()
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("ReadEmployers"))
            {
                TempData["ErrorMessage"] = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                var businessTypes = _db.BusinessTypes.Where(s => s.IsActive).ToList();
                ViewBag.BusinessTypeList = businessTypes;
            }

            return View();
        }

        #region Create BusinessType
        [HttpPost]
        public async Task<IActionResult> CreateBusinessType(string BusinesstypeTh, string BusinesstypeEng)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("CreateEmployers"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (string.IsNullOrEmpty(BusinesstypeTh) || string.IsNullOrEmpty(BusinesstypeEng))
            {
                return NotFound();
            }
            else
            {
                BusinessType createModel = new BusinessType
                {
                    BusinesstypeTh = BusinesstypeTh,
                    BusinesstypeEng = BusinesstypeEng,
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    UserManageID = int.Parse(User.GetLoggedInUserID())
                };

                // Processing the BusinessType creation
                _db.BusinessTypes.Add(createModel);
                await _db.SaveChangesAsync();

                // Log the creation of the BusinessType
                var logEntry = new LogSystemData
                {
                    TableName = "BusinessTypes",
                    Action = "Create",
                    RecordID = createModel.BusinesstypeID,
                    UserManageID = int.Parse(User.GetLoggedInUserID()),
                    ActionTime = DateTime.Now,
                    IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                    OldValue = null,
                    NewValue = $"BusinesstypeTh: {createModel.BusinesstypeTh}, BusinesstypeEng: {createModel.BusinesstypeEng}",
                    Description = $"Created new business type with Name: {createModel.BusinesstypeTh}"
                };

                // Save the log entry
                _db.LogSystemDatas.Add(logEntry);
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
        }
        #endregion

        #region Delete BusinessType
        [HttpPost]
        public async Task<IActionResult> DeleteBusinessType(int BusinesstypeID)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("DeleteEmployers"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (BusinesstypeID == 0)
            {
                return NotFound();
            }
            else
            {
                var model = _db.BusinessTypes.FirstOrDefault(p => p.BusinesstypeID == BusinesstypeID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    model.IsActive = true;
                    model.UpdatedAt = DateTime.Now;
                    model.UserManageID = int.Parse(User.GetLoggedInUserID());

                    // Processing the BusinessType deletion
                    _db.BusinessTypes.Update(model);
                    await _db.SaveChangesAsync();

                    // Log the deletion of the BusinessType
                    var logEntry = new LogSystemData
                    {
                        TableName = "BusinessTypes",
                        Action = "Delete",
                        RecordID = model.BusinesstypeID,
                        UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"BusinesstypeTh: {model.BusinesstypeTh}, BusinesstypeEng: {model.BusinesstypeEng}",
                        NewValue = null,
                        Description = $"Deleted business type with Name: {model.BusinesstypeTh}"
                    };

                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();

                    return Json(new { success = true });
                }
            }
        }
        #endregion

        #region Update BusinessType
        [HttpPost]
        public async Task<IActionResult> UpdateBusinessType(int BusinesstypeID, string BusinesstypeTh, string BusinesstypeEng)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("UpdateEmployers"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (BusinesstypeID == 0 || string.IsNullOrEmpty(BusinesstypeTh) || string.IsNullOrEmpty(BusinesstypeEng))
            {
                return NotFound();
            }
            else
            {
                var model = _db.BusinessTypes.FirstOrDefault(p => p.BusinesstypeID == BusinesstypeID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    var oldTh = model.BusinesstypeTh;
                    var oldEng = model.BusinesstypeEng;

                    model.BusinesstypeTh = BusinesstypeTh;
                    model.BusinesstypeEng = BusinesstypeEng;
                    model.UpdatedAt = DateTime.Now;
                    model.UserManageID = int.Parse(User.GetLoggedInUserID());

                    // Processing the BusinessType update
                    _db.BusinessTypes.Update(model);
                    await _db.SaveChangesAsync();

                    // Log the update of the BusinessType
                    var logEntry = new LogSystemData
                    {
                        TableName = "BusinessTypes",
                        Action = "Update",
                        RecordID = model.BusinesstypeID,
                        UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"BusinesstypeTh: {oldTh}, BusinesstypeEng: {oldEng}",
                        NewValue = $"BusinesstypeTh: {model.BusinesstypeTh}, BusinesstypeEng: {model.BusinesstypeEng}",
                        Description = $"Updated business type with Name: {oldTh} to {model.BusinesstypeTh}"
                    };

                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();

                    return Json(new { success = true });
                }
            }
        }
        #endregion

        #region Check BusinessType Name
        [HttpPost]
        public JsonResult CheckBusinessTypeName(string BusinesstypeTh, string BusinesstypeEng)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("ReadEmployers"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            var model = _db.BusinessTypes.FirstOrDefault(p => p.BusinesstypeTh == BusinesstypeTh || p.BusinesstypeEng == BusinesstypeEng);
            if (model != null)
            {
                return Json(new { success = false, message = "ไม่สามารถบันทึกข้อมูลเนื่องจากมีข้อมูลนี้อยู่แล้ว" });
            }
            else
            {
                return Json(new { success = true });
            }
        }
        #endregion

        #region Get BusinessType Details
        [HttpPost]
        public JsonResult GetBusinessTypeDetails(int BusinesstypeID)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("ReadEmployers"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (BusinesstypeID == 0)
            {
                return Json("BusinessType ID is required");
            }
            else
            {
                var model = _db.BusinessTypes.FirstOrDefault(p => p.BusinesstypeID == BusinesstypeID);
                if (model == null)
                {
                    return Json("BusinessType not found");
                }
                else
                {
                    return Json(model);
                }
            }
        }
        #endregion

        #endregion BusinessTypes Function

        private List<string> GetUserPermissions(int userId)
        {
            // Fetch user permissions from the database
            var permissions = _db.UserPermissions
                .Where(p => p.UserID == userId)
                .Select(p => new
                {
                    p.FunctionName,
                    p.CanRead,
                    p.CanCreate,
                    p.CanUpdate,
                    p.CanDelete
                })
                .ToList();

            var userPermissions = new List<string>();

            foreach (var permission in permissions)
            {
                if ((bool)permission.CanRead)
                {
                    userPermissions.Add("Read" + permission.FunctionName);
                }
                if ((bool)permission.CanCreate)
                {
                    userPermissions.Add("Create" + permission.FunctionName);
                }
                if ((bool)permission.CanUpdate)
                {
                    userPermissions.Add("Update" + permission.FunctionName);
                }
                if ((bool)permission.CanDelete)
                {
                    userPermissions.Add("Delete" + permission.FunctionName);
                }
            }

            return userPermissions;
        }

    }
}
