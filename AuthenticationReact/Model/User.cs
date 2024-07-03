using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AuthenticationReact.Model
{
    public class User:IdentityUser
    {
        [Key]
        public int User_id {  get; set; }
        public string? RefreshToken {  get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    
    }
}
