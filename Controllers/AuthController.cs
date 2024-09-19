using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ssd_authorization_solution.Services; // For JwtTokenService
using ssd_authorization_solution.DTOs;

namespace ssd_authorization_solution.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly JwtTokenService _jwtTokenService;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            JwtTokenService jwtTokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
        {
            var user = await _userManager.FindByNameAsync(loginDto.Username);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.Count > 0 ? roles[0] : "User";

            var token = _jwtTokenService.GenerateToken(user.UserName, role);

            return Ok(new { token });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerDto)
        {
            // Create a new IdentityUser object
            var newUser = new IdentityUser
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email
            };

            // Create the user using UserManager
            var result = await _userManager.CreateAsync(newUser, registerDto.Password);
            if (!result.Succeeded)
            {
                // Return error if user creation failed
                return BadRequest(result.Errors);
            }

            // Assign the default role to the user
            await _userManager.AddToRoleAsync(newUser, "RegisteredUser");

            var roles = await _userManager.GetRolesAsync(newUser);
            var role = roles.Count > 0 ? roles[0] : "RegisteredUser";

            // Generate the JWT token
            var token = _jwtTokenService.GenerateToken(newUser.UserName, role);

            return Ok(new { token });
        }
    }
}
