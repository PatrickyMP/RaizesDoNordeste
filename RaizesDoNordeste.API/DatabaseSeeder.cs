using Microsoft.AspNetCore.Identity;
using RaizesDoNordeste.Domain;
using RaizesDoNordeste.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace RaizesDoNordeste.API
{
    public class DatabaseSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            if (await db.Users.AnyAsync()) return;
            var admin = new User { Name = "Administrador", Email = "admin@raizes.local", Role = UserRole.Administrador, LoyaltyConsent = true, LoyaltyConsentUpdatedAt = DateTime.UtcNow };
            admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, "Admin@123");
            var customer = new User { Name = "Cliente Exemplo", Email = "cliente@raizes.local", Role = UserRole.Cliente, LoyaltyConsent = true, LoyaltyConsentUpdatedAt = DateTime.UtcNow, LoyaltyPoints = 10 };
            customer.PasswordHash = new PasswordHasher<User>().HashPassword(customer, "Cliente@123");
            var attendant = new User { Name = "Atendente", Email = "atendente@raizes.local", Role = UserRole.Atendente };
            attendant.PasswordHash = new PasswordHasher<User>().HashPassword(attendant, "Atendente@123");
            var branch = new Branch { Name = "Unidade Centro", Address = "Rua do Centro, 100" };
            var product = new Product { Name = "Cuscuz Nordestino", Description = "Cuscuz com carne de sol", Price = 24.90m };
            var kitchen = new User { Name = "Cozinha", Email = "cozinha@raizes.local", Role = UserRole.Cozinha };
            kitchen.PasswordHash = new PasswordHasher<User>().HashPassword(kitchen, "Cozinha@123");
            var delivery = new User { Name = "Entregador", Email = "entregador@raizes.local", Role = UserRole.Entregador };
            delivery.PasswordHash = new PasswordHasher<User>().HashPassword(delivery, "Entregador@123");
            await using var transaction = await db.Database.BeginTransactionAsync();
            db.AddRange(admin, customer, attendant, kitchen, delivery, branch, product);
            db.Stocks.Add(new Stock { BranchId = branch.Id, ProductId = product.Id, Quantity = 50 });
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
    }
}
