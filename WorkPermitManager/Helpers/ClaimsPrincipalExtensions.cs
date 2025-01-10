
using System.ComponentModel;
using System.Security.Claims;

namespace ContainerEvaluationSystem.Helpers
{
    public static class ClaimsPrincipalExtensions
    {
        private static byte[] Avatar { get; set; }
        public static T GetLoggedInUserId<T>(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            var loggedInUserId = principal.FindFirst(ClaimTypes.NameIdentifier).Value;

            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(loggedInUserId, typeof(T));
            }
            else if (typeof(T) == typeof(int) || typeof(T) == typeof(long))
            {
                return loggedInUserId != null ? (T)Convert.ChangeType(loggedInUserId, typeof(T)) : (T)Convert.ChangeType(0, typeof(T));
            }
            else if (typeof(T) == typeof(Guid))
            {
                return loggedInUserId != null ? (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(loggedInUserId) : (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(Guid.Empty);
            }
            else
            {
                throw new Exception("Invalid type provided");
            }
        }

        public static string GetLoggedInUserID(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("UserID").Value.ToString();
        }

        public static string GetLoggedInUsername(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("Username").Value.ToString();
        }

        public static string GetLoggedInName(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst(ClaimTypes.Name).Value.ToString();
        }

        public static string GetLoggedInUserEmail(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst(ClaimTypes.Email).Value.ToString();
        }
        public static string GetLoggedInPosition(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("Position").Value.ToString();
        }
        public static string GetLoggedInDepartment(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("Department").Value.ToString();
        }

        public static string GetLoggedEmpFirstName(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("EmpFirstName").Value.ToString();
        }
        public static string GetLoggedInImgProfile(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("ProfileImage").Value.ToString();
        }

        public static string GetLoggedInImgSignature(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("SignatureImage").Value.ToString();
        }

        public static bool GetLoggedInManagementUser(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            var claim = principal.FindFirst("ManagementUser");

            if (claim == null)
                return false; // หรือส่งกลับค่าเริ่มต้นเมื่อไม่มีข้อมูล

            bool.TryParse(claim.Value, out bool result);

            return result; // ส่งกลับค่า `bool` หาก `claim.Value` แปลงได้
        }

        public static string GetLoggedPermissionViewHazardAnalysisForm(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("ViewHazardAnalysisForm").Value.ToString();
        }

        public static string GetLoggedPermissionViewHazardAnalysisObject(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("ViewHazardAnalysisObject").Value.ToString();
        }

        public static string GetLoggedPermissionViewReferenceData(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("ViewReferenceData").Value.ToString();
        }

        public static string GetLoggedPermissionViewUsers(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("ViewUsers").Value.ToString();
        }

        public static string GetLoggedPermissionViewPermissions(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("ViewPermissions").Value.ToString();
        }

        public static string GetLoggedPermissionViewSetting(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("ViewSetting").Value.ToString();
        }

        public static string GetLoggedPermissionViewLogSystem(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("ViewLogSystem").Value.ToString();
        }

    }
}
