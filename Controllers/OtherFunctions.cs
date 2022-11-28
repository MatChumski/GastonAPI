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
    }
}
