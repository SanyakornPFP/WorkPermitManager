using iText.IO.Font;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkPermitManager.Data;
using WorkPermitManager.Helpers;
using WorkPermitManager.Models;

namespace WorkPermitManager.Controllers
{
    [Authorize]
    public class ServicesController : Controller
    {

        private readonly Db_WorkPermitManagerModel _db;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public ServicesController(Db_WorkPermitManagerModel db, IWebHostEnvironment hostEnvironment)
        {
            _db = db;
            _hostingEnvironment = hostEnvironment;
        }

        #region Service
        public IActionResult ServicesPage()
        {
            ViewBag.ServiceList = _db.Services
               .Where(s => s.IsActive)
               .Select(s => new
               {
                   s.ServiceID,
                   s.RecordDate,
                   s.Employer.NameTh,
                   s.ServiceType.ServiceTypeName,
                   s.ServiceItem.ServiceItemName,
                   s.ExpectedPeople,
                   ServiceWorkerCount = s.ServiceWorkers.Where(s => s.IsActive).Count(),
                   s.QuotationNumber,
                   s.Status,
                   s.IsMou
               })
               .OrderByDescending(s => s.ServiceID)
               .ToList();

            ///Select List
            ViewBag.EmployerList = _db.Employers.Where(s => s.IsActive).ToList();
            ViewBag.ServiceTypeList = _db.ServiceTypes.Where(s => s.IsActive).ToList();
            ViewBag.ServiceItemList = _db.ServiceItems.Where(s => s.IsActive).ToList();

            return View();
        }

