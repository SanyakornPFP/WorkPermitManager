using ContainerEvaluationSystem.Helpers;
using Microsoft.AspNetCore.Mvc;
using WorkPermitManager.Data;
using WorkPermitManager.Models;

namespace WorkPermitManager.Controllers
{
    public class AdministratorController : Controller
    {
        private readonly Db_WorkPermitManagerModel _db;
        public AdministratorController(Db_WorkPermitManagerModel db)
        {
            _db = db;
        }

        #region User Functions
        public IActionResult ManageUser()
        {
            ViewBag.UserList = _db.Users.Where(u => u.IsDeleted == false).ToList();
            ViewBag.PositionList = _db.Positions.Where(p => p.IsDeleted == false).ToList();
            ViewBag.DepartmentList = _db.Departments.Where(d => d.IsDeleted == false).ToList();
            ViewBag.CompanyList = _db.Companies.Where(c => c.IsDeleted == false).ToList();
            return View();
        }

        #region Create User
        [HttpPost]
        public async Task<IActionResult> CreateUser(string UserName, string UserEmail, string CardID, string UserPassword, int PositionID, int DepartmentID, int CompanyID)
        {
            if (UserName == null || UserEmail == null || UserPassword == null || PositionID == 0 || DepartmentID == 0 || CompanyID == 0)
            {
                return NotFound();
            }
            else
            {
                User Createmodel = new User
                {
                    Username = UserName,
                    Email = UserEmail,
                    CardID = CardID,
                    Passwordhash = UserPassword,
                    PositionID = PositionID,
                    DepartmentID = DepartmentID,
                    CompanyID = CompanyID,
                    CreatedDate = DateTime.Now,
                    UserManageID = int.Parse(User.GetLoggedInUserID())
                };
                // Processing the User creation
                _db.Users.Add(Createmodel);
                await _db.SaveChangesAsync();
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
                return RedirectToAction(nameof(ManageUser));
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
                    return RedirectToAction(nameof(ManageUser));
                }
            }
        }
        #endregion

        #region Update User
        [HttpPost]
        public async Task<IActionResult> UpdateUser(int UserID, string UserName, string CardID, string UserEmail, string UserPassword, int PositionID, int DepartmentID, int CompanyID)
        {
            if (UserID == 0 || UserName == null || UserEmail == null || UserPassword == null || PositionID == 0 || DepartmentID == 0 || CompanyID == 0)
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
                    model.Passwordhash = UserPassword;
                    model.PositionID = PositionID;
                    model.DepartmentID = DepartmentID;
                    model.CompanyID = CompanyID;
                    model.UpdatedDate = DateTime.Now;
                    model.UserManageID = int.Parse(User.GetLoggedInUserID());
                    // Processing the User update
                    _db.Users.Update(model);
                    await _db.SaveChangesAsync();
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
                    return RedirectToAction(nameof(ManageUser));
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
                    model.UserManageID = int.Parse(User.GetLoggedInUserID());
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



    }
}
