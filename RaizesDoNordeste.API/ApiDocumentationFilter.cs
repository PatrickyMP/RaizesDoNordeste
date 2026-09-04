using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Nodes;

namespace RaizesDoNordeste.API;

public sealed class ApiDocumentationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authorization = context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>()
            .Concat(context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>() ?? []).ToArray();
        if (authorization.Length == 0) operation.Security = [];
        else
        {
            var roles = authorization.Select(x => x.Roles).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            operation.Description = (operation.Description ?? "") + "\nAutenticação JWT. " + (roles.Length == 0 ? "Qualquer perfil autenticado." : "Perfis: " + string.Join("; ", roles));
        }
        var exampleAction = context.MethodInfo.Name switch { "UpdateProduct" => "CreateProduct", "UpdateBranch" => "CreateBranch", "Exit" => "Entry", var name => name };
        var example = exampleAction switch
        {
            "Login" => "{\"email\":\"cliente@raizes.local\",\"password\":\"Cliente@123\"}",
            "Register" => "{\"name\":\"Cliente Exemplo\",\"email\":\"novo@example.com\",\"password\":\"Exemplo@123\",\"loyaltyConsent\":true}",
            "Pay" => "{\"idempotencyKey\":\"pedido-exemplo-001\",\"approve\":true,\"simulateFailure\":false}",
            "Consent" => "{\"consent\":true}",
            "CreateProduct" => "{\"name\":\"Tapioca\",\"description\":\"Tapioca de queijo\",\"price\":12.50,\"loyaltyEligible\":true}",
            "CreateBranch" => "{\"name\":\"Unidade Sul\",\"address\":\"Endereço fictício\"}",
            "Create" => "{\"branchId\":\"00000000-0000-0000-0000-000000000001\",\"canalPedido\":\"TOTEM\",\"serviceType\":\"Retirada\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000002\",\"quantity\":1,\"useReward\":false}]}",
            "Entry" => "{\"productId\":\"00000000-0000-0000-0000-000000000002\",\"quantity\":10,\"reason\":\"Reposição demonstrativa\"}",
            _ => null
        };
        if (example is not null && operation.RequestBody?.Content is { } content)
            foreach (var media in content.Values) media.Example = JsonNode.Parse(example);
        operation.Responses ??= new OpenApiResponses();
        if (context.MethodInfo.Name == "Register")
        {
            operation.Responses.Remove("200");
            operation.Responses.TryAdd("201", new OpenApiResponse { Description = "Cliente cadastrado" });
        }
        var responseExample = ApiResponseExamples.For(context.MethodInfo.Name);
        foreach (var response in operation.Responses.Where(x => x.Key.StartsWith('2') && x.Key != "204"))
        {
            if (response.Value is not OpenApiResponse concrete) continue;
            concrete.Content ??= new Dictionary<string, OpenApiMediaType>();
            if (!concrete.Content.ContainsKey("application/json")) concrete.Content["application/json"] = new OpenApiMediaType();
            concrete.Content["application/json"].Example = responseExample?.DeepClone();
        }
        foreach (var (code, description) in new[] { (400, "Validação de contrato"), (401, "Token ausente ou inválido"),
            (403, "Perfil sem permissão"), (404, "Recurso inexistente"), (409, "Conflito de negócio ou concorrência"),
            (422, "Regra de validação"), (429, "Limite de requisições"), (500, "Falha interna"), (503, "Gateway simulado indisponível") })
            operation.Responses.TryAdd(code.ToString(), new OpenApiResponse
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new()
                    { Schema = context.SchemaGenerator.GenerateSchema(typeof(ApiError), context.SchemaRepository),
                      Example = JsonNode.Parse("""{"error":"CODIGO_DO_ERRO","message":"Descrição legível do problema.","details":[],"timestamp":"2026-08-27T12:00:00Z","path":"/rota","requestId":"identificador-da-requisicao"}""") } }
            });
    }
}
