using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaizesDoNordeste.Domain;
using RaizesDoNordeste.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RaizesDoNordeste.Application;

namespace RaizesDoNordeste.API.Controllers
{
    [ApiController, Authorize, Route("")]
    public class CatalogController(AppDbContext db) : ControllerBase
    {
        [HttpGet("unidades")]
        public Task<List<Branch>> ListBranches([Range(1, 100000)] int page = 1, [Range(1, 100)] int limit = 20) =>
            db.Branches.AsNoTracking().Where(x => x.Active).OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * limit).Take(limit).ToListAsync();

        [HttpPost("unidades"), Authorize(Roles = "Administrador"), ProducesResponseType<Branch>(201)]
        public async Task<IActionResult> CreateBranch(CreateBranchRequest input)
        {
            var branch = new Branch { Name = input.Name.Trim(), Address = input.Address.Trim() };
            db.Branches.Add(branch);
            await db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBranch), new { id = branch.Id }, branch);
        }

        [HttpGet("unidades/{id:guid}")]
        public async Task<ActionResult<Branch>> GetBranch(Guid id) =>
            await db.Branches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.Active) is { } branch ? Ok(branch) : NotFound();

        [HttpPut("unidades/{id:guid}"), Authorize(Roles = "Administrador"), ProducesResponseType<Branch>(200)]
        public async Task<IActionResult> UpdateBranch(Guid id, CreateBranchRequest input)
        {
            var branch = await db.Branches.SingleOrDefaultAsync(x => x.Id == id && x.Active);
            if (branch is null) return NotFound();
            branch.Name = input.Name.Trim();
            branch.Address = input.Address.Trim();
            Audit("UNIDADE_ATUALIZADA", "Branch", id);
            await db.SaveChangesAsync();
            return Ok(branch);
        }

        [HttpDelete("unidades/{id:guid}"), Authorize(Roles = "Administrador"), ProducesResponseType(204)]
        public async Task<IActionResult> DeactivateBranch(Guid id)
        {
            var branch = await db.Branches.SingleOrDefaultAsync(x => x.Id == id && x.Active);
            if (branch is null) return NotFound();
            branch.Active = false;
            Audit("UNIDADE_DESATIVADA", "Branch", id);
            await db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("produtos")]
        public Task<List<Product>> ListProducts([Range(1, 100000)] int page = 1, [Range(1, 100)] int limit = 20) =>
            db.Products.AsNoTracking().Where(x => x.Active).OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * limit).Take(limit).ToListAsync();

        [HttpGet("produtos/{id:guid}")]
        public async Task<ActionResult<Product>> GetProduct(Guid id) => await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.Active) is { } p ? Ok(p) : NotFound();

        [HttpPost("produtos"), Authorize(Roles = "Gerente,Administrador"), ProducesResponseType<Product>(201)]
        public async Task<IActionResult> CreateProduct(CreateProductRequest input)
        {
            var product = new Product { Name = input.Name.Trim(), Description = input.Description.Trim(), Price = decimal.Round(input.Price, 2), LoyaltyEligible = input.LoyaltyEligible };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        [HttpPut("produtos/{id:guid}"), Authorize(Roles = "Gerente,Administrador"), ProducesResponseType<Product>(200)]
        public async Task<IActionResult> UpdateProduct(Guid id, CreateProductRequest input)
        {
            var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id && x.Active);
            if (product is null) return NotFound();
            product.Name = input.Name.Trim();
            product.Description = input.Description.Trim();
            product.Price = decimal.Round(input.Price, 2);
            product.LoyaltyEligible = input.LoyaltyEligible;
            Audit("PRODUTO_ATUALIZADO", "Product", id);
            await db.SaveChangesAsync();
            return Ok(product);
        }

        [HttpDelete("produtos/{id:guid}"), Authorize(Roles = "Gerente,Administrador"), ProducesResponseType(204)]
        public async Task<IActionResult> DeactivateProduct(Guid id)
        {
            var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id && x.Active);
            if (product is null) return NotFound();
            product.Active = false;
            Audit("PRODUTO_DESATIVADO", "Product", id);
            await db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("unidades/{branchId:guid}/cardapio")]
        public async Task<IActionResult> Menu(Guid branchId, [Range(1, 100000)] int page = 1, [Range(1, 100)] int limit = 20)
        {
            if (!await db.Branches.AnyAsync(x => x.Id == branchId && x.Active)) return NotFound();
            return Ok(await db.Stocks.AsNoTracking().Where(x => x.BranchId == branchId && x.Product!.Active && x.Quantity > 0)
                .OrderBy(x => x.Product!.Name).ThenBy(x => x.Id).Skip((page - 1) * limit).Take(limit)
                .Select(x => new { productId = x.ProductId, x.Product!.Name, x.Product.Description, x.Product.Price, x.Product.LoyaltyEligible, available = x.Quantity }).ToListAsync());
        }

        [HttpGet("estoque/{branchId:guid}"), Authorize(Roles = "Atendente,Gerente,Administrador")]
        [ProducesResponseType<List<Stock>>(200)]
        public async Task<IActionResult> GetStock(Guid branchId, [Range(1, 100000)] int page = 1, [Range(1, 100)] int limit = 20)
        {
            if (!await db.Branches.AnyAsync(x => x.Id == branchId && x.Active)) return NotFound();
            return Ok(await db.Stocks.AsNoTracking().Include(x => x.Product).Where(x => x.BranchId == branchId)
                .OrderBy(x => x.Id).Skip((page - 1) * limit).Take(limit).ToListAsync());
        }

        [HttpPost("estoque/{branchId:guid}/entradas"), Authorize(Roles = "Gerente,Administrador"), ProducesResponseType<Stock>(200)]
        public async Task<IActionResult> Entry(Guid branchId, StockEntryRequest input) => Ok(await db.InTransactionAsync(async () =>
        {
            if (!await db.Branches.AnyAsync(x => x.Id == branchId && x.Active) || !await db.Products.AnyAsync(x => x.Id == input.ProductId && x.Active))
                throw new BusinessException(404, "NAO_ENCONTRADO", "Unidade ou produto não encontrado.");
            var stock = await db.Stocks.SingleOrDefaultAsync(x => x.BranchId == branchId && x.ProductId == input.ProductId);
            if (stock is null) { stock = new Stock { BranchId = branchId, ProductId = input.ProductId }; db.Stocks.Add(stock); }
            if (stock.Quantity > int.MaxValue - input.Quantity) throw new BusinessException(409, "LIMITE_ESTOQUE", "Saldo máximo excedido.");
            stock.Quantity += input.Quantity;
            db.StockMovements.Add(new StockMovement { StockId = stock.Id, Type = StockMovementType.Entrada, Quantity = input.Quantity, Reason = input.Reason });
            db.AuditLogs.Add(new AuditLog { UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), Action = "ESTOQUE_ENTRADA", Entity = "Stock", EntityId = stock.Id });
            return stock;
        }));

        [HttpPost("estoque/{branchId:guid}/saidas"), Authorize(Roles = "Gerente,Administrador"), ProducesResponseType<Stock>(200)]
        public async Task<IActionResult> Exit(Guid branchId, StockEntryRequest input) => Ok(await db.InTransactionAsync(async () =>
        {
            if (!await db.Branches.AnyAsync(x => x.Id == branchId && x.Active))
                throw new BusinessException(404, "NAO_ENCONTRADO", "Unidade não encontrada.");
            var stock = await db.Stocks.SingleOrDefaultAsync(x => x.BranchId == branchId && x.ProductId == input.ProductId)
                ?? throw new BusinessException(404, "NAO_ENCONTRADO", "Estoque do produto não encontrado.");
            if (stock.Quantity < input.Quantity) throw new BusinessException(409, "ESTOQUE_INSUFICIENTE", "Saída superior ao saldo disponível.");
            stock.Quantity -= input.Quantity;
            db.StockMovements.Add(new StockMovement { StockId = stock.Id, Type = StockMovementType.Saida, Quantity = input.Quantity, Reason = input.Reason });
            Audit("ESTOQUE_SAIDA", "Stock", stock.Id);
            return stock;
        }));

        [HttpGet("estoque/{branchId:guid}/movimentacoes"), Authorize(Roles = "Gerente,Administrador"), ProducesResponseType<List<StockMovement>>(200)]
        public async Task<IActionResult> Movements(Guid branchId, [Range(1, 100000)] int page = 1, [Range(1, 100)] int limit = 20)
        {
            if (!await db.Branches.AnyAsync(x => x.Id == branchId && x.Active)) return NotFound();
            var stockIds = db.Stocks.Where(x => x.BranchId == branchId).Select(x => x.Id);
            return Ok(await db.StockMovements.AsNoTracking().Where(x => stockIds.Contains(x.StockId)).OrderByDescending(x => x.CreatedAt)
                .ThenBy(x => x.Id).Skip((page - 1) * limit).Take(limit).ToListAsync());
        }

        private void Audit(string action, string entity, Guid id) => db.AuditLogs.Add(new AuditLog
        {
            UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            Action = action,
            Entity = entity,
            EntityId = id
        });
    }
}
