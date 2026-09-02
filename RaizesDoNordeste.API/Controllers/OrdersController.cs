using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaizesDoNordeste.Application;
using RaizesDoNordeste.Domain;
using RaizesDoNordeste.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RaizesDoNordeste.API.Controllers
{
    [ApiController, Authorize, Route("pedidos")]
    public class OrdersController(AppDbContext db, OrderService service) : ControllerBase
    {
        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private bool Staff => !User.IsInRole("Cliente");

        /// <summary>Cria pedido com canal obrigatório, preços calculados e estoque validado.</summary>
        [HttpPost, Authorize(Roles = "Cliente,Atendente,Gerente,Administrador")]
        [ProducesResponseType<Order>(201)]
        public async Task<IActionResult> Create(CreateOrderRequest input)
        {
            if (input.Items.Any(x => x is null))
                return BadRequest(ApiError.Create(HttpContext, "VALIDACAO", "A lista de itens não pode conter valores nulos."));
            var order = await service.CreateAsync(CurrentUserId, new CreateOrder(input.BranchId, input.Channel!.Value,
                input.ServiceType!.Value, input.DeliveryAddress, input.Items.Select(x => new OrderLine(x.ProductId, x.Quantity, x.UseReward)).ToList()));
            return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
        }

        /// <summary>Consulta pedidos paginados; cliente só acessa os próprios pedidos.</summary>
        [HttpGet, ProducesResponseType<List<Order>>(200)]
        public async Task<IActionResult> List([FromQuery, EnumDataType(typeof(OrderChannel))] OrderChannel? canalPedido,
            [FromQuery, EnumDataType(typeof(OrderStatus))] OrderStatus? status,
            [FromQuery, Range(1, 100000)] int page = 1, [FromQuery, Range(1, 100)] int limit = 20)
        {
            var query = db.Orders.AsNoTracking().Include(x => x.Items).AsQueryable();
            if (!Staff) query = query.Where(x => x.CustomerId == CurrentUserId);
            if (canalPedido is not null) query = query.Where(x => x.Channel == canalPedido);
            if (status is not null) query = query.Where(x => x.Status == status);
            return Ok(await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * limit).Take(limit).ToListAsync());
        }

        [HttpGet("{id:guid}"), ProducesResponseType<Order>(200)]
        public async Task<IActionResult> Get(Guid id)
        {
            var order = await service.FindAsync(id, CurrentUserId, Staff);
            service.Audit(CurrentUserId, "PEDIDO_CONSULTADO", id);
            await db.SaveChangesAsync();
            return Ok(order);
        }

        /// <summary>Simula pagamento. Aprovação aceita o pedido e baixa estoque atomicamente.</summary>
        [HttpPost("{id:guid}/pagamentos"), Authorize(Roles = "Cliente,Atendente,Gerente,Administrador")]
        [ProducesResponseType<Payment>(200)]
        public async Task<IActionResult> Pay(Guid id, PaymentRequest input) =>
            Ok(await service.PayAsync(id, CurrentUserId, Staff, input.IdempotencyKey, input.Approve!.Value, input.SimulateFailure));

        /// <summary>Avança o fluxo: Aceito → EmPreparo → Pronto → Finalizado (ou EmRota na entrega).</summary>
        [HttpPatch("{id:guid}/status"), Authorize(Roles = "Atendente,Cozinha,Entregador,Gerente,Administrador")]
        [ProducesResponseType<Order>(200)]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromQuery, Required, EnumDataType(typeof(OrderStatus))] OrderStatus? status)
        {
            var manager = User.IsInRole("Gerente") || User.IsInRole("Administrador");
            var permitted = manager || (User.IsInRole("Cozinha") && status is OrderStatus.EmPreparo or OrderStatus.Pronto)
                || (User.IsInRole("Entregador") && status is OrderStatus.EmRota or OrderStatus.Finalizado)
                || (User.IsInRole("Atendente") && status == OrderStatus.Finalizado);
            if (!permitted) return Forbid();
            if (!manager && status == OrderStatus.Finalizado)
            {
                var order = await service.FindAsync(id, CurrentUserId, true);
                if (User.IsInRole("Atendente") && order.ServiceType == ServiceType.Entrega) return Forbid();
                if (User.IsInRole("Entregador") && order.ServiceType != ServiceType.Entrega) return Forbid();
            }
            return Ok(await service.ChangeStatusAsync(id, CurrentUserId, status!.Value));
        }

        /// <summary>Cancela antes do preparo, devolvendo estoque, pontos e estornando o pagamento mock.</summary>
        [HttpPatch("{id:guid}/cancelar"), Authorize(Roles = "Cliente,Atendente,Gerente,Administrador")]
        [ProducesResponseType<Order>(200)]
        public async Task<IActionResult> Cancel(Guid id) => Ok(await service.CancelAsync(id, CurrentUserId, Staff));
    }
}
