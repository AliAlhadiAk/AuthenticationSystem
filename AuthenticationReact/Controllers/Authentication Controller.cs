using AuthenticationReact.DbContext;
using AuthenticationReact.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Principal;

namespace AuthenticationReact.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;
        private readonly JwtAuthentication _jwt;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AuthenticationController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthenticationController(AppDbContext appDbContext, JwtAuthentication jwt, UserManager<User> userManager, ILogger<AuthenticationController> logger,
            RoleManager<IdentityRole> roleManager)
        {
            _appDbContext = appDbContext;
            _jwt = jwt;
            _userManager = userManager;
            _logger = logger;
            _roleManager = roleManager;
        }

        [HttpPost]
        [Route("seed-roles")]
        public async Task<IActionResult> SeedRoles()
        {
            var checkAdmin = await _roleManager.RoleExistsAsync(UserROLES.Admin);
            var checkUser = await _roleManager.RoleExistsAsync(UserROLES.User);

            if (checkAdmin && checkUser)
            {
                return Ok("Roles are already seeded");
            }

            await _roleManager.CreateAsync(new IdentityRole(UserROLES.Admin));
            await _roleManager.CreateAsync(new IdentityRole(UserROLES.User));
            return Ok("Roles have been seeded successfully");
        }
        [HttpGet]
        [Authorize (Roles ="ADMIN")]
        
        public IActionResult GetPrices()
        {
            return Ok("yahala bl 5al");
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Please fill in the credentials");
            }

            var checkEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (checkEmail == null)
            {
                return BadRequest("Please register before you login");
            }

            var checkPass = await _userManager.CheckPasswordAsync(checkEmail, dto.Password);
            var claims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.UniqueName, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, dto.Email),
                new Claim(JwtRegisteredClaimNames.Name, dto.Email)
            };

            var userRoles = await _userManager.GetRolesAsync(checkEmail);

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            if (checkPass)
            {
                var token = GenerateToken(claims);
                var refresh = GenerateRefreshToken();
                checkEmail.RefreshToken = refresh;
                checkEmail.RefreshTokenExpiry = DateTime.Now.AddDays(7);

                return Ok(new Response()
                {
                    Error = "Welcome back",
                    Result = true,
                    Token = token,
                    RefreshToken = refresh
                });
            }

            return Unauthorized(); // Return Unauthorized if the password is incorrect
        }

        [HttpPost]
        [Route("Sign-Up")]
        public async Task<IActionResult> SignUp(RegisterDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Please fill in the credentials");
            }

            var checkEmail = await _userManager.FindByEmailAsync(dto.email);
            if (checkEmail != null)
            {
                return BadRequest("Email already exists");
            }

            var account = new User
            {
                Email = dto.email,
                UserName = dto.user,
            };

            var checkPass = await _userManager.CreateAsync(account, dto.pwd);

            if (!checkPass.Succeeded)
            {
                var errors = string.Join(", ", checkPass.Errors.Select(e => e.Description));
                return BadRequest($"Failed to create user account: {errors}");
            }

            var claims = new List<Claim>()
    {
        new Claim(JwtRegisteredClaimNames.UniqueName, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Email, dto.email),
        new Claim(JwtRegisteredClaimNames.Name, dto.email)
    };

            if (dto.email == "alialhadiabokhalil@gmail.com")
            {
                var roleResult = await _userManager.AddToRoleAsync(account, UserROLES.Admin);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    return BadRequest($"Failed to assign admin role: {errors}");
                }
            }
            else
            {
                var roleResult = await _userManager.AddToRoleAsync(account, UserROLES.User);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    return BadRequest($"Failed to assign user role: {errors}");
                }
            }

            var userRoles = await _userManager.GetRolesAsync(account);

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = GenerateToken(claims);
            var refresh = GenerateRefreshToken();
            account.RefreshToken = refresh;
            account.RefreshTokenExpiry = DateTime.Now.AddDays(7);

            return Ok(new Response()
            {
                Error = "Welcome back",
                Result = true,
                Token = token,
                RefreshToken=refresh
            });
        }
        [HttpPost("/api/token/refresh")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
        {
            // Verify the refresh token
            var refreshToken = await _appDbContext.Users.FindAsync(request.RefreshToken);
            if (refreshToken == null || refreshToken.RefreshTokenExpiry < DateTime.UtcNow)
            {
                return BadRequest("Invalid or expired refresh token");
            }

            // Get the user associated with the refresh token
            /*var user = await _userManager.FindByIdAsync(refreshToken.UserName);
            if (user == null)
            {
                return BadRequest("Invalid refresh token");
            }*/
            var claims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.UniqueName, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, refreshToken.Email),
                new Claim(JwtRegisteredClaimNames.Name, refreshToken.Email)
            };

            var userRoles = await _userManager.GetRolesAsync(refreshToken);

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            // Generate a new access token and refresh token
            var accessToken = GenerateToken(claims);
            var newRefreshToken = GenerateRefreshToken();

            // Update the refresh token in the database
            refreshToken.RefreshToken = newRefreshToken;
            refreshToken.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7); // Set the new expiration date
            //_appDbContext.Users.Update(refreshToken);
            await _appDbContext.SaveChangesAsync();

            // Return the new access token and refresh token
            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken
            });
        }


        [HttpPost("/api/ForgotPassword")]
        public async Task<IActionResult> ForgotPass(string Email)
        {
            var checkEmail = await _userManager.FindByEmailAsync(Email);
            if (checkEmail == null)
            {
                return BadRequest("email doesn't exist");
            }
            var ResetToken = await _userManager.GeneratePasswordResetTokenAsync(checkEmail);
            
            await SendPasswordResetEmail(Email, "www.google.com");
             checkEmail.ResetToken = ResetToken;
             checkEmail.ResetTokenExpiry=DateTime.Now.AddDays(1);
             _appDbContext.SaveChanges();
            return Ok("You can now reset you password");
        }




        private string GenerateToken(List<Claim> claims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwt.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var tokenCreate = tokenHandler.CreateToken(tokenDescriptor);

            var token = tokenHandler.WriteToken(tokenCreate);
            return token.ToString();
        }
        private string GenerateRefreshToken()
        {
             return Guid.NewGuid().ToString();
        }


        private async Task SendPasswordResetEmail(string recipientEmail, string resetLink)
        {
            // Configure SMTP client
            using (var smtpClient = new SmtpClient("smtp.gmail.com"))
            {
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(EmailSender.Email,EmailSender.Pass);
                smtpClient.EnableSsl = true;
                smtpClient.Port = 587; // or the appropriate port for your SMTP server

                // Create the email message
                var message = new MailMessage();
                message.From = new MailAddress(EmailSender.Email);
                message.To.Add(recipientEmail);
                message.Subject = "Password Reset";
                message.Body = $"Click the link below to reset your password:<br/><a href='{resetLink}'>{resetLink}</a>";
                message.IsBodyHtml = true;

                // Send the email
                await smtpClient.SendMailAsync(message);
            }
        }
    }

}

