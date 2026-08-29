using Microsoft.EntityFrameworkCore;
using RaizesDoNordeste.Domain;

namespace RaizesDoNordeste.Application
{
    public interface IAppDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Branch> Branches { get; }
        DbSet<Product> Products { get; }
        DbSet<Stock> Stocks { get; }
        DbSet<StockMovement> StockMovements { get; }
        DbSet<Order> Orders { get; }
        DbSet<Payment> Payments { get; }
        DbSet<AuditLog> AuditLogs { get; }
        DbSet<OrderItem> OrderItems { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<T> InTransactionAsync<T>(Func<Task<T>> operation);
    }
    public record PaymentResult(PaymentStatus Status, string Reference, string Payload);
    public interface IPaymentGateway
    {
        Task<PaymentResult> PayAsync(Guid orderId, decimal amount, bool approve, bool simulateFailure);
    }

    public sealed class BusinessException(int statusCode, string code, string message) : Exception(message)
    {
        public int StatusCode { get; } = statusCode;
        public string Code { get; } = code;
    }
}
