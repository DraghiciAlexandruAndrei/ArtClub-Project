using Microsoft.AspNetCore.Authorization;
using ArtClub.Models.Enums;

namespace ArtClub.Attributes // Verifică să ai acest namespace în Controller
{
    public class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        public AuthorizeRoleAttribute(params UserRole[] roles)
        {
            // Transformă enum-urile în string-uri separate prin virgulă
            // Ex: UserRole.Member -> "Member"
            Roles = string.Join(",", roles.Select(r => r.ToString()));
        }
    }
}