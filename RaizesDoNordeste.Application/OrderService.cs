using Microsoft.EntityFrameworkCore;
using RaizesDoNordeste.Domain;

namespace RaizesDoNordeste.Application
{
    public record OrderLine(Guid ProductId, int Quantity, bool UseReward);
    public record CreateOrder(Guid BranchId, OrderChannel Channel, ServiceType ServiceType, string? DeliveryAddress, List<OrderLine> Items);

    public class OrderService(IAppDbContext db, IPaymentGateway gateway)
    {
        public Task<Order> CreateAsync(Guid userId, CreateOrder input) => db.InTransactionAsync(async () =>
        {
            if (input.Items.Count == 0 || input.Items.Any(x => x.Quantity <= 0 || x.Quantity > 1000))
                throw new BusinessException(422, "VALIDACAO", "Informe itens com quantidade entre 1 e 1000.");
            if (input.ServiceType == ServiceType.Entrega && string.IsNullOrWhiteSpace(input.DeliveryAddress))
                throw new BusinessException(422, "VALIDACAO", "Endereço é obrigatório para entrega.");
            if (!await db.Branches.AnyAsync(x => x.Id == input.BranchId && x.Active))
                throw new BusinessException(404, "UNIDADE_NAO_ENCONTRADA", "Unidade não encontrada.");
            var ids = input.Items.Select(x => x.ProductId).Distinct().ToList();
            var products = await db.Products.Where(x => ids.Contains(x.Id) && x.Active).ToDictionaryAsync(x => x.Id);
            if (products.Count != ids.Count)
                throw new BusinessException(404, "PRODUTO_NAO_ENCONTRADO", "Produto não encontrado.");
            var order = new Order
            {
                CustomerId = userId,
                BranchId = input.BranchId,
                Channel = input.Channel,
                ServiceType = input.ServiceType,
                DeliveryAddress = input.ServiceType == ServiceType.Entrega ? input.DeliveryAddress?.Trim() : null
            };
            foreach (var line in input.Items)
            {
                var product = products[line.ProductId];
                if (line.UseReward && !product.LoyaltyEligible)
                    throw new BusinessException(409, "PRODUTO_NAO_ELEGIVEL", "Produto não participa da fidelidade.");
                order.Items.Add(new OrderItem
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = line.UseReward ? 0 : product.Price,
                    IsReward = line.UseReward
                });
            }
            await CheckStockAsync(order, deduct: false);
            var points = order.Items.Where(x => x.IsReward).Sum(x => x.Quantity) * OrderRules.PointsPerReward;
            var customer = await db.Users.SingleAsync(x => x.Id == userId && x.Active);
            if (points > 0 && (!customer.LoyaltyConsent || customer.LoyaltyPoints < points))
                throw new BusinessException(409, "FIDELIDADE_INSUFICIENTE", "Consentimento e 10 pontos por item gratuito são necessários.");
            customer.LoyaltyPoints -= points;
            order.Total = order.Items.Sum(x => x.UnitPrice * x.Quantity);
            db.Orders.Add(order);
            Audit(userId, "PEDIDO_CRIADO", order.Id);
            return order;
        });

        public async Task<Order> FindAsync(Guid id, Guid userId, bool staff)
        {
            var order = await db.Orders.Include(x => x.Items).Include(x => x.Payments).SingleOrDefaultAsync(x => x.Id == id)
                ?? throw new BusinessException(404, "PEDIDO_NAO_ENCONTRADO", "Pedido não encontrado.");
            if (!staff && order.CustomerId != userId)
                throw new BusinessException(403, "SEM_PERMISSAO", "Você não pode acessar este pedido.");
            return order;
        }

