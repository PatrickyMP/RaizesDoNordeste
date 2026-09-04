using System.Text.Json.Nodes;

namespace RaizesDoNordeste.API;

public static class ApiResponseExamples
{
    // Exemplos ilustrativos: use IDs e tokens obtidos nas consultas reais.
    public static JsonNode? For(string action)
    {
        const string product = """{"id":"00000000-0000-0000-0000-000000000002","name":"Cuscuz","description":"Porção demonstrativa","price":24.90,"loyaltyEligible":true,"active":true}""";
        const string branch = """{"id":"00000000-0000-0000-0000-000000000001","name":"Centro","address":"Endereço fictício","active":true}""";
        const string stock = """{"id":"00000000-0000-0000-0000-000000000005","branchId":"00000000-0000-0000-0000-000000000001","productId":"00000000-0000-0000-0000-000000000002","quantity":50,"branch":null,"product":null}""";
        const string order = """{"id":"00000000-0000-0000-0000-000000000004","customerId":"00000000-0000-0000-0000-000000000003","branchId":"00000000-0000-0000-0000-000000000001","canalPedido":"TOTEM","serviceType":"Retirada","status":"AguardandoPagamento","total":24.90,"deliveryAddress":null,"createdAt":"2026-08-27T12:00:00Z","items":[{"id":"00000000-0000-0000-0000-000000000006","orderId":"00000000-0000-0000-0000-000000000004","productId":"00000000-0000-0000-0000-000000000002","quantity":1,"unitPrice":24.90,"isReward":false,"product":null}],"payments":[]}""";
        var json = action switch
        {
            "Login" => """{"accessToken":"TOKEN_ILUSTRATIVO_USE_O_LOGIN","tokenType":"Bearer","expiresIn":3600,"user":{"id":"00000000-0000-0000-0000-000000000003","name":"Cliente Exemplo","perfil":"Cliente"}}""",
            "Register" or "Me" => """{"id":"00000000-0000-0000-0000-000000000003","name":"Cliente Exemplo","email":"cliente@example.com","role":"Cliente"}""",
            "Loyalty" or "Consent" => """{"points":10,"consent":true,"updatedAt":"2026-08-27T12:00:00Z"}""",
            "CreateBranch" or "UpdateBranch" or "GetBranch" => branch,
            "ListBranches" => $"[{branch}]",
            "CreateProduct" or "UpdateProduct" or "GetProduct" => product,
            "ListProducts" => $"[{product}]",
            "Entry" or "Exit" => stock,
            "GetStock" => $"[{stock}]",
            "Menu" => """[{"productId":"00000000-0000-0000-0000-000000000002","name":"Cuscuz","description":"Porção demonstrativa","price":24.90,"loyaltyEligible":true,"available":50}]""",
            "Movements" => """[{"id":"00000000-0000-0000-0000-000000000007","stockId":"00000000-0000-0000-0000-000000000005","type":"Entrada","quantity":50,"reason":"Reposição demonstrativa","createdAt":"2026-08-27T12:00:00Z"}]""",
            "Create" or "Get" => order,
            "ChangeStatus" => order.Replace("AguardandoPagamento", "EmPreparo"),
            "Cancel" => order.Replace("AguardandoPagamento", "Cancelado"),
            "List" => $"[{order}]",
            "Pay" => """{"id":"00000000-0000-0000-0000-000000000008","orderId":"00000000-0000-0000-0000-000000000004","idempotencyKey":"pedido-exemplo-001","amount":24.90,"status":"Aprovado","providerReference":"mock-exemplo","providerPayload":"{\"request\":{\"approve\":true},\"response\":{\"status\":\"Aprovado\"}}","createdAt":"2026-08-27T12:00:00Z"}""",
            "Audit" => """[{"id":"00000000-0000-0000-0000-000000000009","userId":"00000000-0000-0000-0000-000000000003","action":"PEDIDO_CRIADO","entity":"Order","entityId":"00000000-0000-0000-0000-000000000004","createdAt":"2026-08-27T12:00:00Z"}]""",
            _ => null
        };
        return json is null ? null : JsonNode.Parse(json);
    }
}
