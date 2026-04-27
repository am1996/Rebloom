using System.Threading.Tasks;
using Xunit;
using Rebloom.Controllers;
using Rebloom.Models;
using Rebloom.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System;

namespace Rebloom.Tests
{
    public class FakeEmailService : Rebloom.Services.IEmailService
    {
        public string? SentTo;
        public string? SentUsername;
        public string? SentToken;

        public Task SendVerificationEmailAsync(string toEmail, string username, string token)
        {
            SentTo = toEmail; SentUsername = username; SentToken = token; return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string toEmail, string username, string token)
        {
            SentTo = toEmail; SentUsername = username; SentToken = token; return Task.CompletedTask;
        }
    }

    public class FakeSmsService : Rebloom.Services.ISmsService
    {
        public string? LastTo;
        public string? LastMessage;
        public Task SendSmsAsync(string phoneNumber, string message)
        {
            LastTo = phoneNumber; LastMessage = message; return Task.CompletedTask;
        }
    }

    public class UserControllerTests
    {
        private static AppDbContext CreateContext(string dbName)
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            var ctx = new AppDbContext(opts);
            return ctx;
        }

        [Fact]
        public async Task ForgotPasswordEmail_SendsEmailAndSetsToken()
        {
            var ctx = CreateContext("fpemail");
            var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "h", IsVerified = true };
            ctx.Users.Add(user); await ctx.SaveChangesAsync();

            var email = new FakeEmailService();
            var sms = new FakeSmsService();
            var ctrl = new UserController(ctx, email, sms);

            var req = new ForgotPasswordEmailRequest { Email = "alice@example.com" };
            var res = await ctrl.ForgotPasswordEmail(req);

            Assert.IsType<OkObjectResult>(res.Result);
            var dbUser = ctx.Users.First(u => u.Email == "alice@example.com");
            Assert.False(string.IsNullOrWhiteSpace(dbUser.ResetToken));
            Assert.Equal(dbUser.ResetToken, email.SentToken);
            Assert.Equal("alice@example.com", email.SentTo);
        }

        [Fact]
        public async Task ForgotPasswordSms_SendsSmsAndSetsToken()
        {
            var ctx = CreateContext("fpsms");
            var user = new User { Username = "bob", PhoneNumber = "+15550001111", Email = "bob@example.com", PasswordHash = "h", IsVerified = true };
            ctx.Users.Add(user); await ctx.SaveChangesAsync();

            var email = new FakeEmailService();
            var sms = new FakeSmsService();
            var ctrl = new UserController(ctx, email, sms);

            var req = new ForgotPasswordSmsRequest { Phone = "+15550001111" };
            var res = await ctrl.ForgotPasswordSms(req);

            Assert.IsType<OkObjectResult>(res.Result);
            var dbUser = ctx.Users.First(u => u.PhoneNumber == "+15550001111");
            Assert.False(string.IsNullOrWhiteSpace(dbUser.ResetToken));
            Assert.Contains(dbUser.ResetToken, sms.LastMessage ?? string.Empty);
            Assert.Equal("+15550001111", sms.LastTo);
        }

        [Fact]
        public async Task ResetPassword_ValidToken_UpdatesPassword()
        {
            var ctx = CreateContext("resetpwd");
            var oldHash = BCrypt.Net.BCrypt.HashPassword("oldpass");
            var user = new User { Username = "carol", Email = "carol@example.com", PasswordHash = oldHash, ResetToken = "tok123", IsVerified = true };
            ctx.Users.Add(user); await ctx.SaveChangesAsync();

            var email = new FakeEmailService();
            var sms = new FakeSmsService();
            var ctrl = new UserController(ctx, email, sms);

            var req = new ResetPasswordRequest { Token = "tok123", NewPassword = "newpass" };
            var res = await ctrl.ResetPassword(req);

            Assert.IsType<OkObjectResult>(res.Result);
            var dbUser = ctx.Users.First(u => u.Username == "carol");
            Assert.True(BCrypt.Net.BCrypt.Verify("newpass", dbUser.PasswordHash));
            Assert.Null(dbUser.ResetToken);
        }
    }
}
