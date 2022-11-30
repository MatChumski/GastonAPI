using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace GastonAPI.Controllers
{
    public class OtherFunctions
    {
        public static bool ValidateToken(ClaimsIdentity identity)
        {
            try
            {
                if (identity.Claims.Count() == 0)
                {
                    return false;
                }

                var perfil = identity.Claims.FirstOrDefault(x => x.Type == "Admin");

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static bool ValidateAdmin(ClaimsIdentity identity)
        {
            var claims = identity.Claims;

            foreach (var claim in claims)
            {
                if (claim.Type == "Role" && claim.Value.ToLower() != "admin")
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ValidateSelf(ClaimsIdentity identity, string id)
        {
            var claims = identity.Claims;

            foreach (var claim in claims)
            {
                if (claim.Type == "Id" && claim.Value != id.ToString())
                {
                    return false;
                }
            }

            return true;
        }
    }
}