        #region Create Service
        [HttpPost]
        public async Task<IActionResult> CreateService(Service model)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("CreateServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }
            else
            {
                model.CreatedAt = DateTime.Now;
                model.RecordDate = DateTime.Now.Date;
                model.Status = "รอดำเนินการ";
                model.IsActive = true;
                model.UserManageID = int.Parse(User.GetLoggedInUserID());

                _db.Services.Add(model);
                await _db.SaveChangesAsync();

                var logEntry = new LogSystemData
                {
                    TableName = "Services",
                    Action = "Create",
                    RecordID = model.ServiceID,
                    UserManageID = int.Parse(User.GetLoggedInUserID()),
                    ActionTime = DateTime.Now,
                    IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                    OldValue = null,
                    NewValue = $"ServiceID: {model.ServiceID}, ServiceTypeID: {model.ServiceTypeID}, ServiceItemID: {model.ServiceItemID}",
                    Description = $"Created new service with ID: {model.ServiceID}"
                };

                _db.LogSystemDatas.Add(logEntry);
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
        }
        #endregion

        #region Delete Service
        [HttpPost]
        public async Task<IActionResult> DeleteService(int ServiceID)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("DeleteServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (ServiceID == 0)
            {
                return NotFound();
            }
            else
            {
                var model = _db.Services.FirstOrDefault(p => p.ServiceID == ServiceID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    model.IsActive = false;
                    model.UpdatedAt = DateTime.Now;
                    model.UserManageID = int.Parse(User.GetLoggedInUserID());

                    _db.Services.Update(model);
                    await _db.SaveChangesAsync();

                    var logEntry = new LogSystemData
                    {
                        TableName = "Services",
                        Action = "Delete",
                        RecordID = model.ServiceID,
                        UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"ServiceID: {model.ServiceID}, ServiceTypeID: {model.ServiceTypeID}, ServiceItemID: {model.ServiceItemID}",
                        NewValue = null,
                        Description = $"Deleted service with ID: {model.ServiceID}"
                    };

                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();

                    return Json(new { success = true });
                }
            }
        }
        #endregion

        #region Update Service
        [HttpPost]
        public async Task<IActionResult> UpdateService(Service model)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("UpdateServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (model.ServiceID == 0 || string.IsNullOrEmpty(model.ServiceTypeID.ToString()) || string.IsNullOrEmpty(model.ServiceItemID.ToString()))
            {
                return NotFound();
            }
            else
            {
                var data = _db.Services.FirstOrDefault(p => p.ServiceID == model.ServiceID);
                if (data == null)
                {
                    return NotFound();
                }
                else
                {
                    var oldValues = new
                    {
                        data.ServiceTypeID,
                        data.ServiceItemID,
                        data.Note,
                        data.Recorder,
                        data.SignatureName,
                        data.IsMou,
                        data.QuotationNumber,
                        data.ExpectedPeople,
                        data.TotalPrice,
                        data.Deposit,
                        data.RemainingPayment,
                        data.OutstandingAmount,
                        data.IsSentToAccounting
                    };

                    // อัปเดตค่าจาก model
                    data.ServiceTypeID = model.ServiceTypeID;
                    data.ServiceItemID = model.ServiceItemID;
                    data.Note = model.Note;
                    data.Recorder = model.Recorder;
                    data.SignatureName = model.SignatureName;
                    data.IsMou = model.IsMou ?? false; // ตรวจสอบค่า null
                    data.QuotationNumber = model.QuotationNumber;
                    data.ExpectedPeople = model.ExpectedPeople;
                    data.TotalPrice = model.TotalPrice;
                    data.Deposit = model.Deposit;
                    data.RemainingPayment = model.RemainingPayment;
                    data.OutstandingAmount = model.OutstandingAmount;
                    data.IsSentToAccounting = model.IsSentToAccounting ?? false; // ตรวจสอบค่า null
                    data.UpdatedAt = DateTime.Now;
                    data.UserManageID = int.Parse(User.GetLoggedInUserID());

                    _db.Services.Update(data);
                    await _db.SaveChangesAsync();

                    var logEntry = new LogSystemData
                    {
                        TableName = "Services",
                        Action = "Update",
                        RecordID = data.ServiceID,
                        UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"ServiceTypeID: {oldValues.ServiceTypeID}, ServiceItemID: {oldValues.ServiceItemID}, Note: {oldValues.Note}, Recorder: {oldValues.Recorder}, SignatureName: {oldValues.SignatureName}, IsMou: {oldValues.IsMou}, QuotationNumber: {oldValues.QuotationNumber}, ExpectedPeople: {oldValues.ExpectedPeople}, TotalPrice: {oldValues.TotalPrice}, Deposit: {oldValues.Deposit}, RemainingPayment: {oldValues.RemainingPayment}, OutstandingAmount: {oldValues.OutstandingAmount}, IsSentToAccounting: {oldValues.IsSentToAccounting}",
                        NewValue = $"ServiceTypeID: {data.ServiceTypeID}, ServiceItemID: {data.ServiceItemID}, Note: {data.Note}, RecordDate: {data.RecordDate}, Recorder: {data.Recorder}, SignatureName: {data.SignatureName}, IsMou: {data.IsMou}, QuotationNumber: {data.QuotationNumber}, ExpectedPeople: {data.ExpectedPeople}, TotalPrice: {data.TotalPrice}, Deposit: {data.Deposit}, RemainingPayment: {data.RemainingPayment}, OutstandingAmount: {data.OutstandingAmount}, Status: {data.Status}, IsSentToAccounting: {data.IsSentToAccounting}",
                        Description = $"Updated service with ID: {model.ServiceID}"
                    };

                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();

                    return Json(new { success = true });
                }
            }
        }
        #endregion

        #region Get Service Details
        [HttpPost]
        public JsonResult GetServiceDetails(int ServiceID)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("ReadServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (ServiceID == 0)
            {
                return Json(new { success = false, message = "Service ID is required" });
            }
            else
            {
                var model = _db.Services
                    .Where(p => p.ServiceID == ServiceID)
                    .Select(s => new
                    {
                        s.ServiceID,
                        s.EmployerID,
                        s.ServiceTypeID,
                        s.ServiceItemID,
                        EmployerName = s.Employer.NameTh,
                        ServiceTypeName = s.ServiceType.ServiceTypeName,
                        ServiceItemName = s.ServiceItem.ServiceItemName,
                        s.Note,
                        s.RecordDate,
                        s.Recorder,
                        s.SignatureName,
                        s.IsMou,
                        s.QuotationNumber,
                        s.ExpectedPeople,
                        s.TotalPrice,
                        s.Deposit,
                        s.RemainingPayment,
                        s.OutstandingAmount,
                        s.IsSentToAccounting,
                        UserCreate = s.UserManage.FullName,

                    })
                    .FirstOrDefault();

                if (model == null)
                {
                    return Json(new { success = false, message = "Service not found" });
                }
                else
                {
                    return Json(new { success = true, data = model });
                }
            }
        }
        #endregion
        #endregion

        #region ServiceType
        public IActionResult ServiceType()
        {
            ViewBag.ServiceList = _db.ServiceTypes
                .Select(s => new
                {
                    s.ServiceTypeID,
                    s.ServiceTypeName,
                    s.Descriptions,
                    s.Note
                })
                .ToList();

            return View();
        }

        #region Create ServiceType
        [HttpPost]
        public async Task<IActionResult> CreateServiceType(ServiceType model)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("CreateServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }
            else
            {
                model.CreateAt = DateTime.Now;
                model.IsActive = true;
                model.UserManageID = int.Parse(User.GetLoggedInUserID());

                _db.ServiceTypes.Add(model);
                await _db.SaveChangesAsync();

                var logEntry = new LogSystemData
                {
                    TableName = "ServiceTypes",
                    Action = "Create",
                    RecordID = model.ServiceTypeID,
                    UserManageID = int.Parse(User.GetLoggedInUserID()),
                    ActionTime = DateTime.Now,
                    IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                    OldValue = null,
                    NewValue = $"ServiceTypeID: {model.ServiceTypeID}, ServiceTypeName: {model.ServiceTypeName}",
                    Description = $"Created new service type with ID: {model.ServiceTypeID}"
                };

                _db.LogSystemDatas.Add(logEntry);
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
        }
        #endregion

        #region Delete ServiceType
        [HttpPost]
        public async Task<IActionResult> DeleteServiceType(int ServiceTypeID)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("DeleteServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (ServiceTypeID == 0)
            {
                return NotFound();
            }
            else
            {
                var model = _db.ServiceTypes.FirstOrDefault(p => p.ServiceTypeID == ServiceTypeID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    model.IsActive = false;
                    model.UpdateAt = DateTime.Now;
                    model.UserManageID = int.Parse(User.GetLoggedInUserID());

                    _db.ServiceTypes.Update(model);
                    await _db.SaveChangesAsync();

                    var logEntry = new LogSystemData
                    {
                        TableName = "ServiceTypes",
                        Action = "Delete",
                        RecordID = model.ServiceTypeID,
                        UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"ServiceTypeID: {model.ServiceTypeID}, ServiceTypeName: {model.ServiceTypeName}",
                        NewValue = null,
                        Description = $"Deleted service type with ID: {model.ServiceTypeID}"
                    };

                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();

                    return Json(new { success = true });
                }
            }
        }
        #endregion

        #region Update ServiceType
        [HttpPost]
        public async Task<IActionResult> UpdateServiceType(ServiceType model)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("UpdateServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (model.ServiceTypeID == 0 || string.IsNullOrEmpty(model.ServiceTypeName))
            {
                return NotFound();
            }
            else
            {
                var data = _db.ServiceTypes.FirstOrDefault(p => p.ServiceTypeID == model.ServiceTypeID);
                if (data == null)
                {
                    return NotFound();
                }
                else
                {
                    var oldValues = new
                    {
                        data.ServiceTypeName,
                        data.Descriptions,
                        data.Note
                    };

                    data.ServiceTypeName = model.ServiceTypeName;
                    data.Descriptions = model.Descriptions;
                    data.Note = model.Note;
                    data.UpdateAt = DateTime.Now;
                    data.UserManageID = int.Parse(User.GetLoggedInUserID());

                    _db.ServiceTypes.Update(data);
                    await _db.SaveChangesAsync();

                    var logEntry = new LogSystemData
                    {
                        TableName = "ServiceTypes",
                        Action = "Update",
                        RecordID = data.ServiceTypeID,
                        UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"ServiceTypeName: {oldValues.ServiceTypeName}, Descriptions: {oldValues.Descriptions}, Note: {oldValues.Note}",
                        NewValue = $"ServiceTypeName: {data.ServiceTypeName}, Descriptions: {data.Descriptions}, Note: {data.Note}",
                        Description = $"Updated service type with ID: {model.ServiceTypeID}"
                    };

                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();

                    return Json(new { success = true });
                }
            }
        }
        #endregion

        #region Check ServiceType Name
        [HttpPost]
        public JsonResult CheckServiceTypeName(string ServiceTypeName, int ServiceTypeID)
        {
            if (ServiceTypeID == 0)
            {
                var model = _db.ServiceTypes.FirstOrDefault(p => p.ServiceTypeName == ServiceTypeName);
                if (model != null)
                {
                    return Json(new { success = false, message = "ชื่อประเภทบริการนี้มีอยู่แล้ว" });
                }
                else
                {
                    return Json(new { success = true });
                }
            }
            else
            {
                var model = _db.ServiceTypes.FirstOrDefault(p => p.ServiceTypeName == ServiceTypeName && p.ServiceTypeID != ServiceTypeID);
                if (model != null)
                {
                    return Json(new { success = false, message = "ชื่อประเภทบริการนี้มีอยู่แล้ว" });
                }
                else
                {
                    return Json(new { success = true });
                }
            }
        }
        #endregion Check ServiceType Name

        #region Get ServiceType Details
        [HttpPost]
        public JsonResult GetServiceTypeDetails(int ServiceTypeID)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("ReadServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (ServiceTypeID == 0)
            {
                return Json(new { success = false, message = "Service Type ID is required" });
            }
            else
            {
                var model = _db.ServiceTypes
                    .Where(p => p.ServiceTypeID == ServiceTypeID)
                    .Select(s => new
                    {
                        s.ServiceTypeID,
                        s.ServiceTypeName,
                        s.Descriptions,
                        s.Note,
                        s.CreateAt,
                        s.UpdateAt,
                        s.IsActive,
                    })
                    .FirstOrDefault();

                if (model == null)
                {
                    return Json(new { success = false, message = "Service type not found" });
                }
                else
                {
                    return Json(new { success = true, data = model });
                }
            }
        }
        #endregion
        #endregion

        #region ServiceItem
        public IActionResult ServiceItem()
        {
            ViewBag.ServiceList = _db.ServiceItems
                .Select(s => new
                {
                    s.ServiceItemID,
                    s.ServiceItemName,
                    s.Descriptions,
                    s.Note
                })
                .ToList();

            return View();
        }

        #region Create ServiceItem
        [HttpPost]
        public async Task<IActionResult> CreateServiceItem(ServiceItem model)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("CreateServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }
            else
            {
                model.CreateAt = DateTime.Now;
                model.IsActive = true;
                model.UserManageID = int.Parse(User.GetLoggedInUserID());

                _db.ServiceItems.Add(model);
                await _db.SaveChangesAsync();

                var logEntry = new LogSystemData
                {
                    TableName = "ServiceItems",
                    Action = "Create",
                    RecordID = model.ServiceItemID,
                    UserManageID = int.Parse(User.GetLoggedInUserID()),
                    ActionTime = DateTime.Now,
                    IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                    OldValue = null,
                    NewValue = $"ServiceItemID: {model.ServiceItemID}, ServiceItemName: {model.ServiceItemName}",
                    Description = $"Created new service item with ID: {model.ServiceItemID}"
                };

                _db.LogSystemDatas.Add(logEntry);
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
        }
        #endregion

        #region Delete ServiceItem
        [HttpPost]
        public async Task<IActionResult> DeleteServiceItem(int ServiceItemID)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("DeleteServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (ServiceItemID == 0)
            {
                return NotFound();
            }
            else
            {
                var model = _db.ServiceItems.FirstOrDefault(p => p.ServiceItemID == ServiceItemID);
                if (model == null)
                {
                    return NotFound();
                }
                else
                {
                    model.IsActive = false;
                    model.UpdateAt = DateTime.Now;
                    model.UserManageID = int.Parse(User.GetLoggedInUserID());

                    _db.ServiceItems.Update(model);
                    await _db.SaveChangesAsync();

                    var logEntry = new LogSystemData
                    {
                        TableName = "ServiceItems",
                        Action = "Delete",
                        RecordID = model.ServiceItemID,
                        UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"ServiceItemID: {model.ServiceItemID}, ServiceItemName: {model.ServiceItemName}",
                        NewValue = null,
                        Description = $"Deleted service item with ID: {model.ServiceItemID}"
                    };

                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();

                    return Json(new { success = true });
                }
            }
        }
        #endregion

        #region Update ServiceItem
        [HttpPost]
        public async Task<IActionResult> UpdateServiceItem(ServiceItem model)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("UpdateServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (model.ServiceItemID == 0 || string.IsNullOrEmpty(model.ServiceItemName))
            {
                return NotFound();
            }
            else
            {
                var data = _db.ServiceItems.FirstOrDefault(p => p.ServiceItemID == model.ServiceItemID);
                if (data == null)
                {
                    return NotFound();
                }
                else
                {
                    var oldValues = new
                    {
                        data.ServiceItemName,
                        data.Descriptions,
                        data.Note
                    };

                    data.ServiceItemName = model.ServiceItemName;
                    data.Descriptions = model.Descriptions;
                    data.Note = model.Note;
                    data.UpdateAt = DateTime.Now;
                    data.UserManageID = int.Parse(User.GetLoggedInUserID());

                    _db.ServiceItems.Update(data);
                    await _db.SaveChangesAsync();

                    var logEntry = new LogSystemData
                    {
                        TableName = "ServiceItems",
                        Action = "Update",
                        RecordID = data.ServiceItemID,
                        UserManageID = int.Parse(User.GetLoggedInUserID()),
                        ActionTime = DateTime.Now,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        OldValue = $"ServiceItemName: {oldValues.ServiceItemName}, Descriptions: {oldValues.Descriptions}, Note: {oldValues.Note}",
                        NewValue = $"ServiceItemName: {data.ServiceItemName}, Descriptions: {data.Descriptions}, Note: {data.Note}",
                        Description = $"Updated service item with ID: {model.ServiceItemID}"
                    };

                    _db.LogSystemDatas.Add(logEntry);
                    await _db.SaveChangesAsync();

                    return Json(new { success = true });
                }
            }
        }
        #endregion

        #region Check ServiceItem Name
        [HttpPost]
        public JsonResult CheckServiceItemName(string ServiceItemName, int ServiceItemID)
        {
            if (ServiceItemID == 0)
            {
                var model = _db.ServiceItems.FirstOrDefault(p => p.ServiceItemName == ServiceItemName);
                if (model != null)
                {
                    return Json(new { success = false, message = "ชื่อรายการบริการนี้มีอยู่แล้ว" });
                }
                else
                {
                    return Json(new { success = true });
                }
            }
            else
            {
                var model = _db.ServiceItems.FirstOrDefault(p => p.ServiceItemName == ServiceItemName && p.ServiceItemID != ServiceItemID);
                if (model != null)
                {
                    return Json(new { success = false, message = "ชื่อรายการบริการนี้มีอยู่แล้ว" });
                }
                else
                {
                    return Json(new { success = true });
                }
            }
        }
        #endregion ตรวจสอบชื่อรายการบริการ

        #region Get ServiceItem Details
        [HttpPost]
        public JsonResult GetServiceItemDetails(int ServiceItemID)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("ReadServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            if (ServiceItemID == 0)
            {
                return Json(new { success = false, message = "Service Item ID is required" });
            }
            else
            {
                var model = _db.ServiceItems
                    .Where(p => p.ServiceItemID == ServiceItemID)
                    .Select(s => new
                    {
                        s.ServiceItemID,
                        s.ServiceItemName,
                        s.Descriptions,
                        s.Note,
                        s.CreateAt,
                        s.UpdateAt,
                        s.IsActive,
                    })
                    .FirstOrDefault();

                if (model == null)
                {
                    return Json(new { success = false, message = "Service item not found" });
                }
                else
                {
                    return Json(new { success = true, data = model });
                }
            }
        }
        #endregion
        #endregion

        #region ServiceWorkerManagement
        public IActionResult ServiceWorkerManagement(int ServiceID)
        {
            var model = _db.Services
                     .Where(p => p.ServiceID == ServiceID)
                     .Select(s => new
                     {
                         s.ServiceID,
                         s.EmployerID,
                         s.ServiceTypeID,
                         s.ServiceItemID,
                         EmployerName = s.Employer.NameTh,
                         ServiceTypeName = s.ServiceType.ServiceTypeName,
                         ServiceItemName = s.ServiceItem.ServiceItemName,
                         s.Note,
                         RecordDate = s.RecordDate.ToString("dd/MM/yyyy"),
                         s.Recorder,
                         s.SignatureName,
                         s.IsMou,
                         s.QuotationNumber,
                         s.ExpectedPeople,
                         s.TotalPrice,
                         s.Deposit,
                         s.RemainingPayment,
                         s.OutstandingAmount,
                         s.IsSentToAccounting,
                         UserCreate = s.UserManage.FullName,

                     })
                     .FirstOrDefault();

            var workers = _db.ServiceWorkers
                .Where(s => s.ServiceID == ServiceID && s.IsActive)
                .ToList();

            ViewBag.WorkerList = workers;

            return View(model);
        }

        #region Create ServiceWorker
        [HttpPost]
        public async Task<IActionResult> CreateServiceWorker(RequestServiceWorkerModel model)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("CreateServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            // สร้าง ServiceWorker ใหม่ตามข้อมูลในโมเดล
            ServiceWorker createModel = new ServiceWorker
            {
                ServiceID = model.ServiceID,
                Title = model.Title,
                FirstNameEN = model.FirstNameEN,
                LastNameEN = model.LastNameEN,
                Nationality = model.Nationality,
                Country = model.Country,
                DateOfBirth = model.DateOfBirth,
                PlaceOfBirth = model.PlaceOfBirth,
                BloodType = model.BloodType,
                PassportNumber = model.PassportNumber,
                PassportDateOfIssue = model.PassportDateOfIssue,
                PassportExpiryDate = model.PassportExpiryDate,
                PassportIssuedAt = model.PassportIssuedAt,
                TypeVisa = model.TypeVisa,
                VisaNumber = model.VisaNumber,
                VisaDateOfIssue = model.VisaDateOfIssue,
                VisaExpiryDate = model.VisaExpiryDate,
                VisaIssuedAt = model.VisaIssuedAt,
                Expiry90Days = model.Expiry90Days,
                Note = model.Note,
                ServiceFee = model.ServiceFee,
                DateOfArrival = model.DateOfArrival,
                ImmigrationCheckpoint = model.ImmigrationCheckpoint,
                PermittedUntil = model.PermittedUntil,
                ResidenceNo = model.ResidenceNo,
                ResidenceIssuedAt = model.ResidenceIssuedAt,
                ResidenceProvince = model.ResidenceProvince,
                ResidenceDateOfIssue = model.ResidenceDateOfIssue,
                ResidenceExpiryDate = model.ResidenceExpiryDate,
                AlienNo = model.AlienNo,
                AlienIssuedAt = model.AlienIssuedAt,
                AlienProvince = model.AlienProvince,
                AlienDateOfIssue = model.AlienDateOfIssue,
                AlienExpiryDate = model.AlienExpiryDate,
                WorkPermitNumber = model.WorkPermitNumber,
                WorkPermitDateOfIssue = model.WorkPermitDateOfIssue,
                WorkPermitExpiryDate = model.WorkPermitExpiryDate,
                WorkPermitIssuedAtProvince = model.WorkPermitIssuedAtProvince,
                WorkPermitActionType = model.WorkPermitActionType,
                CreatedAt = DateTime.Now,
                IsActive = true,
                UserManageID = int.Parse(User.GetLoggedInUserID())
            };

            _db.ServiceWorkers.Add(createModel);

            // คำนวณค่าบริการทั้งหมด
            var service = await _db.Services.FirstOrDefaultAsync(s => s.ServiceID == model.ServiceID);
            if (service != null)
            {
                service.TotalPrice += createModel.ServiceFee ?? 0;
                _db.Services.Update(service);
            }

            await _db.SaveChangesAsync();

            // บันทึก Log การสร้าง
            var logEntry = new LogSystemData
            {
                TableName = "ServiceWorkers",
                Action = "Create",
                RecordID = createModel.ServiceWorkerID,
                UserManageID = int.Parse(User.GetLoggedInUserID()),
                ActionTime = DateTime.Now,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                OldValue = null,
                NewValue = $"ServiceWorkerID: {createModel.ServiceWorkerID}, PassportNumber: {createModel.PassportNumber}",
                Description = $"Created new service worker with ID: {createModel.ServiceWorkerID}"
            };

            _db.LogSystemDatas.Add(logEntry);
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }
        #endregion

        #region Delete ServiceWorker
        [HttpPost]
        public async Task<IActionResult> DeleteServiceWorker(int ServiceWorkerID)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("DeleteServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            var model = _db.ServiceWorkers.FirstOrDefault(p => p.ServiceWorkerID == ServiceWorkerID);
            if (model == null)
            {
                return NotFound();
            }

            model.IsActive = false;
            model.UpdatedAt = DateTime.Now;
            model.UserManageID = int.Parse(User.GetLoggedInUserID());

            _db.ServiceWorkers.Update(model);
            await _db.SaveChangesAsync();

            // Log the deletion
            var logEntry = new LogSystemData
            {
                TableName = "ServiceWorkers",
                Action = "Delete",
                RecordID = model.ServiceWorkerID,
                UserManageID = int.Parse(User.GetLoggedInUserID()),
                ActionTime = DateTime.Now,
                IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                OldValue = $"ServiceWorkerID: {model.ServiceWorkerID}, PassportNumber: {model.PassportNumber}",
                NewValue = null,
                Description = $"Deleted service worker with ID: {model.ServiceWorkerID}"
            };

            _db.LogSystemDatas.Add(logEntry);
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }
        #endregion

        #region Update ServiceWorker
        [HttpPost]
        public async Task<IActionResult> UpdateServiceWorker(RequestServiceWorkerModel model)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("UpdateServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            var data = await _db.ServiceWorkers.FirstOrDefaultAsync(p => p.ServiceWorkerID == model.ServiceWorkerID);
            if (data == null)
            {
                return Json(new { success = false, message = "ไม่พบข้อมูลของ ServiceWorker ที่ต้องการอัปเดต" });
            }

            // Capture old values for logging
            var oldValues = new
            {
                data.PassportNumber,
                data.Nationality,
                data.Title,
                data.FirstNameEN,
                data.LastNameEN,
                data.ServiceFee,
                data.Expiry90Days,
                data.Note,
                data.DateOfBirth,
                data.BloodType,
                data.PassportDateOfIssue,
                data.PassportExpiryDate,
                data.TypeVisa,
                data.VisaNumber,
                data.VisaDateOfIssue,
                data.VisaExpiryDate,
                data.VisaIssuedAt,
                data.PlaceOfBirth,
                data.PassportIssuedAt,
                data.Country,
                data.DateOfArrival,
                data.ImmigrationCheckpoint,
                data.PermittedUntil,
                data.ResidenceNo,
                data.ResidenceIssuedAt,
                data.ResidenceProvince,
                data.ResidenceDateOfIssue,
                data.ResidenceExpiryDate,
                data.AlienNo,
                data.AlienIssuedAt,
                data.AlienProvince,
                data.AlienDateOfIssue,
                data.AlienExpiryDate,
                data.WorkPermitNumber,
                data.WorkPermitDateOfIssue,
                data.WorkPermitExpiryDate,
                data.WorkPermitIssuedAtProvince,
                data.WorkPermitActionType
            };

            // Update the fields
            data.Nationality = model.Nationality;
            data.Title = model.Title;
            data.FirstNameEN = model.FirstNameEN;
            data.LastNameEN = model.LastNameEN;
            data.ServiceFee = model.ServiceFee;
            data.BloodType = model.BloodType;
            data.Expiry90Days = model.Expiry90Days;
            data.Note = model.Note;
            data.DateOfBirth = model.DateOfBirth;
            data.PassportNumber = model.PassportNumber;
            data.PassportDateOfIssue = model.PassportDateOfIssue;
            data.PassportExpiryDate = model.PassportExpiryDate;
            data.PassportIssuedAt = model.PassportIssuedAt;
            data.TypeVisa = model.TypeVisa;
            data.VisaNumber = model.VisaNumber;
            data.VisaExpiryDate = model.VisaExpiryDate;
            data.VisaDateOfIssue = model.VisaDateOfIssue;
            data.VisaIssuedAt = model.VisaIssuedAt;
            data.PlaceOfBirth = model.PlaceOfBirth;
            data.Country = model.Country;
            data.DateOfArrival = model.DateOfArrival;
            data.ImmigrationCheckpoint = model.ImmigrationCheckpoint;
            data.PermittedUntil = model.PermittedUntil;
            data.ResidenceNo = model.ResidenceNo;
            data.ResidenceIssuedAt = model.ResidenceIssuedAt;
            data.ResidenceProvince = model.ResidenceProvince;
            data.ResidenceDateOfIssue = model.ResidenceDateOfIssue;
            data.ResidenceExpiryDate = model.ResidenceExpiryDate;
            data.AlienNo = model.AlienNo;
            data.AlienIssuedAt = model.AlienIssuedAt;
            data.AlienProvince = model.AlienProvince;
            data.AlienDateOfIssue = model.AlienDateOfIssue;
            data.AlienExpiryDate = model.AlienExpiryDate;
            data.WorkPermitNumber = model.WorkPermitNumber;
            data.WorkPermitDateOfIssue = model.WorkPermitDateOfIssue;
            data.WorkPermitExpiryDate = model.WorkPermitExpiryDate;
            data.WorkPermitIssuedAtProvince = model.WorkPermitIssuedAtProvince;
            data.WorkPermitActionType = model.WorkPermitActionType;
            data.UpdatedAt = DateTime.Now;
            data.UserManageID = int.Parse(User.GetLoggedInUserID());

            _db.ServiceWorkers.Update(data);

            // Update Total Sum Fee
            var service = await _db.Services
                .Include(s => s.ServiceWorkers) // Ensure ServiceWorkers are included in the query
                .FirstOrDefaultAsync(s => s.ServiceID == model.ServiceID);

            if (service != null && service.ServiceWorkers != null)
            {
                // Calculate the total sum of ServiceFee for all related ServiceWorkers
                service.TotalPrice = service.ServiceWorkers.Where(s => s.IsActive).Sum(worker => worker.ServiceFee ?? 0);

                // Update the service
                _db.Services.Update(service);
            }

            try
            {
                await _db.SaveChangesAsync();

                // Log the update
                var logEntry = new LogSystemData
                {
                    TableName = "ServiceWorkers",
                    Action = "Update",
                    RecordID = data.ServiceWorkerID,
                    UserManageID = data.UserManageID,
                    ActionTime = DateTime.Now,
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    OldValue = Newtonsoft.Json.JsonConvert.SerializeObject(oldValues),
                    NewValue = Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        data.Nationality,
                        data.Title,
                        data.FirstNameEN,
                        data.LastNameEN,
                        data.ServiceFee,
                        data.Expiry90Days,
                        data.Note,
                        data.DateOfBirth,
                        data.BloodType,
                        data.PassportNumber,
                        data.PassportDateOfIssue,
                        data.PassportExpiryDate,
                        data.PassportIssuedAt,
                        data.TypeVisa,
                        data.VisaNumber,
                        data.VisaDateOfIssue,
                        data.VisaExpiryDate,
                        data.VisaIssuedAt,
                        data.PlaceOfBirth,
                        data.Country,
                        data.DateOfArrival,
                        data.ImmigrationCheckpoint,
                        data.PermittedUntil,
                        data.ResidenceNo,
                        data.ResidenceIssuedAt,
                        data.ResidenceProvince,
                        data.ResidenceDateOfIssue,
                        data.ResidenceExpiryDate,
                        data.AlienNo,
                        data.AlienIssuedAt,
                        data.AlienProvince,
                        data.AlienDateOfIssue,
                        data.AlienExpiryDate,
                        data.WorkPermitNumber,
                        data.WorkPermitDateOfIssue,
                        data.WorkPermitExpiryDate,
                        data.WorkPermitIssuedAtProvince,
                        data.WorkPermitActionType
                    }),
                    Description = $"Updated service worker with ID: {model.ServiceWorkerID}"
                };

                _db.LogSystemDatas.Add(logEntry);
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกข้อมูล", error = ex.Message });
            }
        }
        #endregion

        #region Get ServiceWorker Details
        [HttpPost]
        public JsonResult GetServiceWorkerDetails(int ServiceWorkerID)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("ReadServices"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            var model = _db.ServiceWorkers
                .Where(p => p.ServiceWorkerID == ServiceWorkerID)
                .Select(s => new
                {
                    s.ServiceWorkerID,
                    s.ServiceID,
                    s.Nationality,
                    s.BloodType,
                    s.Title,
                    s.FirstNameEN,
                    s.LastNameEN,
                    s.ServiceFee,
                    s.Expiry90Days,
                    s.Note,
                    s.DateOfBirth,
                    s.PassportNumber,
                    s.PassportDateOfIssue,
                    s.PassportExpiryDate,
                    s.WorkPermitNumber,
                    s.WorkPermitActionType,
                    s.WorkPermitExpiryDate,
                    s.WorkPermitDateOfIssue,
                    s.WorkPermitIssuedAtProvince,
                    s.VisaNumber,
                    s.TypeVisa,
                    s.VisaDateOfIssue,
                    s.VisaExpiryDate,
                    s.VisaIssuedAt,
                    s.PlaceOfBirth,
                    s.PassportIssuedAt,
                    s.Country,
                    s.DateOfArrival,
                    s.ImmigrationCheckpoint,
                    s.PermittedUntil,
                    s.ResidenceNo,
                    s.ResidenceIssuedAt,
                    s.ResidenceProvince,
                    s.ResidenceDateOfIssue,
                    s.ResidenceExpiryDate,
                    s.AlienNo,
                    s.AlienIssuedAt,
                    s.AlienProvince,
                    s.AlienDateOfIssue,
                    s.AlienExpiryDate,
                    s.CreatedAt,
                    s.UpdatedAt,
                    UserCreate = s.User.FullName,
                    s.IsActive
                })
                .FirstOrDefault();

            if (model == null)
            {
                return Json(new { success = false, message = "Service worker not found" });
            }

            return Json(new { success = true, data = model });
        }
        #endregion
        #endregion

        #region Document PDF
        [HttpPost]
        public IActionResult BT25(List<int> serviceWorkerIDs)
        {
            // ระบุเส้นทางไฟล์ PDF ที่ต้องการแก้ไข
            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "document", "บต.25.pdf");

            // ตรวจสอบว่ามีไฟล์อยู่หรือไม่
            if (!System.IO.File.Exists(absolutePath))
            {
                return Json(new { success = false, message = "ไม่พบไฟล์ PDF ที่ระบุ" });
            }

            // ดึงข้อมูลพนักงานตาม ID ที่เลือก
            var worker = _db.ServiceWorkers
                .Where(sw => serviceWorkerIDs.Contains(sw.ServiceWorkerID))
                .Select(s => new
                {
                    s.Title,
                    s.FirstNameEN,
                    s.LastNameEN,
                    s.PlaceOfBirth,
                    s.Country,
                    s.Nationality,
                    s.DateOfBirth,
                    EmployerName = s.Service.Employer.NameTh,
                    BusinessTypeName = s.Service.Employer.BusinessTypeName,
                    JobTypeName = s.Service.Employer.JobTypeName,
                    JobDiscription = s.Service.Employer.JobDiscription,
                    HouseNo = s.Service.Employer.HouseNo,
                    VillageNo = s.Service.Employer.VillageNo,
                    Soi = s.Service.Employer.Soi,
                    Road = s.Service.Employer.Road,
                    Subdistrict = s.Service.Employer.SubdistrictTh,
                    District = s.Service.Employer.DistrictTh,
                    Province = s.Service.Employer.ProvinceTh,
                    Postcode = s.Service.Employer.Postcode,
                    Phone = s.Service.Employer.Phone,
                    Fax = s.Service.Employer.Fax,
                    Email = s.Service.Employer.Email,
                    s.PassportNumber,
                    s.PassportDateOfIssue,
                    s.PassportExpiryDate,
                    s.PassportIssuedAt,
                    s.TypeVisa,
                    s.VisaNumber,
                    s.VisaExpiryDate,
                    s.VisaDateOfIssue,
                    s.VisaIssuedAt,
                    s.ImmigrationCheckpoint,
                    s.DateOfArrival,
                    s.PermittedUntil,
                    s.ResidenceNo,
                    s.ResidenceIssuedAt,
                    s.ResidenceProvince,
                    s.ResidenceDateOfIssue,
                    s.ResidenceExpiryDate,
                    s.AlienNo,
                    s.AlienIssuedAt,
                    s.AlienProvince,
                    s.AlienDateOfIssue,
                    s.AlienExpiryDate,
                    s.WorkPermitActionType,
                    s.WorkPermitNumber,
                    s.WorkPermitDateOfIssue,
                    s.WorkPermitExpiryDate,
                    s.WorkPermitIssuedAtProvince

                })
                .FirstOrDefault();

            // ตรวจสอบว่ามีพนักงานที่ตรงกับ ID ที่เลือกหรือไม่
            if (worker == null)
            {
                return Json(new { success = false, message = "ไม่พบข้อมูล ServiceWorkers ที่เลือก" });
            }

            try
            {
                // ขั้นตอนการสร้าง MemoryStream สำหรับผลลัพธ์
                byte[] pdfBytes;

                using (var memoryStream = new MemoryStream())
                {
                    // เปิดไฟล์ PDF ที่มีอยู่ด้วย PdfReader และสร้าง PdfWriter สำหรับ MemoryStream
                    using (var pdfReader = new PdfReader(absolutePath))
                    using (var pdfWriter = new PdfWriter(memoryStream))
                    {
                        var pdfDocument = new PdfDocument(pdfReader, pdfWriter);

                        // เข้าถึงหน้าที่หนึ่งของไฟล์ PDF
                        #region Page One
                        var page_one = pdfDocument.GetFirstPage();
                        var pdfCanvas = new PdfCanvas(page_one);
                        pdfCanvas.BeginText()
                            .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 16) // ระบุฟอนต์ "Thai Sarabun"
                            .MoveText(250, 453)
                            .ShowText($"{worker.Title ?? ""} {worker.FirstNameEN ?? ""} {worker.LastNameEN ?? ""}")
                            .MoveText(-125, -32)
                            .ShowText($"{worker.Nationality ?? ""}")
                            .MoveText(215, 0)
                            .ShowText($"{worker.DateOfBirth?.ToString("dd/MM/yyyy") ?? ""}")
                            .MoveText(150, 0)
                            .ShowText($"{(DateTime.Now.Year - worker.DateOfBirth.Value.Year)}")
                            .MoveText(-325, -38)
                            .ShowText($"{worker.HouseNo ?? ""}")
                            .MoveText(120, 0)
                            .ShowText($"{worker.VillageNo ?? ""}")
                            .MoveText(150, 0)
                            .ShowText($"{worker.Soi ?? ""}")
                            .MoveText(-340, -32)
                            .ShowText($"{worker.Road ?? ""}")
                            .MoveText(170, 0)
                            .ShowText($"{worker.Subdistrict ?? ""}")
                            .MoveText(170, 0)
                            .ShowText($"{worker.District ?? ""}")
                            .MoveText(-340, -32)
                            .ShowText($"{worker.Province ?? ""}")
                            .MoveText(175, 0)
                            .ShowText($"{worker.Postcode ?? ""}")
                            .MoveText(130, 0)
                            .ShowText($"{worker.Phone ?? ""}")
                            .MoveText(-300, -32)
                            .ShowText($"{worker.Fax ?? ""}")
                            .MoveText(240, 0)
                            .ShowText($"{worker.Email ?? ""}")
                            .MoveText(-250, -108)
                            .ShowText($"{worker.PassportNumber ?? ""}")
                            .MoveText(170, 0)
                            .ShowText($"{worker.PassportIssuedAt ?? ""}")
                            .MoveText(150, 0)
                            .ShowText($"{worker.Country ?? ""}")
                            .MoveText(-290, -32)
                            .ShowText($"{worker.PassportDateOfIssue?.ToString("dd/MM/yyyy") ?? ""}")
                            .MoveText(240, 0)
                            .ShowText($"{worker.PassportExpiryDate?.ToString("dd/MM/yyyy") ?? ""}")
                            .MoveText(-210, -32)
                            .ShowText($"{worker.TypeVisa ?? ""}")
                            .MoveText(150, 0)
                            .ShowText($"{worker.VisaNumber ?? ""}")
                            .MoveText(150, 0)
                            .ShowText($"{worker.VisaIssuedAt ?? ""}")
                            .MoveText(-320, -32)
                            .ShowText($"{worker.VisaDateOfIssue?.ToString("dd/MM/yyyy") ?? ""}")
                            .MoveText(240, 0)
                            .ShowText($"{worker.VisaExpiryDate?.ToString("dd/MM/yyyy") ?? ""}")
                            .EndText();
                        #endregion Page One
                        #region Page Two
                        var pageTwo = pdfDocument.GetPage(2); // ดึงหน้าที่สอง
                        if (pageTwo != null)
                        {
                            var pdfCanvasPageTwo = new PdfCanvas(pageTwo);
                            pdfCanvasPageTwo.BeginText()
                                .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 16)
                                .MoveText(210, 770) // วางตำแหน่งข้อความในหน้าที่สอง
                                .ShowText($"{worker.ImmigrationCheckpoint ?? ""}")
                                .MoveText(180, -30)
                                .ShowText($"{worker.DateOfArrival?.ToString("dd/MM/yyyy") ?? ""}")
                                .MoveText(-190, -30)
                                .ShowText($"{worker.DateOfArrival?.ToString("dd/MM/yyyy") ?? ""}")
                                .MoveText(-100, -65)
                                .ShowText($"{worker.ResidenceNo ?? ""}")
                                .MoveText(160, 0)
                                .ShowText($"{worker.ResidenceIssuedAt ?? ""}")
                                .MoveText(130, 0)
                                .ShowText($"{worker.ResidenceProvince ?? ""}")
                                .MoveText(-260, -29)
                                .ShowText($"{worker.ResidenceDateOfIssue?.ToString("dd/MM/yyyy") ?? ""}")
                                .MoveText(230, 0)
                                .ShowText($"{worker.ResidenceExpiryDate?.ToString("dd/MM/yyyy") ?? ""}")
                                .MoveText(-260, -64)
                                .ShowText($"{worker.AlienNo ?? ""}")
                                .MoveText(160, 0)
                                .ShowText($"{worker.AlienIssuedAt ?? ""}")
                                .MoveText(140, 0)
                                .ShowText($"{worker.AlienProvince ?? ""}")
                                .MoveText(-270, -29)
                                .ShowText($"{worker.AlienDateOfIssue?.ToString("dd/MM/yyyy") ?? ""}")
                                .MoveText(230, 0)
                                .ShowText($"{worker.AlienExpiryDate?.ToString("dd/MM/yyyy") ?? ""}")
                                .MoveText((worker.WorkPermitActionType == "ขออนุญาตทำงาน" ? 50 : -160), (worker.WorkPermitActionType == "ขออนุญาตทำงาน" ? -112 : -170))
                                .ShowText($"{worker.WorkPermitNumber ?? ""}")
                                .MoveText(180, 0)
                                .ShowText($"{(worker.WorkPermitActionType == "ขออนุญาตทำงาน" ? "" : worker.WorkPermitDateOfIssue?.ToString("dd/MM/yyyy"))}")
                                .MoveText(-210, -30)
                                .ShowText($"{(worker.WorkPermitActionType == "ขออนุญาตทำงาน" ? "" : worker.WorkPermitIssuedAtProvince)}")
                                .MoveText(210, 0)
                                .ShowText($"{(worker.WorkPermitActionType == "ขออนุญาตทำงาน" ? "" : worker.WorkPermitExpiryDate?.ToString("dd/MM/yyyy"))}")
                                .MoveText(-200, -102)
                                .ShowText($"{worker.JobTypeName ?? ""}")
                                .MoveText(-20, -31)
                                .ShowText($"{worker.JobDiscription ?? ""}")
                                .EndText();
                        }
                        #endregion Page Two
                        #region Page Three
                        var pageThree = pdfDocument.GetPage(3); // ดึงหน้าที่สอง
                        if (pageThree != null)
                        {
                            var pdfCanvasPageThree = new PdfCanvas(pageThree);
                            pdfCanvasPageThree.BeginText()
                                .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 16)
                                .MoveText(140, 770) // วางตำแหน่งข้อความในหน้าที่สอง
                                .ShowText($"{worker.EmployerName ?? ""}")
                                .MoveText(-30, -30)
                                .ShowText($"{worker.HouseNo ?? ""}")
                                .MoveText(130, 0)
                                .ShowText($"{worker.VillageNo ?? ""}")
                                .MoveText(150, 0)
                                .ShowText($"{worker.Soi ?? ""}")
                                .MoveText(-300, -30)
                                .ShowText($"{worker.Road ?? ""}")
                                .MoveText(160, 0)
                                .ShowText($"{worker.Subdistrict ?? ""}")
                                .MoveText(170, 0)
                                .ShowText($"{worker.District ?? ""}")
                                .MoveText(-320, -30)
                                .ShowText($"{worker.Province ?? ""}")
                                .MoveText(145, 0)
                                .ShowText($"{worker.Postcode ?? ""}")
                                .MoveText(80, 0)
                                .ShowText($"{worker.Phone ?? ""}")
                                .MoveText(130, 0)
                                .ShowText($"{worker.Fax ?? ""}")
                                .MoveText(-255, -36)
                                .ShowText($"{worker.HouseNo ?? ""}")
                                .MoveText(120, 0)
                                .ShowText($"{worker.VillageNo ?? ""}")
                                .MoveText(120, 0)
                                .ShowText($"{worker.Soi ?? ""}")
                                .MoveText(-350, -30)
                                .ShowText($"{worker.Road ?? ""}")
                                .MoveText(160, 0)
                                .ShowText($"{worker.Subdistrict ?? ""}")
                                .MoveText(170, 0)
                                .ShowText($"{worker.District ?? ""}")
                                .MoveText(-320, -30)
                                .ShowText($"{worker.Province ?? ""}")
                                .MoveText(145, 0)
                                .ShowText($"{worker.Postcode ?? ""}")
                                .MoveText(90, 0)
                                .ShowText($"{worker.Phone ?? ""}")
                                .MoveText(140, 0)
                                .ShowText($"{worker.Fax ?? ""}")
                                .EndText();
                        }
                        #endregion Page Three
                        pdfDocument.Close(); // ปิด PdfDocument
                    }

