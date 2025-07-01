using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Template.Api.Models;
using Template.Model.Entities;
using Template.Services.Interfaces;


namespace Template.Api.Controllers
{
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IUserServices _userService;

        public UsersController(
            ILogger<UsersController> logger,
            IConfiguration configuration,
            SignInManager<User> signInManager,
            UserManager<User> userManager, 
            RoleManager<Role> roleManager,
            IUserServices userService
            )
        {
            _logger = logger;
            _configuration = configuration;
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _userService = userService;
        }
        // <summary>
        /// Login a user and return a JWT token.
        /// </summary>
        /// <param name="model">Login model containing username and password.</param>

        [AllowAnonymous]
        [HttpPost("login")]

        public async Task<ActionResult<UserViewModel>> Login (LoginViewModel model)
        {
            try
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password,false, lockoutOnFailure: false);
          
                if (!result.Succeeded)
                {
                    return Unauthorized(new { message = "Invalid login attempt." });
                }

                var user = await _userManager.FindByEmailAsync(model.Email);

                if(user.IsActive == false)
                {
                    return BadRequest("User is not active in the database");
                }

                var roles = await _userManager.GetRolesAsync(user);
                user.Roles = roles;

                var token = GenerateJwtToken(user);
                token.Password = model.Password;
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while logging in the user.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [AllowAnonymous]
        [HttpPost("logout")]

        public async Task<IActionResult> Logout()
        {
                       try
            {
                await _signInManager.SignOutAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while logging out the user.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        private UserViewModel GenerateJwtToken(User user)
        {
            var secretKey = _configuration.GetValue<string>("SecretKey");
            var key = Encoding.ASCII.GetBytes(secretKey);

            var claims = new List<Claim>()
            {
                new(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            foreach (var role in user.Roles ?? [])
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var claim = new ClaimsIdentity(claims);
            var tokenDecriptor = new SecurityTokenDescriptor
            {
                Subject = claim,
                Expires = DateTime.UtcNow.AddDays(10),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var createdToken = tokenHandler.CreateToken(tokenDecriptor);

            var token = new UserViewModel()
            {
                Id = user.Id,
                Token = tokenHandler.WriteToken(createdToken),
                TokenExpirationDate = createdToken.ValidTo,
                UserName = user.UserName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = user.Roles ?? [],
            };
            return token;
        }

    }
}
