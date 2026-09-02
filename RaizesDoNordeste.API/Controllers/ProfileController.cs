using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaizesDoNordeste.Domain;
using RaizesDoNordeste.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;


namespace RaizesDoNordeste.API.Controllers
{
    [ApiController, Authorize]
    public class ProfileController(AppDbContext db) : ControllerBase
    {
        private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet("usuarios/me")]
        public async Task<IActionResult> Me()
        {
            var user = await db.Users.FindAsync(UserId);
            if (user is null) return NotFound();
            db.AuditLogs.Add(new AuditLog { UserId = UserId, Action = "PERFIL_CONSULTADO", Entity = "User", EntityId = UserId });
            await db.SaveChangesAsync();
            return Ok(new { user.Id, user.Name, user.Email, user.Role });
        }

        [HttpGet("fidelidade")]
        public async Task<IActionResult> Loyalty()
        {
            var user = await db.Users.SingleAsync(x => x.Id == UserId);
            return Ok(new { points = user.LoyaltyPoints, consent = user.LoyaltyConsent, updatedAt = user.LoyaltyConsentUpdatedAt });
        }

        [HttpPatch("fidelidade/consentimento")]
        public async Task<IActionResult> Consent(ConsentRequest input)
        {
            var user = await db.Users.SingleAsync(x => x.Id == UserId);
            user.LoyaltyConsent = input.Consent!.Value;
            user.LoyaltyConsentUpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(new AuditLog { UserId = UserId, Action = user.LoyaltyConsent ? "CONSENTIMENTO_CONCEDIDO" : "CONSENTIMENTO_REVOGADO", Entity = "User", EntityId = UserId });
            await db.SaveChangesAsync();
            return Ok(new { points = user.LoyaltyPoints, consent = user.LoyaltyConsent, updatedAt = user.LoyaltyConsentUpdatedAt });
        }

        [HttpGet("auditoria"), Authorize(Roles = "Gerente,Administrador")]
        public Task<List<AuditLog>> Audit([FromQuery] Guid? entityId, [Range(1, 100000)] int page = 1, [Range(1, 100)] int limit = 20) =>
            db.AuditLogs.AsNoTracking().Where(x => entityId == null || x.EntityId == entityId).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
                .Skip((page - 1) * limit).Take(limit).ToListAsync();
    }

}
