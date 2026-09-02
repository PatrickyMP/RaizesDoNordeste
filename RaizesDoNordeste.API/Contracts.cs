using RaizesDoNordeste.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaizesDoNordeste.API
{
    public sealed class RegisterRequest
    {
        [Required, StringLength(100, MinimumLength = 2)] public string Name { get; init; } = "";
        [Required, EmailAddress, StringLength(254)] public string Email { get; init; } = "";
        [Required, StringLength(128, MinimumLength = 8)] public string Password { get; init; } = "";
        public bool LoyaltyConsent { get; init; }
    }
    public sealed class LoginRequest
    {
        [Required, EmailAddress] public string Email { get; init; } = "";
        [Required, StringLength(128)] public string Password { get; init; } = "";
    }
    public sealed class CreateBranchRequest
    {
        [Required, StringLength(100)] public string Name { get; init; } = "";
        [Required, StringLength(300)] public string Address { get; init; } = "";
    }
    public sealed class CreateProductRequest
    {
        [Required, StringLength(100)] public string Name { get; init; } = "";
        [Required, StringLength(500)] public string Description { get; init; } = "";
        [Range(typeof(decimal), "0.01", "999999.99", ParseLimitsInInvariantCulture = true)] public decimal Price { get; init; }
        public bool LoyaltyEligible { get; init; } = true;
    }
    public sealed class StockEntryRequest
    {
        public Guid ProductId { get; init; }
        [Range(1, 1000000)] public int Quantity { get; init; }
        [Required, StringLength(200)] public string Reason { get; init; } = "";
    }
    public sealed class OrderLineRequest
    {
        public Guid ProductId { get; init; }
        [Range(1, 1000)] public int Quantity { get; init; }
        public bool UseReward { get; init; }
    }
    public sealed class CreateOrderRequest
    {
        public Guid BranchId { get; init; }
        [Required, EnumDataType(typeof(OrderChannel)), JsonPropertyName("canalPedido")]
        public OrderChannel? Channel { get; init; }
        [Required, EnumDataType(typeof(ServiceType))] public ServiceType? ServiceType { get; init; }
        [StringLength(300)] public string? DeliveryAddress { get; init; }
        [Required, MinLength(1), MaxLength(100)] public List<OrderLineRequest> Items { get; init; } = [];
    }
    public sealed class PaymentRequest
    {
        [Required, StringLength(100, MinimumLength = 8)] public string IdempotencyKey { get; init; } = "";
        [Required] public bool? Approve { get; init; }
        public bool SimulateFailure { get; init; }
    }
    public sealed class ConsentRequest
    {
        [Required] public bool? Consent { get; init; }
    }
}
