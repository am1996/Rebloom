using Microsoft.AspNetCore.Mvc;
using Rebloom.Models;

namespace Rebloom.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        [HttpPost("register")]
        public ActionResult<AuthResponse> Register([FromBody] AuthRequest request)
        {
            return Ok(new AuthResponse { Success = true, Message = "User registered (demo)", Token = null });
        }

        [HttpPost("login")]
        public ActionResult<AuthResponse> Login([FromBody] AuthRequest request)
        {
            if (request.Username == "demo" && request.Password == "demo")
            {
                return Ok(new AuthResponse { Success = true, Message = "Login successful", Token = "demo-token" });
            }

            return Unauthorized(new AuthResponse { Success = false, Message = "Invalid credentials", Token = null });
        }

        [HttpPost("forgot-password")]
        public ActionResult<AuthResponse> ForgotPassword([FromBody] AuthRequest request)
        {
            return Ok(new AuthResponse { Success = true, Message = "If the account exists, a reset link was sent", Token = null });
        }
    }
}
