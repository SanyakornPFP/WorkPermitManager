using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WorkPermitManager.Data;
using WorkPermitManager.Helpers;
using WorkPermitManager.Models;

namespace WorkPermitManager.Controllers
{
    public class AuthenicationController : Controller
    {
        private readonly Db_WorkPermitManagerModel _db;

        public AuthenicationController(Db_WorkPermitManagerModel db)
        {
            _db = db;

        }

        public IActionResult Login()
        {
            return View();
        }

        public async Task<IActionResult> LoginResponse()
        {
            if (User.Identity!.IsAuthenticated)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("th-TH")),
                        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddHours(8) }
                    );

                return RedirectToAction("ListForm", "PowerOfAttorney");
            }
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        public async Task<IActionResult> LoginResponse(string Username, string Password, bool RememberMe)
        {
            try
            {
                var user = _db.Users
                    .Where(s => s.Username == Username && s.Passwordhash == ComputeSha256Hash(Password))
                    .Select(s => new
                    {
                        s.UserID,
                        s.Username,
                        s.FullName,
                        s.Email,
                        s.Position.PositionName,
                        s.Department.DepartmentName,
                        s.Company.CompanyName,
                        s.ProfilePicture,
                        s.Signature,
                        s.AdministratorActive
                    })
                    .FirstOrDefault();

                if (user == null)
                {
                    return Unauthorized(); // ส่งสถานะ HTTP 401 ถ้าผู้ใช้หรือรหัสผ่านไม่ถูกต้อง
                }

                var LoginDateNow = _db.Users.FirstOrDefault(s => s.UserID == user.UserID);
                LoginDateNow.LoginDate = DateTime.Now;
                _db.Users.Update(LoginDateNow);
                await _db.SaveChangesAsync();

                var props = new AuthenticationProperties
                {
                    IsPersistent = RememberMe,
                    AllowRefresh = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
                };

                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim("UserID", user.UserID.ToString()),
                new Claim("Username", user.Username.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email  == null ? "NULL" :user.Email),
                new Claim("Position", user.PositionName),
                new Claim("Department", user.DepartmentName),
                new Claim("Company", user.CompanyName),
                new Claim("ProfileImage", user.ProfilePicture  == null ? "NULL" :user.ProfilePicture),
                new Claim("SignatureImage", user.Signature  == null ? "NULL" :user.Signature),
                new Claim("ViewPowerOfAttorney", _db.UserPermissions.Where(p=>p.FunctionName.Contains("PowerOfAttorney")).Select(p => p.CanRead).FirstOrDefault() == true ? "True" : "False"),
                new Claim("ViewAdministrator", _db.UserPermissions.Where(p=>p.FunctionName.Contains("Administrator")).Select(p => p.CanRead).FirstOrDefault() == true ? "True" : "False"),
                new Claim("AdministratorIsActive", user.AdministratorActive == true ? "True" : "False"),
            };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.NameIdentifier, ClaimsIdentity.DefaultRoleClaimType);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

                _db.LoginHistories.Add(new LoginHistory
                {
                    UserID = user.UserID,
                    LoginTime = DateTime.Now,
                    IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                    DeviceInfo = Request.Headers["User-Agent"].ToString()
                });
                await _db.SaveChangesAsync();

                return Ok(); // ส่งสถานะ HTTP 200 หากเข้าสู่ระบบสำเร็จ
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}"); // บันทึกข้อผิดพลาด
                return StatusCode(500, "เกิดข้อผิดพลาดภายในเซิร์ฟเวอร์"); // ส่งสถานะ sHTTP 500 พร้อมข้อความที่เหมาะสม
            }
        }

        [HttpGet]
        public JsonResult CheckBeen(string Username, string Password)
        {
            //var model = _db.Users.Where(s => s.Username == Username && s.PasswordHash == Password).FirstOrDefault();
            var model = _db.Users.Where(s => s.Username == Username && s.Passwordhash == ComputeSha256Hash(Password)).FirstOrDefault();
            return Json(model);
        }

        public async Task<IActionResult> Logout()
        {
            var LogoutTime = _db.LoginHistories.Where(s => s.UserID == int.Parse(User.GetLoggedInUserID())).OrderByDescending(s => s.LoginTime).FirstOrDefault();
            LogoutTime.LogoutTime = DateTime.Now;
            _db.LoginHistories.Update(LogoutTime);
            await _db.SaveChangesAsync();

            // Clear the existing external cookie
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(Login), "Authenication");
        }

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