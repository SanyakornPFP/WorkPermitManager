
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
        public static string GetLoggedInCompany(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("Company").Value.ToString();
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

        public static string GetLoggedPermission_Administrator(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("ViewAdministrator").Value.ToString();
        }

        public static string GetRole_AdministratorIsActive(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst("AdministratorIsActive").Value.ToString();
        }

    }
}
