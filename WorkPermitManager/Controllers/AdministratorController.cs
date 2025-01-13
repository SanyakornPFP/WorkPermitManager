using ContainerEvaluationSystem.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using WorkPermitManager.Data;
using WorkPermitManager.Models;

namespace WorkPermitManager.Controllers
{
    [Authorize]
    public class AdministratorController : Controller
    {
        private readonly Db_WorkPermitManagerModel _db;
        public AdministratorController(Db_WorkPermitManagerModel db)
        {
            _db = db;
        }

        #region User Functions
        public IActionResult ManageUsers()
        {
            ViewBag.UserList = _db.Users.Where(u => u.IsDeleted == false).ToList();
            ViewBag.PositionList = _db.Positions.Where(p => p.IsDeleted == false).ToList();
            ViewBag.DepartmentList = _db.Departments.Where(d => d.IsDeleted == false).ToList();
            ViewBag.CompanyList = _db.Companies.Where(c => c.IsDeleted == false).ToList();
            return View();
        }

        #region Create User
        [HttpPost]
        public async Task<IActionResult> CreateUser(string UserName, string FullName, string UserEmail, string CardID, int PositionID, int DepartmentID, int CompanyID)
        {
            if (UserName == null || UserEmail == null || PositionID == 0 || DepartmentID == 0 || CompanyID == 0)
            {
                return NotFound();
            }
            else
            {
                User Createmodel = new User
                {
                    Username = UserName,
                    FullName = FullName,
                    Email = UserEmail,
                    CardID = CardID,
                    Passwordhash = ComputeSha256Hash(CardID),
                    PositionID = PositionID,
                    DepartmentID = DepartmentID,
                    CompanyID = CompanyID,
                    CreatedDate = DateTime.Now,
                    UserManageID = int.Parse(User.GetLoggedInUserID())
                };
                // Processing the User creation
                _db.Users.Add(Createmodel);
                await _db.SaveChangesAsync();

                string[] function = ["Administrator"];
                foreach (var item in function)
                {
                    UserPermission newUserPermission = new UserPermission
                    {
                        UserID = Createmodel.UserID,
                        FunctionName = item,
                        CanRead = false,
                        CanCreate = false,
                        CanUpdate = false,
                        CanDelete = false,
                        CreatedDate = DateTime.Now,
                        UserManageID = int.Parse(User.GetLoggedInUserID())
                    };

                    _db.UserPermissions.Add(newUserPermission);
                    await _db.SaveChangesAsync();
                }

                // Log the creation of the User
                var logEntry = new LogSystemData
                {
                    TableName = "Companies",
                    Action = "Create",
                    RecordID = Createmodel.UserID, // Assuming UserID is the primary key
                    UserManageID = int.Parse(User.GetLoggedInUserID()), // Retrieve the logged user's ID
                    ActionTime = DateTime.Now,
                    IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                    OldValue = null, // No previous value since it's a new record
                    NewValue = $"Name: {Createmodel.Username}, Email: {Createmodel.Email}, Position: {Createmodel.PositionID}, Department: {Createmodel.DepartmentID}, Company: {Createmodel.CompanyID}", // New record's details
                    Description = $"Created new company with Name: {Createmodel.Username}, Email: {Createmodel.Email}, Position: {Createmodel.PositionID}, Department: {Createmodel.DepartmentID}, Company: {Createmodel.CompanyID}"
                };
                // Save the log entry
                _db.LogSystemDatas.Add(logEntry);
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "บันทึกข้อมูลผู้ใช้งานเรียบร้อย" });
            }
        }
        #endregion

        #region Delete User
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int UserID)
        {
            if (UserID == 0)
            {
                return NotFound();
            }
            else
            {
                var model = _db.Users.FirstOrDefault(u => u.UserID == UserID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    model.IsDeleted = true;
                    model.UpdatedDate = DateTime.Now;
                    model.UserManageID = int.Parse(User.GetLoggedInUserID());
                    // Processing the User deletion
                    _db.Users.Update(model);
                    await _db.SaveChangesAsync();
                    // Log the deletion of the User
                    var logEntry = new LogSystemData
                    {
                        TableName = "Companies",
                        Action = "Delete",
                        RecordID = model.UserID,
                        UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"Name: {model.Username}, Email: {model.Email}, Position: {model.PositionID}, Department: {model.DepartmentID}, Company: {model.CompanyID}",
                        NewValue = null,
                        Description = $"Deleted company with Name: {model.Username}, Email: {model.Email}, Position: {model.PositionID}, Department: {model.DepartmentID}, Company: {model.CompanyID}"
                    };
                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();
                    return Json(new { success = true, message = "ลบข้อมูลผู้ใช้งานเรียบร้อย" });
                }
            }
        }
        #endregion

        #region Update User
        [HttpPost]
        public async Task<IActionResult> UpdateUser(int UserID, string UserName, string CardID, string UserEmail, int PositionID, int DepartmentID, int CompanyID)
        {
            if (UserID == 0 || UserName == null || UserEmail == null || PositionID == 0 || DepartmentID == 0 || CompanyID == 0)
            {
                return NotFound();
            }
            else
            {
                var model = _db.Users.FirstOrDefault(u => u.UserID == UserID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    var oldName = model.Username;
                    var oldEmail = model.Email;
                    var oldPosition = model.PositionID;
                    var oldDepartment = model.DepartmentID;
                    var oldCompany = model.CompanyID;
                    model.Username = UserName;
                    model.Email = UserEmail;
                    model.CardID = CardID;
                    model.PositionID = PositionID;
                    model.DepartmentID = DepartmentID;
                    model.CompanyID = CompanyID;
                    model.UpdatedDate = DateTime.Now;
                    model.UserManageID = int.Parse(User.GetLoggedInUserID());
                    // Processing the User update
                    _db.Users.Update(model);
                    await _db.SaveChangesAsync();

                    var userPermission = _db.UserPermissions.FirstOrDefault(u => u.UserID == model.UserID);
                    string[] function = ["Administrator"];
                    if (userPermission == null)
                    {
                        foreach (var item in function)
                        {
                            UserPermission newUserPermission = new UserPermission
                            {
                                UserID = model.UserID,
                                FunctionName = item,
                                CanRead = false,
                                CanCreate = false,
                                CanUpdate = false,
                                CanDelete = false,
                                CreatedDate = DateTime.Now,
                                UserManageID = int.Parse(User.GetLoggedInUserID())
                            };
                            _db.UserPermissions.Add(newUserPermission);
                            await _db.SaveChangesAsync();
                        }

                    }
                    // Log the update of the User
                    var logEntry = new LogSystemData
                    {
                        TableName = "Companies",
                        Action = "Update",
                        RecordID = model.UserID,
                        UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"Name: {oldName}, Email: {oldEmail}, Position: {oldPosition}, Department: {oldDepartment}, Company: {oldCompany}",
                        NewValue = $"Name: {model.Username}, Email: {model.Email}, Position: {model.PositionID}, Department: {model.DepartmentID}, Company: {model.CompanyID}",
                        Description = $"Updated company with Name: {oldName}, Email: {oldEmail}, Position: {oldPosition}, Department: {oldDepartment}, Company: {oldCompany} to {model.Username}, Email: {model.Email}, Position: {model.PositionID}, Department: {model.DepartmentID}, Company: {model.CompanyID}"
                    };
                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();
                    return Json(new { success = true, message = "อัพเดทข้อมูลผู้ใช้งานเรียบร้อย" });
                }
            }
        }
        #endregion

        #region Change Password User
        [HttpPost]
        public async Task<IActionResult> ChangePasswordUser(int UserID, string Password)
        {
            if (UserID == 0 || Password == null)
            {
                return NotFound();
            }
            else
            {
                var model = _db.Users.FirstOrDefault(u => u.UserID == UserID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    var oldPassword = model.Passwordhash;
                    model.Passwordhash = ComputeSha256Hash(Password);
                    model.UpdatedDate = DateTime.Now;
                    //model.UserManageID = int.Parse(User.GetLoggedInUserID());
                    // Processing the User password change
                    _db.Users.Update(model);
                    await _db.SaveChangesAsync();
                    // Log the password change of the User
                    var logEntry = new LogSystemData
                    {
                        TableName = "Companies",
                        Action = "Update",
                        RecordID = model.UserID,
                        UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"Password: {oldPassword}",
                        NewValue = $"Password: {model.Passwordhash}",
                        Description = $"Updated company with Password: {oldPassword} to {model.Passwordhash}"
                    };
                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();
                    return Json(new { success = true, message = "เปลี่ยนรหัสผ่านผู้ใช้งานเรียบร้อย" });
                }
            }
        }
        #endregion

        #region Check User Name
        [HttpPost]
        public JsonResult CheckUserName(string UserName)
        {
            if (UserName == null)
            {
                return Json("User name is required");
            }
            else
            {
                var model = _db.Users.FirstOrDefault(u => u.Username == UserName);
                if (model != null)
                {
                    return Json("User name already exists");
                }
                else
                {
                    return Json(true);
                }
            }
        }
        #endregion

        #region Get User Details
        [HttpPost]
        public JsonResult GetUserDetails(int UserID)
        {
            if (UserID == 0)
            {
                return Json("User ID is required");
            }
            else
            {
                var model = _db.Users.FirstOrDefault(u => u.UserID == UserID);
                if (model == null)
                {
                    return Json("User not found");
                }
                else
                {
                    return Json(model);
                }
            }
        }
        #endregion

        #region Get User Profile Model
        [HttpPost]
        public JsonResult GetUserProfileModel(int UserID)
        {
            if (UserID == 0)
            {
                return Json(new { success = false, message = "User ID is required" });
            }
            else
            {
                var model = _db.Users
                    .Where(u => u.UserID == UserID)
                    .Select(s => new
                    {
                        s.Username,
                        s.FullName,
                        s.CardID,
                        s.Email,
                        s.Position.PositionName,
                        s.Department.DepartmentName,
                        s.Company.CompanyName,
                        s.LoginDate,
                        s.ProfilePicture
                    })
                    .FirstOrDefault();
                if (model == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }
                else
                {
                    var modelNew = new
                    {
                        Username = model.Username,
                        FullName = model.FullName,
                        CardID = model.CardID ?? "ไม่พบข้อมูล",
                        Email = model.Email ?? "ไม่พบข้อมูล",
                        Position = model.PositionName, // Corrected to assign the Position object
                        Department = model.DepartmentName,
                        Company = model.CompanyName,
                        LoginDate = model.LoginDate,
                        ProfilePicture = model.ProfilePicture ?? "Default.png"
                    };
                    return Json(new
                    {
                        success = true,
                        data = modelNew

                    });
                }
            }
        }
        #endregion

        #endregion

        #region Position Functions
        public IActionResult ManagePosition()
        {
            ViewBag.PositionList = _db.Positions.Where(p => p.IsDeleted == false).ToList();
            return View();
        }

        #region Create Position
        [HttpPost]
        public async Task<IActionResult> CreatePosition(string PositionName)
        {
            if (PositionName == null)
            {
                return NotFound();
            }
            else
            {
                Position Createmodel = new Position
                {
                    PositionName = PositionName,
                    CreatedDate = DateTime.Now,
                    //UserManageID = int.Parse(User.GetLoggedInUserID())
                };

                // Processing the Position creation
                _db.Positions.Add(Createmodel);
                await _db.SaveChangesAsync();

                // Log the creation of the Position
                var logEntry = new LogSystemData
                {
                    TableName = "Companies",
                    Action = "Create",
                    RecordID = Createmodel.PositionID, // Assuming PositionID is the primary key
                    //UserManageID = int.Parse(User.GetLoggedInUserID()), // Retrieve the logged user's ID
                    ActionTime = DateTime.Now,
                    IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                    OldValue = null, // No previous value since it's a new record
                    NewValue = $"Name: {Createmodel.PositionName}", // New record's details
                    Description = $"Created new company with Name: {Createmodel.PositionName}"
                };

                // Save the log entry
                _db.LogSystemDatas.Add(logEntry);
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
        }
        #endregion

        #region Delete Position
        [HttpPost]
        public async Task<IActionResult> DeletePosition(int PositionID)
        {
            if (PositionID == 0)
            {
                return NotFound();
            }
            else
            {
                var model = _db.Positions.FirstOrDefault(p => p.PositionID == PositionID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    model.IsDeleted = true;
                    model.UpdatedDate = DateTime.Now;
                    //model.UserManageID = int.Parse(User.GetLoggedInUserID());
                    // Processing the Position deletion
                    _db.Positions.Update(model);
                    await _db.SaveChangesAsync();
                    // Log the deletion of the Position
                    var logEntry = new LogSystemData
                    {
                        TableName = "Companies",
                        Action = "Delete",
                        RecordID = model.PositionID,
                        //UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"Name: {model.PositionName}",
                        NewValue = null,
                        Description = $"Deleted company with Name: {model.PositionName}"
                    };
                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();
                    return Json(new { success = true });
                }
            }
        }
        #endregion

        #region Update Position
        [HttpPost]
        public async Task<IActionResult> UpdatePosition(int PositionID, string PositionName)
        {
            if (PositionID == 0 || PositionName == null)
            {
                return NotFound();
            }
            else
            {
                var model = _db.Positions.FirstOrDefault(p => p.PositionID == PositionID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    var oldName = model.PositionName;
                    model.PositionName = PositionName;
                    model.UpdatedDate = DateTime.Now;
                    //model.UserManageID = int.Parse(User.GetLoggedInUserID());
                    // Processing the Position update
                    _db.Positions.Update(model);
                    await _db.SaveChangesAsync();
                    // Log the update of the Position
                    var logEntry = new LogSystemData
                    {
                        TableName = "Companies",
                        Action = "Update",
                        RecordID = model.PositionID,
                        //UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"Name: {oldName}",
                        NewValue = $"Name: {model.PositionName}",
                        Description = $"Updated company with Name: {oldName} to {model.PositionName}"
                    };
                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();
                    return Json(new { success = true });
                }
            }
        }
        #endregion

        #region Check Position Name
        [HttpPost]
        public JsonResult CheckPositionName(string PositionName)
        {
            var model = _db.Positions.FirstOrDefault(p => p.PositionName == PositionName);
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

        #region Get Position Details
        [HttpPost]
        public JsonResult GetPositionDetails(int PositionID)
        {
            if (PositionID == 0)
            {
                return Json("Position ID is required");
            }
            else
            {
                var model = _db.Positions.FirstOrDefault(p => p.PositionID == PositionID);
                if (model == null)
                {
                    return Json("Position not found");
                }
                else
                {
                    return Json(model);
                }
            }
        }
        #endregion
        #endregion

        #region Department Functions
        public IActionResult ManageDepartment()
        {
            ViewBag.DepartmentList = _db.Departments.Where(d => d.IsDeleted == false).ToList();
            return View();
        }

        #region Create Department
        [HttpPost]
        public async Task<IActionResult> CreateDepartment(string DepartmentName)
        {
            if (DepartmentName == null)
            {
                return NotFound();
            }
            else
            {
                Department Createmodel = new Department
                {
                    DepartmentName = DepartmentName,
                    CreatedDate = DateTime.Now,
                    //UserManageID = int.Parse(User.GetLoggedInUserID())
                };
                // Processing the Department creation
                _db.Departments.Add(Createmodel);
                await _db.SaveChangesAsync();
                // Log the creation of the Department
                var logEntry = new LogSystemData
                {
                    TableName = "Companies",
                    Action = "Create",
                    RecordID = Createmodel.DepartmentID, // Assuming DepartmentID is the primary key
                    //UserManageID = int.Parse(User.GetLoggedInUserID()), // Retrieve the logged user's ID
                    ActionTime = DateTime.Now,
                    IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                    OldValue = null, // No previous value since it's a new record
                    NewValue = $"Name: {Createmodel.DepartmentName}", // New record's details
                    Description = $"Created new company with Name: {Createmodel.DepartmentName}"
                };
                // Save the log entry
                _db.LogSystemDatas.Add(logEntry);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(ManageDepartment));
            }
        }
        #endregion

        #region Delete Department
        [HttpPost]
        public async Task<IActionResult> DeleteDepartment(int DepartmentID)
        {
            if (DepartmentID == 0)
            {
                return NotFound();
            }
            else
            {
                var model = _db.Departments.FirstOrDefault(d => d.DepartmentID == DepartmentID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    model.IsDeleted = true;
                    model.UpdatedDate = DateTime.Now;
                    //model.UserManageID = int.Parse(User.GetLoggedInUserID());
                    // Processing the Department deletion
                    _db.Departments.Update(model);
                    await _db.SaveChangesAsync();
                    // Log the deletion of the Department
                    var logEntry = new LogSystemData
                    {
                        TableName = "Companies",
                        Action = "Delete",
                        RecordID = model.DepartmentID,
                        //UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"Name: {model.DepartmentName}",
                        NewValue = null,
                        Description = $"Deleted company with Name: {model.DepartmentName}"
                    };
                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(ManageDepartment));
                }
            }
        }
        #endregion

        #region Update Department
        [HttpPost]
        public async Task<IActionResult> UpdateDepartment(int DepartmentID, string DepartmentName)
        {
            if (DepartmentID == 0 || DepartmentName == null)
            {
                return NotFound();
            }
            else
            {
                var model = _db.Departments.FirstOrDefault(d => d.DepartmentID == DepartmentID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    var oldName = model.DepartmentName;
                    model.DepartmentName = DepartmentName;
                    model.UpdatedDate = DateTime.Now;
                    //model.UserManageID = int.Parse(User.GetLoggedInUserID());
                    // Processing the Department update
                    _db.Departments.Update(model);
                    await _db.SaveChangesAsync();
                    // Log the update of the Department
                    var logEntry = new LogSystemData
                    {
                        TableName = "Companies",
                        Action = "Update",
                        RecordID = model.DepartmentID,
                        //UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"Name: {oldName}",
                        NewValue = $"Name: {model.DepartmentName}",
                        Description = $"Updated company with Name: {oldName} to {model.DepartmentName}"
                    };
                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(ManageDepartment));
                }
            }
        }
        #endregion

        #region Check Department Name
        [HttpPost]
        public JsonResult CheckDepartmentName(string DepartmentName)
        {
            if (DepartmentName == null)
            {
                return Json("Department name is required");
            }
            else
            {
                var model = _db.Departments.FirstOrDefault(d => d.DepartmentName == DepartmentName);
                if (model != null)
                {
                    return Json("Department name already exists");
                }
                else
                {
                    return Json(true);
                }
            }
        }
        #endregion

        #region Get Department Details
        [HttpPost]
        public JsonResult GetDepartmentDetails(int DepartmentID)
        {
            if (DepartmentID == 0)
            {
                return Json("Department ID is required");
            }
            else
            {
                var model = _db.Departments.FirstOrDefault(d => d.DepartmentID == DepartmentID);
                if (model == null)
                {
                    return Json("Department not found");
                }
                else
                {
                    return Json(model);
                }
            }
        }
        #endregion
        #endregion

        #region Company Functions
        public IActionResult ManageCompany()
        {
            ViewBag.CompanyList = _db.Companies.Where(c => c.IsDeleted == false).ToList();
            return View();
        }

        #region Create Company
        [HttpPost]
        public async Task<IActionResult> CreateCompany(string CompanyName)
        {
            if (CompanyName == null)
            {
                return NotFound();
            }
            else
            {
                Company Createmodel = new Company
                {
                    CompanyName = CompanyName,
                    CreatedDate = DateTime.Now,
                    //UserManageID = int.Parse(User.GetLoggedInUserID())
                };
                // Processing the Company creation
                _db.Companies.Add(Createmodel);
                await _db.SaveChangesAsync();
                // Log the creation of the Company
                var logEntry = new LogSystemData
                {
                    TableName = "Companies",
                    Action = "Create",
                    RecordID = Createmodel.CompanyID, // Assuming CompanyID is the primary key
                    UserManageID = int.Parse(User.GetLoggedInUserID()), // Retrieve the logged user's ID
                    ActionTime = DateTime.Now,
                    IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                    OldValue = null, // No previous value since it's a new record
                    NewValue = $"Name: {Createmodel.CompanyName}", // New record's details
                    Description = $"Created new company with Name: {Createmodel.CompanyName}"
                };
                // Save the log entry
                _db.LogSystemDatas.Add(logEntry);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(ManageCompany));
            }
        }
        #endregion

        #region Delete Company
        [HttpPost]
        public async Task<IActionResult> DeleteCompany(int CompanyID)
        {
            if (CompanyID == 0)
            {
                return NotFound();
            }
            else
            {
                var model = _db.Companies.FirstOrDefault(c => c.CompanyID == CompanyID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    model.IsDeleted = true;
                    model.UpdatedDate = DateTime.Now;
                    model.UserManageID = int.Parse(User.GetLoggedInUserID());
                    // Processing the Company deletion
                    _db.Companies.Update(model);
                    await _db.SaveChangesAsync();
                    // Log the deletion of the Company
                    var logEntry = new LogSystemData
                    {
                        TableName = "Companies",
                        Action = "Delete",
                        RecordID = model.CompanyID,
                        UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"Name: {model.CompanyName}",
                        NewValue = null,
                        Description = $"Deleted company with Name: {model.CompanyName}"
                    };
                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(ManageCompany));
                }
            }
        }
        #endregion

        #region Update Company
        [HttpPost]
        public async Task<IActionResult> UpdateCompany(int CompanyID, string CompanyName)
        {
            if (CompanyID == 0 || CompanyName == null)
            {
                return NotFound();
            }
            else
            {
                var model = _db.Companies.FirstOrDefault(c => c.CompanyID == CompanyID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    var oldName = model.CompanyName;
                    model.CompanyName = CompanyName;
                    model.UpdatedDate = DateTime.Now;
                    //model.UserManageID = int.Parse(User.GetLoggedInUserID());
                    // Processing the Company update
                    _db.Companies.Update(model);
                    await _db.SaveChangesAsync();
                    // Log the update of the Company
                    var logEntry = new LogSystemData
                    {
                        TableName = "Companies",
                        Action = "Update",
                        RecordID = model.CompanyID,
                        UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"Name: {oldName}",
                        NewValue = $"Name: {model.CompanyName}",
                        Description = $"Updated company with Name: {oldName} to {model.CompanyName}"
                    };
                    // Save the log entry
                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(ManageCompany));
                }
            }
        }
        #endregion

        #region Check Company Name
        [HttpPost]
        public JsonResult CheckCompanyName(string CompanyName)
        {
            if (CompanyName == null)
            {
                return Json("Company name is required");
            }
            else
            {
                var model = _db.Companies.FirstOrDefault(c => c.CompanyName == CompanyName);
                if (model != null)
                {
                    return Json("Company name already exists");
                }
                else
                {
                    return Json(true);
                }
            }
        }
        #endregion

        #region Get Company Details
        [HttpPost]
        public JsonResult GetCompanyDetails(int CompanyID)
        {
            if (CompanyID == 0)
            {
                return Json("Company ID is required");
            }
            else
            {
                var model = _db.Companies.FirstOrDefault(c => c.CompanyID == CompanyID);
                if (model == null)
                {
                    return Json("Company not found");
                }
                else
                {
                    return Json(model);
                }
            }
        }
        #endregion
        #endregion

        #region Permission Functions
        public IActionResult ManagePermission()
        {
            ViewBag.PermissionList = _db.Users.Where(p => p.IsDeleted == false).ToList();
            return View();
        }

        [HttpGet]
        public JsonResult GetDetailsPermissions(int UserID)
        {
            try
            {
                var user = _db.Users.FirstOrDefault(u => u.UserID == UserID);

                if (user == null)
                {
                    return Json(new { success = false, message = "ไม่พบผู้ใช้งาน" });
                }

                // Fetch permissions logic from the database
                var permissions = _db.UserPermissions
                    .Where(p => p.UserID == UserID)
                    .ToList();

                if (permissions == null || permissions.Count == 0)
                {
                    return Json(new { success = false, message = "ไม่พบสิทธิ์การเข้าใช้งานสำหรับผู้ใช้งานนี้" });
                }

                var permissionsDict = new Dictionary<string, object>();
                foreach (var perm in permissions)
                {
                    permissionsDict[perm.FunctionName.ToString()] = new
                    {
                        CanRead = perm.CanRead,
                        CanCreate = perm.CanCreate,
                        CanUpdate = perm.CanUpdate,
                        CanDelete = perm.CanDelete
                    };
                }

                var response = new
                {
                    groupUserID = user.UserID,
                    groupName = user.Username,
                    fullName = user.FullName,
                    permissions = permissionsDict
                };

                return Json(new { success = true, permissions = response.permissions, groupUserID = response.groupUserID, groupName = response.groupName, fullName = response.fullName });
            }
            catch (Exception ex)
            {
                string message = "An error occurred while updating lease history.";
                if (ex.InnerException != null)
                {
                    message += " Inner exception: " + ex.InnerException.Message;
                }
                return Json(new
                {
                    success = false,
                    message
                });
            }
        }

        // Example method to get user permissions
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
                    userPermissions.Add("View" + permission.FunctionName);
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

        #endregion



        //Hashing คือการแปลงข้อมูลให้อยู่ในรูปแบบที่ไม่สามารถย้อนกลับได้ เหมาะสำหรับการตรวจสอบความถูกต้องของข้อมูล เช่น การตรวจสอบรหัสผ่าน
        public static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

    }
}
