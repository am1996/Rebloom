using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rebloom.Data;
using Rebloom.Models;
using Rebloom.Services;

namespace Rebloom.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;
        private readonly ISmsService _sms;

        public UserController(AppDbContext db, IEmailService email, ISmsService sms)
        {
            _db = db;
            _email = email;
            _sms = sms;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] AuthRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new AuthResponse { Success = false, Message = "Username, email and password are required." });

            var exists = await _db.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email);
            if (exists)
                return Conflict(new AuthResponse { Success = false, Message = "User with that username or email already exists." });

            var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var token = Guid.NewGuid().ToString();

            var user = new User
            {
                Username = request.Username!,
                Email = request.Email!,
                PasswordHash = hash,
                IsVerified = false,
                VerificationToken = token
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await _email.SendVerificationEmailAsync(user.Email, user.Username, token);

            return Ok(new AuthResponse { Success = true, Message = "Registered — verification email sent.", Token = null });
        }

        [HttpGet("verify")]
        public async Task<IActionResult> Verify([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return BadRequest(new { success = false, message = "Token required" });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);
            if (user == null) return NotFound(new { success = false, message = "Invalid token" });

            user.IsVerified = true;
            user.VerificationToken = null;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Account verified" });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] AuthRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new AuthResponse { Success = false, Message = "Username/email and password required." });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username);
            if (user == null) return Unauthorized(new AuthResponse { Success = false, Message = "Invalid credentials" });

            var ok = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!ok) return Unauthorized(new AuthResponse { Success = false, Message = "Invalid credentials" });

            if (!user.IsVerified) return StatusCode(403, new AuthResponse { Success = false, Message = "Email not verified" });

            var authToken = Guid.NewGuid().ToString();
            user.AuthToken = authToken;
            await _db.SaveChangesAsync();

            return Ok(new AuthResponse { Success = true, Message = "Login successful", Token = authToken });
        }

        [HttpPost("forgot-password")]
        public ActionResult<AuthResponse> ForgotPassword()
        {
            return BadRequest(new AuthResponse { Success = false, Message = "Use /forgot-password/email or /forgot-password/sms endpoints" });
        }

        [HttpPost("forgot-password/email")]
        public async Task<ActionResult<AuthResponse>> ForgotPasswordEmail([FromBody] ForgotPasswordEmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Email))
                return BadRequest(new AuthResponse { Success = false, Message = "Email required" });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return Ok(new AuthResponse { Success = true, Message = "If the account exists, a reset link was sent", Token = null });

            var reset = Guid.NewGuid().ToString();
            user.ResetToken = reset;
            await _db.SaveChangesAsync();

            await _email.SendPasswordResetAsync(user.Email, user.Username, reset);

            return Ok(new AuthResponse { Success = true, Message = "If the account exists, a reset link was sent", Token = null });
        }

        [HttpPost("forgot-password/sms")]
        public async Task<ActionResult<AuthResponse>> ForgotPasswordSms([FromBody] ForgotPasswordSmsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Phone))
                return BadRequest(new AuthResponse { Success = false, Message = "Phone required" });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.Phone);
            if (user == null)
                return Ok(new AuthResponse { Success = true, Message = "If the account exists, a reset code was sent", Token = null });

            var reset = Guid.NewGuid().ToString();
            user.ResetToken = reset;
            await _db.SaveChangesAsync();

            var smsMessage = $"Your password reset code: {reset}";
            await _sms.SendSmsAsync(user.PhoneNumber!, smsMessage);

            return Ok(new AuthResponse { Success = true, Message = "If the account exists, a reset code was sent", Token = null });
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<AuthResponse>> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest(new AuthResponse { Success = false, Message = "Token and new password required" });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.ResetToken == req.Token);
            if (user == null) return BadRequest(new AuthResponse { Success = false, Message = "Invalid token" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            user.ResetToken = null;
            await _db.SaveChangesAsync();

            return Ok(new AuthResponse { Success = true, Message = "Password reset successful" });
        }
    }
}