        public Task<Payment> PayAsync(Guid id, Guid userId, bool staff, string key, bool approve, bool simulateFailure) => db.InTransactionAsync(async () =>
        {
            // A propriedade do pedido é verificada antes de consultar a chave de idempotência.
            var order = await FindAsync(id, userId, staff);
            var existing = await db.Payments.SingleOrDefaultAsync(x => x.IdempotencyKey == key);
            if (existing is not null)
            {
                var originallyApproved = existing.Status is PaymentStatus.Aprovado or PaymentStatus.Estornado;
                if (existing.OrderId != id || originallyApproved != approve)
                    throw new BusinessException(409, "IDEMPOTENCIA_CONFLITO", "Chave já utilizada com outro pedido ou resultado.");
                return existing;
            }
            if (order.Status != OrderStatus.AguardandoPagamento)
                throw new BusinessException(409, "PAGAMENTO_NAO_PERMITIDO", "Pedido não está aguardando pagamento.");
            var result = await gateway.PayAsync(id, order.Total, approve, simulateFailure);
            if (result.Status == PaymentStatus.Aprovado)
            {
                await CheckStockAsync(order, deduct: true);
                order.Status = OrderStatus.Aceito;
            }
            var payment = new Payment
            {
                OrderId = id,
                IdempotencyKey = key,
                Amount = order.Total,
                Status = result.Status,
                ProviderReference = result.Reference,
                ProviderPayload = result.Payload
            };
            db.Payments.Add(payment);
            Audit(userId, result.Status == PaymentStatus.Aprovado ? "PAGAMENTO_APROVADO" : "PAGAMENTO_RECUSADO", id);
            return payment;
        });

        public Task<Order> ChangeStatusAsync(Guid id, Guid actorId, OrderStatus status) => db.InTransactionAsync(async () =>
        {
            var order = await FindAsync(id, actorId, true);
            if (!OrderRules.CanTransition(order.Status, status, order.ServiceType))
                throw new BusinessException(409, "TRANSICAO_INVALIDA", "Transição de status não permitida.");
            order.Status = status;
            if (status == OrderStatus.Finalizado)
            {
                var customer = await db.Users.SingleAsync(x => x.Id == order.CustomerId);
                if (customer.LoyaltyConsent) customer.LoyaltyPoints += OrderRules.EarnedPoints(order.Total);
            }
            Audit(actorId, $"PEDIDO_{status.ToString().ToUpperInvariant()}", id);
            return order;
        });

        public Task<Order> CancelAsync(Guid id, Guid userId, bool staff) => db.InTransactionAsync(async () =>
        {
            var order = await FindAsync(id, userId, staff);
            if (order.Status is not (OrderStatus.AguardandoPagamento or OrderStatus.Aceito))
                throw new BusinessException(409, "CANCELAMENTO_NAO_PERMITIDO", "Só é possível cancelar antes do preparo.");
            if (order.Status == OrderStatus.Aceito)
            {
                foreach (var group in order.Items.GroupBy(x => x.ProductId))
                {
                    var stock = await db.Stocks.SingleAsync(x => x.BranchId == order.BranchId && x.ProductId == group.Key);
                    var quantity = group.Sum(x => x.Quantity);
                    stock.Quantity += quantity;
                    db.StockMovements.Add(new StockMovement { StockId = stock.Id, Type = StockMovementType.Estorno, Quantity = quantity, Reason = $"Cancelamento {id}" });
                }
            }
            var customer = await db.Users.SingleAsync(x => x.Id == order.CustomerId);
            customer.LoyaltyPoints += order.Items.Where(x => x.IsReward).Sum(x => x.Quantity) * OrderRules.PointsPerReward;
            foreach (var payment in order.Payments.Where(x => x.Status == PaymentStatus.Aprovado)) payment.Status = PaymentStatus.Estornado;
            order.Status = OrderStatus.Cancelado;
            Audit(userId, "PEDIDO_CANCELADO", id);
            return order;
        });

        private async Task CheckStockAsync(Order order, bool deduct)
        {
            foreach (var group in order.Items.GroupBy(x => x.ProductId))
            {
                var quantity = group.Sum(x => x.Quantity);
                var stock = await db.Stocks.SingleOrDefaultAsync(x => x.BranchId == order.BranchId && x.ProductId == group.Key);
                if (stock is null || stock.Quantity < quantity)
                    throw new BusinessException(409, "ESTOQUE_INSUFICIENTE", "Não há estoque suficiente nesta unidade.");
                if (!deduct) continue;
                stock.Quantity -= quantity;
                db.StockMovements.Add(new StockMovement { StockId = stock.Id, Type = StockMovementType.Saida, Quantity = quantity, Reason = $"Pedido {order.Id}" });
            }
        }

        public void Audit(Guid userId, string action, Guid orderId) =>
            db.AuditLogs.Add(new AuditLog { UserId = userId, Action = action, Entity = "Pedido", EntityId = orderId });
    }
}
