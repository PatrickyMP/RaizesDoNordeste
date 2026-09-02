using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using RaizesDoNordeste.Domain;
using RaizesDoNordeste.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace RaizesDoNordeste.API.Controllers
{
    [ApiController, Route("auth"), EnableRateLimiting("auth")]
    public class AuthController(AppDbContext db, IConfiguration config) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest input)
        {
            if (await db.Users.AnyAsync(x => x.Email == input.Email.Trim().ToLowerInvariant())) return Conflict(ApiError.Create(HttpContext, "EMAIL_EM_USO", "E-mail já cadastrado."));
            var user = new User { Name = input.Name.Trim(), Email = input.Email.Trim().ToLowerInvariant(), LoyaltyConsent = input.LoyaltyConsent, LoyaltyConsentUpdatedAt = DateTime.UtcNow };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, input.Password);
            db.Users.Add(user);
            db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "USUARIO_CADASTRADO", Entity = "User", EntityId = user.Id });
            await db.SaveChangesAsync();
            return Created("/usuarios/me", new { user.Id, user.Name, user.Email, user.Role });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest input)
        {
            var user = await db.Users.SingleOrDefaultAsync(x => x.Email == input.Email.Trim().ToLowerInvariant() && x.Active);
            if (user is null || new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, input.Password) == PasswordVerificationResult.Failed)
                return Unauthorized(ApiError.Create(HttpContext, "CREDENCIAIS_INVALIDAS", "E-mail ou senha inválidos."));
            var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Chave JWT ausente.");
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, user.Role.ToString()) };
            var token = new JwtSecurityToken(issuer: "RaizesDoNordeste", audience: "RaizesDoNordeste.Clients", claims: claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
            db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "LOGIN", Entity = "User", EntityId = user.Id });
            await db.SaveChangesAsync();
            return Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token), tokenType = "Bearer", expiresIn = 3600, user = new { user.Id, user.Name, perfil = user.Role.ToString() } });
        }
    }
}
