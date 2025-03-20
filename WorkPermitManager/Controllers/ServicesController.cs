using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("CreateServiceWorkers"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            ServiceWorker createModel = new ServiceWorker
            {
                ServiceID = model.ServiceID,
                PassportNumber = model.PassportNumber,
                Nationality = model.Nationality,
                Title = model.Title,
                FirstNameEN = model.FirstNameEN,
                FirstNameTH = model.FirstNameTH,
                LastNameEN = model.LastNameEN,
                LastNameTH = model.LastNameTH,
                ServiceItemID = model.ServiceItemID,
                ServiceFee = model.ServiceFee,
                Expiry90Days = model.Expiry90Days,
                Note = model.Note,
                DateOfBirth = model.DateOfBirth,
                PassportIssueDate = model.PassportIssueDate,
                PassportExpiryDate = model.PassportExpiryDate,
                WorkPermitNumber = model.WorkPermitNumber,
                EntryVisaNumber = model.EntryVisaNumber,
                PlaceOfBirth = model.PlaceOfBirth,
                PassportIssuedAt = model.PassportIssuedAt,
                Country = model.Country,
                CreatedAt = DateTime.Now,
                IsActive = true,
                UserManageID = int.Parse(User.GetLoggedInUserID())
            };

            _db.ServiceWorkers.Add(createModel);
            await _db.SaveChangesAsync();

            // Log the creation
            var logEntry = new LogSystemData
            {
                TableName = "ServiceWorkers",
                Action = "Create",
                RecordID = createModel.ServiceWorkerID,
                UserManageID = int.Parse(User.GetLoggedInUserID()),
                ActionTime = DateTime.Now,
                IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
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
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("DeleteServiceWorkers"))
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
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("UpdateServiceWorkers"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            var data = _db.ServiceWorkers.FirstOrDefault(p => p.ServiceWorkerID == model.ServiceWorkerID);
            if (data == null)
            {
                return NotFound();
            }

            var oldValues = new
            {
                data.PassportNumber,
                data.Nationality,
                data.FirstNameEN,
                data.LastNameEN,
                data.ServiceFee
            };

            data.PassportNumber = model.PassportNumber;
            data.Nationality = model.Nationality;
            data.Title = model.Title;
            data.FirstNameEN = model.FirstNameEN;
            data.FirstNameTH = model.FirstNameTH;
            data.LastNameEN = model.LastNameEN;
            data.LastNameTH = model.LastNameTH;
            data.ServiceItemID = model.ServiceItemID;
            data.ServiceFee = model.ServiceFee;
            data.Expiry90Days = model.Expiry90Days;
            data.Note = model.Note;
            data.DateOfBirth = model.DateOfBirth;
            data.PassportIssueDate = model.PassportIssueDate;
            data.PassportExpiryDate = model.PassportExpiryDate;
            data.WorkPermitNumber = model.WorkPermitNumber;
            data.EntryVisaNumber = model.EntryVisaNumber;
            data.PlaceOfBirth = model.PlaceOfBirth;
            data.PassportIssuedAt = model.PassportIssuedAt;
            data.Country = model.Country;
            data.UpdatedAt = DateTime.Now;
            data.UserManageID = int.Parse(User.GetLoggedInUserID());

            _db.ServiceWorkers.Update(data);
            await _db.SaveChangesAsync();

            // Log the update
            var logEntry = new LogSystemData
            {
                TableName = "ServiceWorkers",
                Action = "Update",
                RecordID = data.ServiceWorkerID,
                UserManageID = int.Parse(User.GetLoggedInUserID()),
                ActionTime = DateTime.Now,
                IPAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                OldValue = $"PassportNumber: {oldValues.PassportNumber}, Nationality: {oldValues.Nationality}, FirstNameEN: {oldValues.FirstNameEN}, LastNameEN: {oldValues.LastNameEN}, ServiceFee: {oldValues.ServiceFee}",
                NewValue = $"PassportNumber: {data.PassportNumber}, Nationality: {data.Nationality}, FirstNameEN: {data.FirstNameEN}, LastNameEN: {data.LastNameEN}, ServiceFee: {data.ServiceFee}",
                Description = $"Updated service worker with ID: {model.ServiceWorkerID}"
            };

            _db.LogSystemDatas.Add(logEntry);
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }
        #endregion

        #region Get ServiceWorker Details
        [HttpPost]
        public JsonResult GetServiceWorkerDetails(int ServiceWorkerID)
        {
            if (!GetUserPermissions(int.Parse(User.GetLoggedInUserID())).Contains("ReadServiceWorkers"))
            {
                return Json(new { success = false, message = "คุณไม่ได้รับอนุญาติในส่วนนี้ โปรดติดต่อผู้ดูแล" });
            }

            var model = _db.ServiceWorkers
                .Where(p => p.ServiceWorkerID == ServiceWorkerID)
                .Select(s => new
                {
                    s.ServiceWorkerID,
                    s.PassportNumber,
                    s.Nationality,
                    s.Title,
                    s.FirstNameEN,
                    s.FirstNameTH,
                    s.LastNameEN,
                    s.LastNameTH,
                    s.ServiceFee,
                    s.Expiry90Days,
                    s.Note,
                    s.DateOfBirth,
                    s.PassportIssueDate,
                    s.PassportExpiryDate,
                    s.WorkPermitNumber,
                    s.EntryVisaNumber,
                    s.PlaceOfBirth,
                    s.PassportIssuedAt,
                    s.Country,
                    s.CreatedAt,
                    s.UpdatedAt,
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
