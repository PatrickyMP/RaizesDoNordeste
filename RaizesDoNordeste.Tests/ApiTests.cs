using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaizesDoNordeste.Domain;
using RaizesDoNordeste.Infrastructure;

namespace RaizesDoNordeste.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"raizes-test-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", "chave-jwt-exclusiva-para-testes-com-mais-de-32-bytes");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={databasePath};Pooling=False");
        builder.UseSetting("Demo:Seed", "true");
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(databasePath)) File.Delete(databasePath);
    }
}

public sealed class ApiTests : IDisposable
{
    private readonly ApiFactory factory = new();
    private HttpClient Client => factory.CreateClient();
    public void Dispose() => factory.Dispose();

    private static async Task<JsonNode> Json(HttpResponseMessage response, HttpStatusCode status = HttpStatusCode.OK)
    {
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"Esperado {(int)status}, recebido {(int)response.StatusCode}: {content}");
        return JsonNode.Parse(content)!;
    }

    private async Task<HttpClient> Login(string user = "cliente", string password = "Cliente@123")
    {
        var client = Client;
        var body = await Json(await client.PostAsJsonAsync("/auth/login", new { email = $"{user}@raizes.local", password }));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body["accessToken"]!.GetValue<string>());
        return client;
    }

    private async Task<JsonObject> OrderBody(HttpClient client, int quantity = 1, bool reward = false)
    {
        var branch = (await Json(await client.GetAsync("/unidades")))[0]!["id"]!.GetValue<string>();
        var product = (await Json(await client.GetAsync("/produtos")))[0]!["id"]!.GetValue<string>();
        return new JsonObject { ["branchId"] = branch, ["canalPedido"] = "TOTEM", ["serviceType"] = "Retirada",
            ["items"] = new JsonArray(new JsonObject { ["productId"] = product, ["quantity"] = quantity, ["useReward"] = reward }) };
    }

    private async Task<string> Create(HttpClient client, int quantity = 1, bool reward = false) =>
        (await Json(await client.PostAsJsonAsync("/pedidos", await OrderBody(client, quantity, reward)), HttpStatusCode.Created))["id"]!.GetValue<string>();

    private static Task<HttpResponseMessage> Pay(HttpClient client, string id, string key, bool approve = true, bool simulateFailure = false) =>
        client.PostAsJsonAsync($"/pedidos/{id}/pagamentos", new { idempotencyKey = key, approve, simulateFailure });

    [Fact]
    public async Task Fluxo_completo_com_pagamento_estoque_fidelidade_auditoria_e_idempotencia()
    {
        var client = await Login();
        var id = await Create(client);
        var payment = await Json(await Pay(client, id, "fluxo-completo"));
        Assert.Equal("Aprovado", payment["status"]!.GetValue<string>());
        var repeat = await Json(await Pay(client, id, "fluxo-completo"));
        Assert.Equal(payment["id"]!.ToString(), repeat["id"]!.ToString());
        var order = await Json(await client.GetAsync($"/pedidos/{id}"));
        Assert.Equal("Aceito", order["status"]!.GetValue<string>());
        Assert.Equal("TOTEM", order["canalPedido"]!.GetValue<string>());
        Assert.Equal(24.90m, order["total"]!.GetValue<decimal>());
        var kitchen = await Login("cozinha", "Cozinha@123");
        await Json(await kitchen.PatchAsync($"/pedidos/{id}/status?status=EmPreparo", null));
        await Json(await kitchen.PatchAsync($"/pedidos/{id}/status?status=Pronto", null));
        var attendant = await Login("atendente", "Atendente@123");
        await Json(await attendant.PatchAsync($"/pedidos/{id}/status?status=Finalizado", null));
        var loyalty = await Json(await client.GetAsync("/fidelidade"));
        Assert.Equal(12, loyalty["points"]!.GetValue<int>());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(49, (await db.Stocks.SingleAsync()).Quantity);
        Assert.Equal(1, await db.Payments.CountAsync());
        Assert.True(await db.AuditLogs.AnyAsync(x => x.EntityId == Guid.Parse(id) && x.Action == "PEDIDO_FINALIZADO"));
        await Json(await attendant.PatchAsync($"/pedidos/{id}/status?status=Finalizado", null), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Pagamento_recusado_e_falha_nao_baixam_estoque_e_permitem_nova_tentativa()
    {
        var client = await Login();
        var id = await Create(client);
        await Json(await Pay(client, id, "falha-gateway", simulateFailure: true), HttpStatusCode.ServiceUnavailable);
        var refusal = await Json(await Pay(client, id, "recusado-001", false));
        Assert.Equal("Recusado", refusal["status"]!.GetValue<string>());
        Assert.Equal("AguardandoPagamento", (await Json(await client.GetAsync($"/pedidos/{id}")))["status"]!.GetValue<string>());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(50, (await db.Stocks.SingleAsync()).Quantity);
        await Json(await Pay(client, id, "falha-gateway"));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("invalid")]
    [InlineData("numeric")]
    [InlineData("quantity")]
    [InlineData("null-items")]
    [InlineData("empty-items")]
    [InlineData("null-item")]
    public async Task Contrato_invalido_retorna_erro_padrao(string scenario)
    {
        var client = await Login();
        var body = await OrderBody(client);
        if (scenario == "missing") body.Remove("canalPedido");
        if (scenario == "invalid") body["canalPedido"] = "INVALIDO";
        if (scenario == "numeric") body["canalPedido"] = 999;
        if (scenario == "quantity") body["items"]![0]!["quantity"] = -1;
        if (scenario == "null-items") body["items"] = null;
        if (scenario == "empty-items") body["items"] = new JsonArray();
        if (scenario == "null-item") body["items"] = new JsonArray((JsonNode?)null);
        var error = await Json(await client.PostAsJsonAsync("/pedidos", body), HttpStatusCode.BadRequest);
        foreach (var field in new[] { "error", "message", "details", "timestamp", "path", "requestId" }) Assert.NotNull(error[field]);
    }

    [Fact]
    public async Task Sem_token_e_perfil_incorreto_retornam_401_e_403()
    {
        Assert.Equal("NAO_AUTENTICADO", (await Json(await Client.GetAsync("/pedidos"), HttpStatusCode.Unauthorized))["error"]!.ToString());
        var client = await Login();
        await Json(await client.PostAsJsonAsync("/produtos", new { name = "Teste", description = "Teste", price = 1 }), HttpStatusCode.Forbidden);
        await Json(await client.GetAsync("/auditoria"), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unidade_inexistente_e_estoque_insuficiente()
    {
        var client = await Login();
        var body = await OrderBody(client);
        body["branchId"] = Guid.NewGuid().ToString();
        await Json(await client.PostAsJsonAsync("/pedidos", body), HttpStatusCode.NotFound);
        await Json(await client.PostAsJsonAsync("/pedidos", await OrderBody(client, 51)), HttpStatusCode.Conflict);
        var duplicate = await OrderBody(client, 30);
        duplicate["items"]!.AsArray().Add(duplicate["items"]![0]!.DeepClone());
        await Json(await client.PostAsJsonAsync("/pedidos", duplicate), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cancelamento_devolve_estoque_pontos_e_estorna_pagamento_uma_vez()
    {
        var client = await Login();
        var id = await Create(client, reward: true);
        await Json(await Pay(client, id, "resgate-cancelamento"));
        await Json(await client.PatchAsync($"/pedidos/{id}/cancelar", null));
        await Json(await client.PatchAsync($"/pedidos/{id}/cancelar", null), HttpStatusCode.Conflict);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(50, (await db.Stocks.SingleAsync()).Quantity);
        Assert.Equal(10, (await db.Users.SingleAsync(x => x.Email == "cliente@raizes.local")).LoyaltyPoints);
        Assert.Equal(PaymentStatus.Estornado, (await db.Payments.SingleAsync()).Status);
    }

    [Fact]
    public async Task Outro_cliente_nao_acessa_pedido_nem_pagamento_por_chave_conhecida()
    {
        var client = await Login();
        var id = await Create(client);
        await Json(await Pay(client, id, "chave-conhecida"));
        var other = Client;
        await Json(await other.PostAsJsonAsync("/auth/register", new { name = "Outro Cliente", email = "outro@raizes.local", password = "Outra@123", loyaltyConsent = false }), HttpStatusCode.Created);
        other = await Login("outro", "Outra@123");
        await Json(await other.GetAsync($"/pedidos/{id}"), HttpStatusCode.Forbidden);
        await Json(await Pay(other, id, "chave-conhecida"), HttpStatusCode.Forbidden);
        Assert.Empty((await Json(await other.GetAsync("/pedidos"))).AsArray());
        var ownId = await Create(other);
        await Json(await Pay(other, ownId, "chave-conhecida"), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Concorrencia_no_estoque_nao_permite_venda_acima_do_saldo()
    {
        var client = await Login();
        var first = await Create(client, 30);
        var second = await Create(client, 30);
        var results = await Task.WhenAll(Pay(client, first, "concorrencia-001"), Pay(client, second, "concorrencia-002"));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(20, (await db.Stocks.SingleAsync()).Quantity);
        Assert.Equal(1, await db.Payments.CountAsync());
    }

    [Fact]
    public async Task Filtro_canal_paginacao_e_swagger_funcionam()
    {
        var client = await Login();
        await Create(client);
        Assert.Single((await Json(await client.GetAsync("/pedidos?canalPedido=TOTEM&page=1&limit=10"))).AsArray());
        Assert.Empty((await Json(await client.GetAsync("/pedidos?canalPedido=WEB"))).AsArray());
        await Json(await client.GetAsync("/pedidos?canalPedido=999"), HttpStatusCode.BadRequest);
        await Json(await client.GetAsync("/produtos?page=0"), HttpStatusCode.BadRequest);
        var swagger = await Json(await Client.GetAsync("/swagger/v1/swagger.json"));
        Assert.NotNull(swagger["paths"]!["/pedidos/{id}/pagamentos"]);
        Assert.NotNull(swagger["components"]!["securitySchemes"]!["Bearer"]);
        Assert.NotNull(swagger["components"]!["schemas"]!["CreateOrderRequest"]!["properties"]!["canalPedido"]);
    }

    [Fact]
    public async Task Cadastro_de_produto_com_preco_decimal_e_consulta_por_location()
    {
        var admin = await Login("admin", "Admin@123");
        var response = await admin.PostAsJsonAsync("/produtos", new { name = "Tapioca", description = "Produto de teste", price = 0.50m });
        var product = await Json(response, HttpStatusCode.Created);
        Assert.Equal(0.50m, product["price"]!.GetValue<decimal>());
        Assert.NotNull(response.Headers.Location);
        await Json(await admin.GetAsync(response.Headers.Location));
    }

    [Fact]
    public async Task Consentimento_pode_ser_revogado_e_bloqueia_resgate()
    {
        var client = await Login();
        var consent = await Json(await client.PatchAsJsonAsync("/fidelidade/consentimento", new { consent = false }));
        Assert.False(consent["consent"]!.GetValue<bool>());
        Assert.NotNull(consent["updatedAt"]);
        await Json(await client.PostAsJsonAsync("/pedidos", await OrderBody(client, reward: true)), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Catalogo_editavel_sem_apagar_historico()
    {
        var admin = await Login("admin", "Admin@123");
        var body = await OrderBody(admin);
        var branch = body["branchId"]!.ToString();
        var product = body["items"]![0]!["productId"]!.ToString();
        var id = await Create(admin);
        await Json(await admin.PutAsJsonAsync($"/unidades/{branch}", new { name = "Centro atualizado", address = "Endereço fictício" }));
        await Json(await admin.PutAsJsonAsync($"/produtos/{product}", new { name = "Novo nome", description = "Descrição", price = 30.00m }));
        Assert.Equal(24.90m, (await Json(await admin.GetAsync($"/pedidos/{id}")))["total"]!.GetValue<decimal>());
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/produtos/{product}")).StatusCode);
        await Json(await admin.GetAsync($"/produtos/{product}"), HttpStatusCode.NotFound);
        await Json(await admin.PostAsJsonAsync("/pedidos", body), HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/unidades/{branch}")).StatusCode);
        await Json(await admin.GetAsync($"/unidades/{branch}"), HttpStatusCode.NotFound);
        await Json(await admin.GetAsync($"/pedidos/{id}"));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.Database.CanConnectAsync());
        Assert.Equal(4, await db.AuditLogs.CountAsync(x => x.Entity == "Product" || x.Entity == "Branch"));
    }

    [Fact]
    public async Task Saida_de_estoque_valida_saldo_registra_movimento_e_exige_perfil()
    {
        var admin = await Login("admin", "Admin@123");
        var body = await OrderBody(admin);
        var branch = body["branchId"]!.ToString();
        var product = body["items"]![0]!["productId"]!.ToString();
        var movement = new { productId = product, quantity = 3, reason = "Perda demonstrativa" };
        var client = await Login();
        await Json(await client.PostAsJsonAsync($"/estoque/{branch}/saidas", movement), HttpStatusCode.Forbidden);
        var stock = await Json(await admin.PostAsJsonAsync($"/estoque/{branch}/saidas", movement));
        Assert.Equal(47, stock["quantity"]!.GetValue<int>());
        await Json(await admin.PostAsJsonAsync($"/estoque/{branch}/saidas", new { productId = product, quantity = 48, reason = "Inválido" }), HttpStatusCode.Conflict);
        var history = await Json(await admin.GetAsync($"/estoque/{branch}/movimentacoes"));
        Assert.Contains(history.AsArray(), x => x!["type"]!.ToString() == "Saida" && x["quantity"]!.GetValue<int>() == 3);
        await Json(await admin.GetAsync($"/estoque/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
    }
}
