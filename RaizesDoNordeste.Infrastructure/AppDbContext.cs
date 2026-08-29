using Microsoft.EntityFrameworkCore;
using RaizesDoNordeste.Application;
using RaizesDoNordeste.Domain;
using System.Data;

namespace RaizesDoNordeste.Infrastructure
{
    public class AppDbContext(DbContextOptions options) : DbContext(options), IAppDbContext
    {
        public DbSet<User> Users => throw new NotImplementedException();

        public DbSet<Branch> Branches => throw new NotImplementedException();

        public DbSet<Product> Products => throw new NotImplementedException();

        public DbSet<Stock> Stocks => throw new NotImplementedException();

        public DbSet<StockMovement> StockMovements => throw new NotImplementedException();

        public DbSet<Order> Orders => throw new NotImplementedException();

        public DbSet<Payment> Payments => throw new NotImplementedException();

        public DbSet<AuditLog> AuditLogs => throw new NotImplementedException();

        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<User>().Property(x => x.Version).IsConcurrencyToken();
            model.Entity<Stock>().Property(x => x.Version).IsConcurrencyToken();
            model.Entity<Order>().Property(x => x.Version).IsConcurrencyToken();
            model.Entity<Order>().HasOne<User>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            model.Entity<Order>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            model.Entity<StockMovement>().HasOne<Stock>().WithMany().HasForeignKey(x => x.StockId).OnDelete(DeleteBehavior.Restrict);
            model.Entity<AuditLog>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            model.Entity<Stock>().ToTable(t => t.HasCheckConstraint("CK_Stock_Quantity", "\"Quantity\" >= 0"));
            model.Entity<User>().ToTable(t => t.HasCheckConstraint("CK_User_Points", "\"LoyaltyPoints\" >= 0"));
            model.Entity<User>().HasIndex(x => x.Email).IsUnique();
            model.Entity<Stock>().HasIndex(x => new { x.BranchId, x.ProductId }).IsUnique();
            model.Entity<Payment>().HasIndex(x => x.IdempotencyKey).IsUnique();
            model.Entity<Product>().Property(x => x.Price).HasPrecision(12, 2);
            model.Entity<Order>().Property(x => x.Total).HasPrecision(12, 2);
            model.Entity<OrderItem>().Property(x => x.UnitPrice).HasPrecision(12, 2);
            model.Entity<Payment>().Property(x => x.Amount).HasPrecision(12, 2);
            model.Entity<Order>().HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            model.Entity<Order>().HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries().Where(x => x.State == EntityState.Modified))
            {
                if (entry.Entity is User user) user.Version = Guid.NewGuid();
                if (entry.Entity is Stock stock) stock.Version = Guid.NewGuid();
                if (entry.Entity is Order order) order.Version = Guid.NewGuid();
            }
            return base.SaveChangesAsync(cancellationToken);
        }

        public async Task<T> InTransactionAsync<T>(Func<Task<T>> operation)
        {
            await using var transaction = await Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var result = await operation();
            await SaveChangesAsync();
            await transaction.CommitAsync();
            return result;
        }
    }
}
