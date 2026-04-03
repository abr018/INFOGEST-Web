using System.ComponentModel.DataAnnotations;

namespace INFOGEST_Web.Models
{
    public class LoginRquest
    {
        [Required(ErrorMessage = "O nome de utilizador é obrigatório")]
        public string Username { get; set; }

        [Required(ErrorMessage = "A password é obrigatória")]
        public string Password { get; set; }
    }
}