using System.ComponentModel.DataAnnotations;

namespace AuthenticationReact.Model
{
    public class LoginDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [MinLength(6)]
        public string Password { get; set; }
    }
}
