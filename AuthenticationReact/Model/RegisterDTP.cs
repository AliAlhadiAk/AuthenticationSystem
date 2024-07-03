using System.ComponentModel.DataAnnotations;

namespace AuthenticationReact.Model
{
    public class RegisterDTO
    {
        [Required]
        public string user {  get; set; }
        [Required]
        [EmailAddress]
        public string email { get; set; }
        [Required]
        [MinLength(6)]
        public string pwd { get; set; }
    }
}