                    // คัดลอกข้อมูลใน MemoryStream ไปยัง byte array
                    pdfBytes = memoryStream.ToArray();
                }

                // ส่งไฟล์ PDF ที่แก้ไขแล้วกลับไปยังผู้ใช้เพื่อดาวน์โหลด
                string fileName = "UpdatedDocument.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (FileNotFoundException ex)
            {
                // บันทึกข้อความข้อผิดพลาดเมื่อไม่พบไฟล์
                Console.WriteLine($"File Not Found Exception: {ex.Message}");
                return Json(new { success = false, message = "ไม่พบไฟล์ PDF ที่ระบุ" });
            }
            catch (iText.Kernel.Exceptions.PdfException ex)
            {
                // บันทึกข้อความข้อผิดพลาดที่เกี่ยวข้องกับ PDF
                Console.WriteLine($"PDF Exception: {ex.Message}");
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการจัดการ PDF", error = ex.Message });
            }
            catch (Exception ex)
            {
                // บันทึกข้อความข้อผิดพลาดทั่วไป
                Console.WriteLine($"General Exception: {ex.Message}");
                return Json(new { success = false, message = "เกิดข้อผิดพลาดทั่วไป", error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult BT30(List<int> serviceWorkerIDs)
        {
            // ระบุเส้นทางไฟล์ PDF ที่ต้องการแก้ไข
            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "document", "บต.30.pdf");

            // ตรวจสอบว่ามีไฟล์อยู่หรือไม่
            if (!System.IO.File.Exists(absolutePath))
            {
                return Json(new { success = false, message = "ไม่พบไฟล์ PDF ที่ระบุ" });
            }

            // ดึงข้อมูลพนักงานตาม ID ที่เลือก
            var worker = _db.ServiceWorkers
                .Where(sw => serviceWorkerIDs.Contains(sw.ServiceWorkerID))
                .Select(s => new
                {
                    s.Title,
                    s.FirstNameEN,
                    s.LastNameEN,
                    s.PlaceOfBirth,
                    s.Country,
                    s.Nationality,
                    s.DateOfBirth,
                    EmployerName = s.Service.Employer.NameTh,
                    BusinessTypeName = s.Service.Employer.BusinessTypeName,
                    JobTypeName = s.Service.Employer.JobTypeName,
                    JobDiscription = s.Service.Employer.JobDiscription,
                    HouseNo = s.Service.Employer.HouseNo,
                    VillageNo = s.Service.Employer.VillageNo,
                    Soi = s.Service.Employer.Soi,
                    Road = s.Service.Employer.Road,
                    Subdistrict = s.Service.Employer.SubdistrictTh,
                    District = s.Service.Employer.DistrictTh,
                    Province = s.Service.Employer.ProvinceTh,
                    Postcode = s.Service.Employer.Postcode,
                    Phone = s.Service.Employer.Phone,
                    Fax = s.Service.Employer.Fax,
                    Email = s.Service.Employer.Email,
                    s.PassportNumber,
                    s.PassportDateOfIssue,
                    s.PassportExpiryDate,
                    s.PassportIssuedAt,
                    s.TypeVisa,
                    s.VisaNumber,
                    s.VisaExpiryDate,
                    s.VisaDateOfIssue,
                    s.VisaIssuedAt,
                    s.ImmigrationCheckpoint,
                    s.DateOfArrival,
                    s.PermittedUntil,
                    s.ResidenceNo,
                    s.ResidenceIssuedAt,
                    s.ResidenceProvince,
                    s.ResidenceDateOfIssue,
                    s.ResidenceExpiryDate,
                    s.AlienNo,
                    s.AlienIssuedAt,
                    s.AlienProvince,
                    s.AlienDateOfIssue,
                    s.AlienExpiryDate,
                    s.WorkPermitActionType,
                    s.WorkPermitNumber,
                    s.WorkPermitDateOfIssue,
                    s.WorkPermitExpiryDate,
                    s.WorkPermitIssuedAtProvince

                })
                .FirstOrDefault();

            // ตรวจสอบว่ามีพนักงานที่ตรงกับ ID ที่เลือกหรือไม่
            if (worker == null)
            {
                return Json(new { success = false, message = "ไม่พบข้อมูล ServiceWorkers ที่เลือก" });
            }

            try
            {
                // ขั้นตอนการสร้าง MemoryStream สำหรับผลลัพธ์
                byte[] pdfBytes;

                using (var memoryStream = new MemoryStream())
                {
                    // เปิดไฟล์ PDF ที่มีอยู่ด้วย PdfReader และสร้าง PdfWriter สำหรับ MemoryStream
                    using (var pdfReader = new PdfReader(absolutePath))
                    using (var pdfWriter = new PdfWriter(memoryStream))
                    {
                        var pdfDocument = new PdfDocument(pdfReader, pdfWriter);

                        // เข้าถึงหน้าที่หนึ่งของไฟล์ PDF
                        #region Page One
                        var page_one = pdfDocument.GetFirstPage();
                        var pdfCanvas = new PdfCanvas(page_one);
                        pdfCanvas.BeginText()
                            .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 16) // ระบุฟอนต์ "Thai Sarabun"
                            .MoveText(250, 533)
                            .ShowText($"{worker.Title ?? ""} {worker.FirstNameEN ?? ""} {worker.LastNameEN ?? ""}")
                            .MoveText(-125, -30)
                            .ShowText($"{worker.Nationality ?? ""}")
                            .MoveText(215, 0)
                            .ShowText($"{worker.DateOfBirth?.ToString("dd/MM/yyyy") ?? ""}")
                            .MoveText(150, 0)
                            .ShowText($"{(DateTime.Now.Year - worker.DateOfBirth.Value.Year)}")
                            .MoveText(-325, -36)
                            .ShowText($"{worker.HouseNo ?? ""}")
                            .MoveText(120, 0)
                            .ShowText($"{worker.VillageNo ?? ""}")
                            .MoveText(150, 0)
                            .ShowText($"{worker.Soi ?? ""}")
                            .MoveText(-340, -30)
                            .ShowText($"{worker.Road ?? ""}")
                            .MoveText(170, 0)
                            .ShowText($"{worker.Subdistrict ?? ""}")
                            .MoveText(170, 0)
                            .ShowText($"{worker.District ?? ""}")
                            .MoveText(-340, -30)
                            .ShowText($"{worker.Province ?? ""}")
                            .MoveText(175, 0)
                            .ShowText($"{worker.Postcode ?? ""}")
                            .MoveText(130, 0)
                            .ShowText($"{worker.Phone ?? ""}")
                            .MoveText(-300, -30)
                            .ShowText($"{worker.Fax ?? ""}")
                            .MoveText(240, 0)
                            .ShowText($"{worker.Email ?? ""}")
                            .MoveText(-250, -102)
                            .ShowText($"{worker.PassportNumber ?? ""}")
                            .MoveText(170, 0)
                            .ShowText($"{worker.PassportIssuedAt ?? ""}")
                            .MoveText(150, 0)
                            .ShowText($"{worker.Country ?? ""}")
                            .MoveText(-290, -30)
                            .ShowText($"{worker.PassportDateOfIssue?.ToString("dd/MM/yyyy") ?? ""}")
                            .MoveText(240, 0)
                            .ShowText($"{worker.PassportExpiryDate?.ToString("dd/MM/yyyy") ?? ""}")
                            .MoveText(-210, -30)
                            .ShowText($"{worker.TypeVisa ?? ""}")
                            .MoveText(150, 0)
                            .ShowText($"{worker.VisaNumber ?? ""}")
                            .MoveText(150, 0)
                            .ShowText($"{worker.VisaIssuedAt ?? ""}")
                            .MoveText(-320, -30)
                            .ShowText($"{worker.VisaDateOfIssue?.ToString("dd/MM/yyyy") ?? ""}")
                            .MoveText(240, 0)
                            .ShowText($"{worker.VisaExpiryDate?.ToString("dd/MM/yyyy") ?? ""}")
                            .MoveText(-150, -30)
                            .ShowText($"{worker.ImmigrationCheckpoint ?? ""}")
                            .MoveText(180, -30)
                            .ShowText($"{worker.DateOfArrival?.ToString("dd/MM/yyyy") ?? ""}")
                            .MoveText(-190, -30)
                            .ShowText($"{worker.DateOfArrival?.ToString("dd/MM/yyyy") ?? ""}")
                            .EndText();
                        #endregion Page One
                        #region Page Two
                        var pageTwo = pdfDocument.GetPage(2); // ดึงหน้าที่สอง
                        if (pageTwo != null)
                        {
                            var pdfCanvasPageTwo = new PdfCanvas(pageTwo);
                            pdfCanvasPageTwo.BeginText()
                                .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 16)
                                .MoveText(350, 902)
                                .MoveText(-160, -170)
                                .ShowText($"{worker.WorkPermitNumber ?? ""}")
                                .MoveText(180, 0)
                                .ShowText($"{worker.WorkPermitDateOfIssue?.ToString("dd/MM/yyyy")}")
                                .MoveText(-210, -32)
                                .ShowText($"{worker.WorkPermitIssuedAtProvince}")
                                .MoveText(210, 0)
                                .ShowText($"{worker.WorkPermitExpiryDate?.ToString("dd/MM/yyyy")}")
                                .MoveText(-200, -85)
                                .ShowText($"{worker.JobTypeName ?? ""}")
                                .MoveText(-20, -30)
                                .ShowText($"{worker.JobDiscription ?? ""}")
                                .MoveText(-20, -32)
                                .ShowText($"{worker.EmployerName ?? ""}")
                                .MoveText(-30, -32)
                                .ShowText($"{worker.HouseNo ?? ""}")
                                .MoveText(130, 0)
                                .ShowText($"{worker.VillageNo ?? ""}")
                                .MoveText(150, 0)
                                .ShowText($"{worker.Soi ?? ""}")
                                .MoveText(-300, -32)
                                .ShowText($"{worker.Road ?? ""}")
                                .MoveText(160, 0)
                                .ShowText($"{worker.Subdistrict ?? ""}")
                                .MoveText(170, 0)
                                .ShowText($"{worker.District ?? ""}")
                                .MoveText(-320, -32)
                                .ShowText($"{worker.Province ?? ""}")
                                .MoveText(145, 0)
                                .ShowText($"{worker.Postcode ?? ""}")
                                .MoveText(80, 0)
                                .ShowText($"{worker.Phone ?? ""}")
                                .MoveText(130, 0)
                                .ShowText($"{worker.Fax ?? ""}")
                                .MoveText(-255, -38)
                                .ShowText($"{worker.HouseNo ?? ""}")
                                .MoveText(120, 0)
                                .ShowText($"{worker.VillageNo ?? ""}")
                                .MoveText(120, 0)
                                .ShowText($"{worker.Soi ?? ""}")
                                .MoveText(-350, -32)
                                .ShowText($"{worker.Road ?? ""}")
                                .MoveText(160, 0)
                                .ShowText($"{worker.Subdistrict ?? ""}")
                                .MoveText(170, 0)
                                .ShowText($"{worker.District ?? ""}")
                                .MoveText(-320, -32)
                                .ShowText($"{worker.Province ?? ""}")
                                .MoveText(145, 0)
                                .ShowText($"{worker.Postcode ?? ""}")
                                .MoveText(90, 0)
                                .ShowText($"{worker.Phone ?? ""}")
                                .MoveText(140, 0)
                                .ShowText($"{worker.Fax ?? ""}")
                                .EndText();
                        }
                        #endregion Page Two
                        pdfDocument.Close(); // ปิด PdfDocument
                    }

                    // คัดลอกข้อมูลใน MemoryStream ไปยัง byte array
                    pdfBytes = memoryStream.ToArray();
                }

                // ส่งไฟล์ PDF ที่แก้ไขแล้วกลับไปยังผู้ใช้เพื่อดาวน์โหลด
                string fileName = "UpdatedDocument.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (FileNotFoundException ex)
            {
                // บันทึกข้อความข้อผิดพลาดเมื่อไม่พบไฟล์
                Console.WriteLine($"File Not Found Exception: {ex.Message}");
                return Json(new { success = false, message = "ไม่พบไฟล์ PDF ที่ระบุ" });
            }
            catch (iText.Kernel.Exceptions.PdfException ex)
            {
                // บันทึกข้อความข้อผิดพลาดที่เกี่ยวข้องกับ PDF
                Console.WriteLine($"PDF Exception: {ex.Message}");
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการจัดการ PDF", error = ex.Message });
            }
            catch (Exception ex)
            {
                // บันทึกข้อความข้อผิดพลาดทั่วไป
                Console.WriteLine($"General Exception: {ex.Message}");
                return Json(new { success = false, message = "เกิดข้อผิดพลาดทั่วไป", error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult BT33(List<int> serviceWorkerIDs)
        {
            // ระบุเส้นทางไฟล์ PDF ที่ต้องการแก้ไข
            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "document", "บต.33.pdf");

            // ตรวจสอบว่ามีไฟล์อยู่หรือไม่
            if (!System.IO.File.Exists(absolutePath))
            {
                return Json(new { success = false, message = "ไม่พบไฟล์ PDF ที่ระบุ" });
            }

            // ดึงข้อมูลพนักงานตาม ID ที่เลือก
            var worker = _db.ServiceWorkers
                .Where(sw => serviceWorkerIDs.Contains(sw.ServiceWorkerID))
                .Select(s => new
                {
                    s.Title,
                    s.FirstNameEN,
                    s.LastNameEN,
                    s.PlaceOfBirth,
                    s.Country,
                    s.Nationality,
                    s.DateOfBirth,
                    EmployerName = s.Service.Employer.NameTh,
                    BusinessTypeName = s.Service.Employer.BusinessTypeName,
                    JobTypeName = s.Service.Employer.JobTypeName,
                    JobDiscription = s.Service.Employer.JobDiscription,
                    HouseNo = s.Service.Employer.HouseNo,
                    VillageNo = s.Service.Employer.VillageNo,
                    Soi = s.Service.Employer.Soi,
                    Road = s.Service.Employer.Road,
                    Subdistrict = s.Service.Employer.SubdistrictTh,
                    District = s.Service.Employer.DistrictTh,
                    Province = s.Service.Employer.ProvinceTh,
                    Postcode = s.Service.Employer.Postcode,
                    Phone = s.Service.Employer.Phone,
                    Fax = s.Service.Employer.Fax,
                    Email = s.Service.Employer.Email,
                    s.PassportNumber,
                    s.PassportDateOfIssue,
                    s.PassportExpiryDate,
                    s.PassportIssuedAt,
                    s.TypeVisa,
                    s.VisaNumber,
                    s.VisaExpiryDate,
                    s.VisaDateOfIssue,
                    s.VisaIssuedAt,
                    s.ImmigrationCheckpoint,
                    s.DateOfArrival,
                    s.PermittedUntil,
                    s.ResidenceNo,
                    s.ResidenceIssuedAt,
                    s.ResidenceProvince,
                    s.ResidenceDateOfIssue,
                    s.ResidenceExpiryDate,
                    s.AlienNo,
                    s.AlienIssuedAt,
                    s.AlienProvince,
                    s.AlienDateOfIssue,
                    s.AlienExpiryDate,
                    s.WorkPermitActionType,
                    s.WorkPermitNumber,
                    s.WorkPermitDateOfIssue,
                    s.WorkPermitExpiryDate,
                    s.WorkPermitIssuedAtProvince

                })
                .FirstOrDefault();

            // ตรวจสอบว่ามีพนักงานที่ตรงกับ ID ที่เลือกหรือไม่
            if (worker == null)
            {
                return Json(new { success = false, message = "ไม่พบข้อมูล ServiceWorkers ที่เลือก" });
            }

            try
            {
                // ขั้นตอนการสร้าง MemoryStream สำหรับผลลัพธ์
                byte[] pdfBytes;

                using (var memoryStream = new MemoryStream())
                {
                    // เปิดไฟล์ PDF ที่มีอยู่ด้วย PdfReader และสร้าง PdfWriter สำหรับ MemoryStream
                    using (var pdfReader = new PdfReader(absolutePath))
                    using (var pdfWriter = new PdfWriter(memoryStream))
                    {
                        var pdfDocument = new PdfDocument(pdfReader, pdfWriter);

                        // เข้าถึงหน้าที่หนึ่งของไฟล์ PDF
                        #region Page One
                        var page_one = pdfDocument.GetFirstPage();
                        var pdfCanvas = new PdfCanvas(page_one);
                        pdfCanvas.BeginText()
                            .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 16) // ระบุฟอนต์ "Thai Sarabun"
                            .MoveText(140, 527)
                            .ShowText($"{worker.EmployerName ?? ""}")
                            .MoveText(-30, -30)
                            .ShowText($"{worker.HouseNo ?? ""}")
                            .MoveText(130, 0)
                            .ShowText($"{worker.VillageNo ?? ""}")
                            .MoveText(150, 0)
                            .ShowText($"{worker.Soi ?? ""}")
                            .MoveText(-300, -29)
                            .ShowText($"{worker.Road ?? ""}")
                            .MoveText(160, 0)
                            .ShowText($"{worker.Subdistrict ?? ""}")
                            .MoveText(170, 0)
                            .ShowText($"{worker.District ?? ""}")
                            .MoveText(-320, -30)
                            .ShowText($"{worker.Province ?? ""}")
                            .MoveText(145, 0)
                            .ShowText($"{worker.Postcode ?? ""}")
                            .MoveText(80, 0)
                            .ShowText($"{worker.Phone ?? ""}")
                            .MoveText(130, 0)
                            .ShowText($"{worker.Fax ?? ""}")
                            .MoveText(-200, -82)
                            .ShowText($"{worker.Title ?? ""} {worker.FirstNameEN ?? ""} {worker.LastNameEN ?? ""}")
                            .MoveText(-125, -30)
                            .ShowText($"{worker.Nationality ?? ""}")
                            .MoveText(215, 0)
                            .ShowText($"{worker.DateOfBirth?.ToString("dd/MM/yyyy") ?? ""}")
                            .MoveText(150, 0)
                            .ShowText($"{(DateTime.Now.Year - worker.DateOfBirth.Value.Year)}")
                            .MoveText(-380, -65)
                            .ShowText($"{worker.PassportNumber ?? ""}")
                            .MoveText(170, 0)
                            .ShowText($"{worker.PassportIssuedAt ?? ""}")
                            .MoveText(150, 0)
                            .ShowText($"{worker.Country ?? ""}")
                            .MoveText(-290, -29)
                            .ShowText($"{worker.PassportDateOfIssue?.ToString("dd/MM/yyyy") ?? ""}")
                            .MoveText(240, 0)
                            .ShowText($"{worker.PassportExpiryDate?.ToString("dd/MM/yyyy") ?? ""}")
                            .MoveText(-200, -84)
                            .ShowText($"{worker.JobTypeName ?? ""}")
                            .MoveText(-20, -29)
                            .ShowText($"{worker.JobDiscription ?? ""}")
                            .EndText();
                        #endregion Page One
                        #region Page Two
                        var pageTwo = pdfDocument.GetPage(2); // ดึงหน้าที่สอง
                        if (pageTwo != null)
                        {
                            var pdfCanvasPageTwo = new PdfCanvas(pageTwo);
                            pdfCanvasPageTwo.BeginText()
                                .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 16)
                                .MoveText(195, 762)
                                .ShowText($"{worker.HouseNo ?? ""}")
                                .MoveText(110, 0)
                                .ShowText($"{worker.VillageNo ?? ""}")
                                .MoveText(120, 0)
                                .ShowText($"{worker.Soi ?? ""}")
                                .MoveText(-340, -32)
                                .ShowText($"{worker.Road ?? ""}")
                                .MoveText(170, 0)
                                .ShowText($"{worker.Subdistrict ?? ""}")
                                .MoveText(170, 0)
                                .ShowText($"{worker.District ?? ""}")
                                .MoveText(-330, -32)
                                .ShowText($"{worker.Province ?? ""}")
                                .MoveText(150, 0)
                                .ShowText($"{worker.Postcode ?? ""}")
                                .MoveText(80, 0)
                                .ShowText($"{worker.Phone ?? ""}")
                                .MoveText(145, 0)
                                .ShowText($"{worker.Fax ?? ""}")
                                .EndText();
                        }
                        #endregion Page Two
                        pdfDocument.Close(); // ปิด PdfDocument
                    }

                    // คัดลอกข้อมูลใน MemoryStream ไปยัง byte array
                    pdfBytes = memoryStream.ToArray();
                }

                // ส่งไฟล์ PDF ที่แก้ไขแล้วกลับไปยังผู้ใช้เพื่อดาวน์โหลด
                string fileName = "UpdatedDocument.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (FileNotFoundException ex)
            {
                // บันทึกข้อความข้อผิดพลาดเมื่อไม่พบไฟล์
                Console.WriteLine($"File Not Found Exception: {ex.Message}");
                return Json(new { success = false, message = "ไม่พบไฟล์ PDF ที่ระบุ" });
            }
            catch (iText.Kernel.Exceptions.PdfException ex)
            {
                // บันทึกข้อความข้อผิดพลาดที่เกี่ยวข้องกับ PDF
                Console.WriteLine($"PDF Exception: {ex.Message}");
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการจัดการ PDF", error = ex.Message });
            }
            catch (Exception ex)
            {
                // บันทึกข้อความข้อผิดพลาดทั่วไป
                Console.WriteLine($"General Exception: {ex.Message}");
                return Json(new { success = false, message = "เกิดข้อผิดพลาดทั่วไป", error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult BT46(List<int> serviceWorkerIDs)
        {
            // ระบุเส้นทางไฟล์ PDF ที่ต้องการแก้ไข
            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "document", "บต.46.pdf");

            // ตรวจสอบว่ามีไฟล์อยู่หรือไม่
            if (!System.IO.File.Exists(absolutePath))
            {
                return Json(new { success = false, message = "ไม่พบไฟล์ PDF ที่ระบุ" });
            }

            // ดึงข้อมูลพนักงานตาม ID ที่เลือก
            var worker = _db.ServiceWorkers
                .Where(sw => serviceWorkerIDs.Contains(sw.ServiceWorkerID))
                .Select(s => new
                {
                    s.Title,
                    s.FirstNameEN,
                    s.LastNameEN,
                    s.PlaceOfBirth,
                    s.Country,
                    s.Nationality,
                    s.DateOfBirth,
                    s.BloodType,
                    EmployerCard = s.Service.Employer.CardID,
                    RegistrationNumber = s.Service.Employer.RegistrationNumber,
                    RegistrationDate = s.Service.Employer.RegistrationDate,
                    RegisteredCapital = s.Service.Employer.RegisteredCapital,
                    EmployerName = s.Service.Employer.NameTh,
                    BusinessTypeName = s.Service.Employer.BusinessTypeName,
                    JobTypeName = s.Service.Employer.JobTypeName,
                    JobDiscription = s.Service.Employer.JobDiscription,
                    HouseNo = s.Service.Employer.HouseNo,
                    VillageNo = s.Service.Employer.VillageNo,
                    Soi = s.Service.Employer.Soi,
                    Road = s.Service.Employer.Road,
                    Subdistrict = s.Service.Employer.SubdistrictTh,
                    District = s.Service.Employer.DistrictTh,
                    Province = s.Service.Employer.ProvinceTh,
                    Postcode = s.Service.Employer.Postcode,
                    Phone = s.Service.Employer.Phone,
                    Fax = s.Service.Employer.Fax,
                    Email = s.Service.Employer.Email,
                    s.PassportNumber,
                    s.PassportDateOfIssue,
                    s.PassportExpiryDate,
                    s.PassportIssuedAt,
                    s.TypeVisa,
                    s.VisaNumber,
                    s.VisaExpiryDate,
                    s.VisaDateOfIssue,
                    s.VisaIssuedAt,
                    s.ImmigrationCheckpoint,
                    s.DateOfArrival,
                    s.PermittedUntil,
                    s.ResidenceNo,
                    s.ResidenceIssuedAt,
                    s.ResidenceProvince,
                    s.ResidenceDateOfIssue,
                    s.ResidenceExpiryDate,
                    s.AlienNo,
                    s.AlienIssuedAt,
                    s.AlienProvince,
                    s.AlienDateOfIssue,
                    s.AlienExpiryDate,
                    s.WorkPermitActionType,
                    s.WorkPermitNumber,
                    s.WorkPermitDateOfIssue,
                    s.WorkPermitExpiryDate,
                    s.WorkPermitIssuedAtProvince

                })
                .FirstOrDefault();

            // ตรวจสอบว่ามีพนักงานที่ตรงกับ ID ที่เลือกหรือไม่
            if (worker == null)
            {
                return Json(new { success = false, message = "ไม่พบข้อมูล ServiceWorkers ที่เลือก" });
            }

            try
            {
                // ขั้นตอนการสร้าง MemoryStream สำหรับผลลัพธ์
                byte[] pdfBytes;

                using (var memoryStream = new MemoryStream())
                {
                    // เปิดไฟล์ PDF ที่มีอยู่ด้วย PdfReader และสร้าง PdfWriter สำหรับ MemoryStream
                    using (var pdfReader = new PdfReader(absolutePath))
                    using (var pdfWriter = new PdfWriter(memoryStream))
                    {
                        var pdfDocument = new PdfDocument(pdfReader, pdfWriter);

                        // เข้าถึงหน้าที่หนึ่งของไฟล์ PDF
                        #region Page One
                        var page_one = pdfDocument.GetFirstPage();
                        var pdfCanvas = new PdfCanvas(page_one);
                        pdfCanvas.BeginText()
                            .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 16) // ระบุฟอนต์ "Thai Sarabun"
                            .MoveText(210, 749)
                            .ShowText($"{(worker.RegistrationNumber == null ? "" : worker.RegistrationDate)}")
                            .MoveText(100, 10)
                            .ShowText($"{(worker.RegistrationNumber == null ? "" : worker.RegistrationNumber)}")
                            .MoveText(160, -10)
                            .ShowText($"{(worker.RegistrationNumber == null ? "" : ((int)worker.RegisteredCapital).ToString("#,##0"))}")
                            .MoveText(-200, -55)
                            .ShowText($"{(worker.RegistrationNumber == null ? worker.EmployerCard : "")}")
                            .MoveText(0, -28)
                            .ShowText($"{worker.EmployerName ?? ""}")
                            .MoveText(-80, -15)
                            .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 14) // ระบุฟอนต์ "Thai Sarabun"
                            .ShowText($"{"บ้านเลขที่ " + worker.HouseNo + " หมู่ที่ " + worker.VillageNo + "     ซอย " + (worker.Soi == "" ? "-" : worker.Soi) + "      ถนน " + (worker.Road == "" ? "-" : worker.Road)}")
                            .MoveText(-120, -13)
                            .ShowText($"{"ตำบล" + worker.Subdistrict + "    อำเภอ" + worker.District + "    จังหวัด" + worker.Province + "    รหัสไปรษณีย์ " + worker.Postcode}")
                            .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 16) // ระบุฟอนต์ "Thai Sarabun"
                            .MoveText(120, -15)
                            .ShowText($"{worker.BusinessTypeName ?? ""}")
                            .MoveText(150, -208)
                            .ShowText($"{(worker.Title ?? "") + (worker.FirstNameEN ?? "") + " " + (worker.LastNameEN ?? "")}")
                            .MoveText(-180, -16)
                            .ShowText($"{worker.Nationality ?? ""}")
                            .MoveText(240, 0)
                            .ShowText($"{worker.BloodType ?? ""}")
                            .MoveText(-190, -16)
                            .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 14) // ระบุฟอนต์ "Thai Sarabun"
                            .ShowText($"{"บ้านเลขที่ " + worker.HouseNo + " หมู่ที่ " + worker.VillageNo + (worker.Soi == "" ? "" : " ซอย " + worker.Soi) + (worker.Road == "" ? "" : " ถนน " + worker.Road)}")
                            .ShowText($"{" ตำบล" + worker.Subdistrict + " อำเภอ" + worker.District + " จังหวัด" + worker.Province + " รหัสไปรษณีย์ " + worker.Postcode}")
                            .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 16) // ระบุฟอนต์ "Thai Sarabun"
                            .MoveText(-30, -15)
                            .ShowText($"{worker.JobTypeName ?? ""}")
                            .MoveText(0, -15)
                            .ShowText($"{worker.JobDiscription ?? ""}")
                            .MoveText(110, -29)
                            .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 14) // ระบุฟอนต์ "Thai Sarabun"
                            .ShowText($"{"บ้านเลขที่ " + worker.HouseNo + " หมู่ที่ " + worker.VillageNo + (worker.Soi == "" ? "" : " ซอย " + worker.Soi) + (worker.Road == "" ? "" : " ถนน " + worker.Road)}")
                            .ShowText($"{" ตำบล" + worker.Subdistrict + " อำเภอ" + worker.District + " จังหวัด" + worker.Province}")
                            .EndText();
                        #endregion Page One
                        pdfDocument.Close(); // ปิด PdfDocument
                    }

                    // คัดลอกข้อมูลใน MemoryStream ไปยัง byte array
                    pdfBytes = memoryStream.ToArray();
                }

                // ส่งไฟล์ PDF ที่แก้ไขแล้วกลับไปยังผู้ใช้เพื่อดาวน์โหลด
                string fileName = "UpdatedDocument.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (FileNotFoundException ex)
            {
                // บันทึกข้อความข้อผิดพลาดเมื่อไม่พบไฟล์
                Console.WriteLine($"File Not Found Exception: {ex.Message}");
                return Json(new { success = false, message = "ไม่พบไฟล์ PDF ที่ระบุ" });
            }
            catch (iText.Kernel.Exceptions.PdfException ex)
            {
                // บันทึกข้อความข้อผิดพลาดที่เกี่ยวข้องกับ PDF
                Console.WriteLine($"PDF Exception: {ex.Message}");
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการจัดการ PDF", error = ex.Message });
            }
            catch (Exception ex)
            {
                // บันทึกข้อความข้อผิดพลาดทั่วไป
                Console.WriteLine($"General Exception: {ex.Message}");
                return Json(new { success = false, message = "เกิดข้อผิดพลาดทั่วไป", error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult BT47(List<int> serviceWorkerIDs)
        {
            // ระบุเส้นทางไฟล์ PDF ที่ต้องการแก้ไข
            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "document", "บต.47.pdf");

            // ตรวจสอบว่ามีไฟล์อยู่หรือไม่
            if (!System.IO.File.Exists(absolutePath))
            {
                return Json(new { success = false, message = "ไม่พบไฟล์ PDF ที่ระบุ" });
            }

            // ดึงข้อมูลพนักงานตาม ID ที่เลือก
            var worker = _db.ServiceWorkers
                .Where(sw => serviceWorkerIDs.Contains(sw.ServiceWorkerID))
                .Select(s => new
                {
                    s.Title,
                    s.FirstNameEN,
                    s.LastNameEN,
                    s.PlaceOfBirth,
                    s.Country,
                    s.Nationality,
                    s.DateOfBirth,
                    s.BloodType,
                    EmployerCard = s.Service.Employer.CardID,
                    RegistrationNumber = s.Service.Employer.RegistrationNumber,
                    RegistrationDate = s.Service.Employer.RegistrationDate,
                    RegisteredCapital = s.Service.Employer.RegisteredCapital,
                    EmployerName = s.Service.Employer.NameTh,
                    BusinessTypeName = s.Service.Employer.BusinessTypeName,
                    JobTypeName = s.Service.Employer.JobTypeName,
                    JobDiscription = s.Service.Employer.JobDiscription,
                    HouseNo = s.Service.Employer.HouseNo,
                    VillageNo = s.Service.Employer.VillageNo,
                    Soi = s.Service.Employer.Soi,
                    Road = s.Service.Employer.Road,
                    Subdistrict = s.Service.Employer.SubdistrictTh,
                    District = s.Service.Employer.DistrictTh,
                    Province = s.Service.Employer.ProvinceTh,
                    Postcode = s.Service.Employer.Postcode,
                    Phone = s.Service.Employer.Phone,
                    Fax = s.Service.Employer.Fax,
                    Email = s.Service.Employer.Email,
                    s.PassportNumber,
                    s.PassportDateOfIssue,
                    s.PassportExpiryDate,
                    s.PassportIssuedAt,
                    s.TypeVisa,
                    s.VisaNumber,
                    s.VisaExpiryDate,
                    s.VisaDateOfIssue,
                    s.VisaIssuedAt,
                    s.ImmigrationCheckpoint,
                    s.DateOfArrival,
                    s.PermittedUntil,
                    s.ResidenceNo,
                    s.ResidenceIssuedAt,
                    s.ResidenceProvince,
                    s.ResidenceDateOfIssue,
                    s.ResidenceExpiryDate,
                    s.AlienNo,
                    s.AlienIssuedAt,
                    s.AlienProvince,
                    s.AlienDateOfIssue,
                    s.AlienExpiryDate,
                    s.WorkPermitActionType,
                    s.WorkPermitNumber,
                    s.WorkPermitDateOfIssue,
                    s.WorkPermitExpiryDate,
                    s.WorkPermitIssuedAtProvince

                })
                .FirstOrDefault();

            // ตรวจสอบว่ามีพนักงานที่ตรงกับ ID ที่เลือกหรือไม่
            if (worker == null)
            {
                return Json(new { success = false, message = "ไม่พบข้อมูล ServiceWorkers ที่เลือก" });
            }

            try
            {
                // ขั้นตอนการสร้าง MemoryStream สำหรับผลลัพธ์
                byte[] pdfBytes;

                using (var memoryStream = new MemoryStream())
                {
                    // เปิดไฟล์ PDF ที่มีอยู่ด้วย PdfReader และสร้าง PdfWriter สำหรับ MemoryStream
                    using (var pdfReader = new PdfReader(absolutePath))
                    using (var pdfWriter = new PdfWriter(memoryStream))
                    {
                        var pdfDocument = new PdfDocument(pdfReader, pdfWriter);

                        // เข้าถึงหน้าที่หนึ่งของไฟล์ PDF
                        #region Page One
                        var page_one = pdfDocument.GetFirstPage();
                        var pdfCanvas = new PdfCanvas(page_one);
                        pdfCanvas.BeginText()
                            .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 16) // ระบุฟอนต์ "Thai Sarabun"
                            .MoveText(210, 749)
                            .ShowText($"{(worker.RegistrationNumber == null ? "" : worker.RegistrationDate)}")
                            .MoveText(100, 10)
                            .ShowText($"{(worker.RegistrationNumber == null ? "" : worker.RegistrationNumber)}")
                            .MoveText(160, -10)
                            .ShowText($"{(worker.RegistrationNumber == null ? "" : ((int)worker.RegisteredCapital).ToString("#,##0"))}")
                            .MoveText(-200, -55)
                            .ShowText($"{(worker.RegistrationNumber == null ? worker.EmployerCard : "")}")
                            .MoveText(0, -28)
                            .ShowText($"{worker.EmployerName ?? ""}")
                            .MoveText(-80, -15)
                            .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 14) // ระบุฟอนต์ "Thai Sarabun"
                            .ShowText($"{"บ้านเลขที่ " + worker.HouseNo + " หมู่ที่ " + worker.VillageNo + "     ซอย " + (worker.Soi == "" ? "-" : worker.Soi) + "      ถนน " + (worker.Road == "" ? "-" : worker.Road)}")
                            .MoveText(-120, -13)
                            .ShowText($"{"ตำบล" + worker.Subdistrict + "    อำเภอ" + worker.District + "    จังหวัด" + worker.Province + "    รหัสไปรษณีย์ " + worker.Postcode}")
                            .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 16) // ระบุฟอนต์ "Thai Sarabun"
                            .MoveText(120, -15)
                            .ShowText($"{worker.BusinessTypeName ?? ""}")
                            .MoveText(150, -208)
                            .ShowText($"{(worker.Title ?? "") + (worker.FirstNameEN ?? "") + " " + (worker.LastNameEN ?? "")}")
                            .MoveText(-180, -16)
                            .ShowText($"{worker.Nationality ?? ""}")
                            .MoveText(240, 0)
                            .ShowText($"{worker.BloodType ?? ""}")
                            .MoveText(-190, -16)
                            .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 14) // ระบุฟอนต์ "Thai Sarabun"
                            .ShowText($"{"บ้านเลขที่ " + worker.HouseNo + " หมู่ที่ " + worker.VillageNo + (worker.Soi == "" ? "" : " ซอย " + worker.Soi) + (worker.Road == "" ? "" : " ถนน " + worker.Road)}")
                            .ShowText($"{" ตำบล" + worker.Subdistrict + " อำเภอ" + worker.District + " จังหวัด" + worker.Province + " รหัสไปรษณีย์ " + worker.Postcode}")
                            .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 16) // ระบุฟอนต์ "Thai Sarabun"
                            .MoveText(-30, -15)
                            .ShowText($"{worker.JobTypeName ?? ""}")
                            .MoveText(0, -15)
                            .ShowText($"{worker.JobDiscription ?? ""}")
                            .MoveText(110, -29)
                            .SetFontAndSize(PdfFontFactory.CreateFont(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew Bold.ttf"), PdfEncodings.IDENTITY_H), 14) // ระบุฟอนต์ "Thai Sarabun"
                            .ShowText($"{"บ้านเลขที่ " + worker.HouseNo + " หมู่ที่ " + worker.VillageNo + (worker.Soi == "" ? "" : " ซอย " + worker.Soi) + (worker.Road == "" ? "" : " ถนน " + worker.Road)}")
                            .ShowText($"{" ตำบล" + worker.Subdistrict + " อำเภอ" + worker.District + " จังหวัด" + worker.Province}")
                            .EndText();
                        #endregion Page One
                        pdfDocument.Close(); // ปิด PdfDocument
                    }

                    // คัดลอกข้อมูลใน MemoryStream ไปยัง byte array
                    pdfBytes = memoryStream.ToArray();
                }

                // ส่งไฟล์ PDF ที่แก้ไขแล้วกลับไปยังผู้ใช้เพื่อดาวน์โหลด
                string fileName = "UpdatedDocument.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (FileNotFoundException ex)
            {
                // บันทึกข้อความข้อผิดพลาดเมื่อไม่พบไฟล์
                Console.WriteLine($"File Not Found Exception: {ex.Message}");
                return Json(new { success = false, message = "ไม่พบไฟล์ PDF ที่ระบุ" });
            }
            catch (iText.Kernel.Exceptions.PdfException ex)
            {
                // บันทึกข้อความข้อผิดพลาดที่เกี่ยวข้องกับ PDF
                Console.WriteLine($"PDF Exception: {ex.Message}");
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการจัดการ PDF", error = ex.Message });
            }
            catch (Exception ex)
            {
                // บันทึกข้อความข้อผิดพลาดทั่วไป
                Console.WriteLine($"General Exception: {ex.Message}");
                return Json(new { success = false, message = "เกิดข้อผิดพลาดทั่วไป", error = ex.Message });
            }
        }
        #endregion

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
