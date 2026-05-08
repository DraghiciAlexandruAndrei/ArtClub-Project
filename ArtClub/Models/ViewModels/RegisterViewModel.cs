using System.ComponentModel.DataAnnotations;

namespace ArtClub.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Prenumele este obligatoriu")]
        [Display(Name = "Prenume")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Numele este obligatoriu")]
        [Display(Name = "Nume")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email-ul este obligatoriu")]
        [EmailAddress(ErrorMessage = "Adresa de email nu este validă")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Parola este obligatorie")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Parola trebuie să aibă cel puțin 6 caractere")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmă parola")]
        [Compare("Password", ErrorMessage = "Parolele nu coincid")]
        public string ConfirmPassword { get; set; }
    }
}