using ContainerEvaluationSystem.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WorkPermitManager.Data;

namespace WorkPermitManager.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly Db_WorkPermitManagerModel _db;
        public UserController(Db_WorkPermitManagerModel db)
        {
            _db = db;
        }

        public IActionResult ProfileInfo()
        {
            ViewData["ManageSendEmail"] = _db.Users.Where(u => u.UserID == int.Parse(User.GetLoggedInUserID())).Select(u => u.ManageSendEmail).FirstOrDefault();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string NewPassword, string UserID)
        {
            if (string.IsNullOrEmpty(UserID) || string.IsNullOrEmpty(NewPassword))
            {
                return BadRequest("ข้อมูลไม่ครบถ้วน"); // ส่งสถานะ HTTP 400
            }

            // ค้นหาผู้ใช้จากเลขบัตรประจำตัว
            var user = _db.Users.FirstOrDefault(u => u.UserID == int.Parse(UserID));

            if (user == null)
            {
                return NotFound("ไม่พบผู้ใช้ที่ระบุ"); // ส่งสถานะ HTTP 404
            }

            // เปลี่ยนรหัสผ่าน
            user.Passwordhash = ComputeSha256Hash(NewPassword);

            _db.Users.Update(user); // อัปเดตผู้ใช้ในฐานข้อมูล
            await _db.SaveChangesAsync(); // บันทึกการเปลี่ยนแปลง

            return Ok("การเปลี่ยนรหัสผ่านสำเร็จ"); // ส่งสถานะ HTTP 200
        }


        [HttpGet]
        public JsonResult CheckCardID(string CardID, string UserID)
        {
            // ค้นหาผู้ใช้จากเลขบัตรประจำตัวและ UserID
            var user = _db.Users.FirstOrDefault(u => u.UserID == int.Parse(UserID) && u.CardID == CardID);

            // หากผู้ใช้ไม่ถูกต้อง, ส่ง false
            if (user == null)
            {
                return Json(false); // ผู้ใช้ที่ระบุไม่ถูกต้อง
            }

            // หากพบผู้ใช้ที่ตรงกัน, ส่ง true
            return Json(true); // ผู้ใช้ที่ระบุถูกต้อง
        }

        [HttpGet]
        public async Task<JsonResult> ManageSendEmail()
        {
            // ค้นหาผู้ใช้จาก EmpID
            var user = _db.Users.FirstOrDefault(u => u.UserID == int.Parse(User.GetLoggedInUserID()));

            if (user.Email == null || user.Email == "")
            {
                return Json(false); // ผู้ใช้ไม่ถูกต้อง
            }

            // เปลี่ยนสถานะ ManageSendEmail
            user.ManageSendEmail = !user.ManageSendEmail;
            _db.Users.Update(user);
            await _db.SaveChangesAsync(); // บันทึกการเปลี่ยนแปลง

            return Json(true); // การเปลี่ยนแปลงสำเร็จ
        }


        [HttpPost]
        public async Task<IActionResult> UpdateProfile(IFormFile ProfileImage, IFormFile Signature, string Email)
        {
            /// ตรวจสอบว่าอีเมลล์ถูกต้อง
            if (string.IsNullOrEmpty(Email) || !Email.Contains("@"))
            {
                return BadRequest("อีเมลล์ไม่ถูกต้อง");
            }

            // ค้นหาผู้ใช้ที่กำลังเข้าสู่ระบบ
            var user = _db.Users.FirstOrDefault(u => u.UserID == int.Parse(User.GetLoggedInUserID()));

            if (user == null)
            {
                return NotFound("ไม่พบผู้ใช้");
            }

            try
            {
                // อัปเดตอีเมลล์
                user.Email = Email;

                // จัดการไฟล์โปรไฟล์
                if (ProfileImage != null && ProfileImage.Length > 0)
                {
                    var profileImagePath = Path.Combine("wwwroot", "assets", "SystemImages", "ProfileImage", $"{user.UserID}.jpg");
                    using (var stream = new FileStream(profileImagePath, FileMode.Create))
                    {
                        await ProfileImage.CopyToAsync(stream);
                    }
                    user.ProfilePicture = Path.GetFileName(profileImagePath);
                }

                // จัดการลายเซ็นต์
                if (Signature != null && Signature.Length > 0)
                {
                    var signaturePath = Path.Combine("wwwroot", "assets", "SystemImages", "Signature", $"{user.UserID}.jpg");
                    using (var stream = new FileStream(signaturePath, FileMode.Create))
                    {
                        await Signature.CopyToAsync(stream);
                    }
                    user.Signature = Path.GetFileName(signaturePath);
                }

                await _db.SaveChangesAsync();

                var dataUser = _db.Users
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
                        s.Signature
                    })
                    .FirstOrDefault(s => s.UserID == int.Parse(User.GetLoggedInUserID()));

                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim("UserID", dataUser.UserID.ToString()),
                new Claim("Username", dataUser.Username.ToString()),
                new Claim(ClaimTypes.Name, dataUser.FullName),
                new Claim(ClaimTypes.Email, dataUser.Email  == null ? "NULL" :dataUser.Email),
                new Claim("Position", dataUser.PositionName),
                new Claim("Department", dataUser.DepartmentName),
                new Claim("Company", dataUser.CompanyName),
                new Claim("ProfileImage", dataUser.ProfilePicture  == null ? "NULL" :dataUser.ProfilePicture),
                new Claim("SignatureImage", dataUser.Signature  == null ? "NULL" :dataUser.Signature),
                new Claim("ViewAdministrator", _db.UserPermissions.Where(p=>p.FunctionName.Contains("Administrator")).Select(p => p.CanRead).FirstOrDefault() == true ? "True" : "Fasle")
            };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.NameIdentifier, ClaimsIdentity.DefaultRoleClaimType);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                // บันทึกการเปลี่ยนแปลงในฐานข้อมูล

                return Ok("อัปเดตโปรไฟล์สำเร็จ");
            }
            catch (Exception ex)
            {
                // จัดการข้อผิดพลาดและส่งสถานะ HTTP 500
                return StatusCode(500, $"ข้อผิดพลาดในการอัปเดตโปรไฟล์: {ex.Message}");
            }

        }

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
