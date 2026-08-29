using System.Text.Json.Serialization;

namespace RaizesDoNordeste.Domain
{

    public enum UserRole
    {
        Cliente,
        Atendente,
        Cozinha,
        Entregador,
        Gerente,
        Administrador
    }
    public enum OrderChannel
    {
        APP,
        TOTEM,
        BALCAO,
        PICKUP,
        WEB
    }
    public enum OrderStatus
    {
        AguardandoPagamento,
        Aceito, EmPreparo,
        Pronto,
        EmRota,
        Finalizado,
        Cancelado
    }
    public enum ServiceType
    {
        Entrega,
        Retirada,
        ConsumoLocal
    }
 
    public enum PaymentStatus
    {
        Pendente,
        Aprovado,
        Recusado,
        Estornado
    }
    public enum StockMovementType
    {
        Entrada,
        Saida,
        Estorno
    }

    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        [JsonIgnore] public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Cliente;
        public bool LoyaltyConsent { get; set; }
        public DateTime? LoyaltyConsentUpdatedAt { get; set; }
        public int LoyaltyPoints { get; set; }
        public bool Active { get; set; } = true;
        [JsonIgnore] public Guid Version { get; set; } = Guid.NewGuid();
    }

    public class Branch
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool Active { get; set; } = true;
    }

    public class Product
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool LoyaltyEligible { get; set; } = true;
        public bool Active { get; set; } = true;
    }

    public class Stock
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BranchId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        [JsonIgnore] public Guid Version { get; set; } = Guid.NewGuid();
        public Branch? Branch { get; set; }
        public Product? Product { get; set; }
    }

    public class StockMovement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StockId { get; set; }
        public StockMovementType Type { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CustomerId { get; set; }
        public Guid BranchId { get; set; }
        [JsonPropertyName("canalPedido")] public OrderChannel Channel { get; set; }
        public ServiceType ServiceType { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.AguardandoPagamento;
        public decimal Total { get; set; }
        public string? DeliveryAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<OrderItem> Items { get; set; } = [];
        public List<Payment> Payments { get; set; } = [];
        [JsonIgnore] public Guid Version { get; set; } = Guid.NewGuid();
    }

    public class OrderItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public bool IsReward { get; set; }
        public Product? Product { get; set; }
    }

    public class Payment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pendente;
        public string ProviderReference { get; set; } = string.Empty;
        public string? ProviderPayload { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
