using ArtClub.Models.Enums;

namespace ArtClub.Models.ViewModels
{
    public class EditUserRoleViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public UserRole CurrentRole { get; set; }
        public UserRole NewRole { get; set; }
    }
}
