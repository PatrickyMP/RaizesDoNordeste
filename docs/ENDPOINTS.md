# Endpoints e contratos

Fonte: `openapi.json`, exportado do Swagger da API. Os exemplos são ilustrativos; obtenha IDs e tokens reais nas consultas. As listagens são arrays paginados. A coleção Postman executa o fluxo automaticamente.

Todos os endpoints de negócio exigem JWT, exceto cadastro e login. `/health` e `/swagger` são públicos. Envie `Authorization: Bearer TOKEN`. O token vale uma hora. A autorização por perfil se soma à restrição de propriedade dos pedidos.

## Erros

O envelope contém `error`, `message`, `details`, `timestamp`, `path` e `requestId`. HTTP 400 indica contrato inválido; 401 autenticação; 403 autorização; 404 ausência; 409 conflito de negócio/concorrência; 422 validação de domínio; 429 limite de auth; 500 erro interno; 503 falha simulada do pagamento. A lista geral do Swagger descreve a convenção; cada código só ocorre quando aplicável à rota.

## Rotas

### POST /auth/register



Rota pública de infraestrutura.

Requisição:

```json
{
  "name": "Cliente Exemplo",
  "email": "novo@example.com",
  "password": "Exemplo@123",
  "loyaltyConsent": true
}
```

Resposta de sucesso: **201**.

```json
{
  "id": "00000000-0000-0000-0000-000000000003",
  "name": "Cliente Exemplo",
  "email": "cliente@example.com",
  "role": "Cliente"
}
```

### POST /auth/login



Rota pública de infraestrutura.

Requisição:

```json
{
  "email": "cliente@raizes.local",
  "password": "Cliente@123"
}
```

Resposta de sucesso: **200**.

```json
{
  "accessToken": "TOKEN_ILUSTRATIVO_USE_O_LOGIN",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "user": {
    "id": "00000000-0000-0000-0000-000000000003",
    "name": "Cliente Exemplo",
    "perfil": "Cliente"
  }
}
```

### GET /unidades




Autenticação JWT. Qualquer perfil autenticado.

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| page | query | não | integer; padrão: 1 |
| limit | query | não | integer; padrão: 20 |

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **200**.

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000001",
    "name": "Centro",
    "address": "Endereço fictício",
    "active": true
  }
]
```

### POST /unidades




Autenticação JWT. Perfis: Administrador

Requisição:

```json
{
  "name": "Unidade Sul",
  "address": "Endereço fictício"
}
```

Resposta de sucesso: **201**.

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "name": "Centro",
  "address": "Endereço fictício",
  "active": true
}
```

### GET /unidades/{id}




Autenticação JWT. Qualquer perfil autenticado.

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| id | path | sim | string; padrão: - |

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **200**.

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "name": "Centro",
  "address": "Endereço fictício",
  "active": true
}
```

### PUT /unidades/{id}




Autenticação JWT. Perfis: Administrador

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| id | path | sim | string; padrão: - |

Requisição:

```json
{
  "name": "Unidade Sul",
  "address": "Endereço fictício"
}
```

Resposta de sucesso: **200**.

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "name": "Centro",
  "address": "Endereço fictício",
  "active": true
}
```

### DELETE /unidades/{id}




Autenticação JWT. Perfis: Administrador

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| id | path | sim | string; padrão: - |

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **204**.

Sem corpo. Desativação lógica, preservando histórico.

### GET /produtos




Autenticação JWT. Qualquer perfil autenticado.

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| page | query | não | integer; padrão: 1 |
| limit | query | não | integer; padrão: 20 |

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **200**.

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000002",
    "name": "Cuscuz",
    "description": "Porção demonstrativa",
    "price": 24.9,
    "loyaltyEligible": true,
    "active": true
  }
]
```

### POST /produtos




Autenticação JWT. Perfis: Gerente,Administrador

Requisição:

```json
{
  "name": "Tapioca",
  "description": "Tapioca de queijo",
  "price": 12.5,
  "loyaltyEligible": true
}
```

Resposta de sucesso: **201**.

```json
{
  "id": "00000000-0000-0000-0000-000000000002",
  "name": "Cuscuz",
  "description": "Porção demonstrativa",
  "price": 24.9,
  "loyaltyEligible": true,
  "active": true
}
```

### GET /produtos/{id}




Autenticação JWT. Qualquer perfil autenticado.

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| id | path | sim | string; padrão: - |

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **200**.

```json
{
  "id": "00000000-0000-0000-0000-000000000002",
  "name": "Cuscuz",
  "description": "Porção demonstrativa",
  "price": 24.9,
  "loyaltyEligible": true,
  "active": true
}
```

### PUT /produtos/{id}




Autenticação JWT. Perfis: Gerente,Administrador

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| id | path | sim | string; padrão: - |

Requisição:

```json
{
  "name": "Tapioca",
  "description": "Tapioca de queijo",
  "price": 12.5,
  "loyaltyEligible": true
}
```

Resposta de sucesso: **200**.

```json
{
  "id": "00000000-0000-0000-0000-000000000002",
  "name": "Cuscuz",
  "description": "Porção demonstrativa",
  "price": 24.9,
  "loyaltyEligible": true,
  "active": true
}
```

### DELETE /produtos/{id}




Autenticação JWT. Perfis: Gerente,Administrador

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| id | path | sim | string; padrão: - |

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **204**.

Sem corpo. Desativação lógica, preservando histórico.

### GET /unidades/{branchId}/cardapio




Autenticação JWT. Qualquer perfil autenticado.

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| branchId | path | sim | string; padrão: - |
| page | query | não | integer; padrão: 1 |
| limit | query | não | integer; padrão: 20 |

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **200**.

```json
[
  {
    "productId": "00000000-0000-0000-0000-000000000002",
    "name": "Cuscuz",
    "description": "Porção demonstrativa",
    "price": 24.9,
    "loyaltyEligible": true,
    "available": 50
  }
]
```

### GET /estoque/{branchId}




Autenticação JWT. Perfis: Atendente,Gerente,Administrador

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| branchId | path | sim | string; padrão: - |
| page | query | não | integer; padrão: 1 |
| limit | query | não | integer; padrão: 20 |

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **200**.

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000005",
    "branchId": "00000000-0000-0000-0000-000000000001",
    "productId": "00000000-0000-0000-0000-000000000002",
    "quantity": 50,
    "branch": null,
    "product": null
  }
]
```

### POST /estoque/{branchId}/entradas




Autenticação JWT. Perfis: Gerente,Administrador

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| branchId | path | sim | string; padrão: - |

Requisição:

```json
{
  "productId": "00000000-0000-0000-0000-000000000002",
  "quantity": 10,
  "reason": "Reposição demonstrativa"
}
```

Resposta de sucesso: **200**.

```json
{
  "id": "00000000-0000-0000-0000-000000000005",
  "branchId": "00000000-0000-0000-0000-000000000001",
  "productId": "00000000-0000-0000-0000-000000000002",
  "quantity": 50,
  "branch": null,
  "product": null
}
```

### POST /estoque/{branchId}/saidas




Autenticação JWT. Perfis: Gerente,Administrador

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| branchId | path | sim | string; padrão: - |

Requisição:

```json
{
  "productId": "00000000-0000-0000-0000-000000000002",
  "quantity": 10,
  "reason": "Reposição demonstrativa"
}
```

Resposta de sucesso: **200**.

```json
{
  "id": "00000000-0000-0000-0000-000000000005",
  "branchId": "00000000-0000-0000-0000-000000000001",
  "productId": "00000000-0000-0000-0000-000000000002",
  "quantity": 50,
  "branch": null,
  "product": null
}
```

### GET /estoque/{branchId}/movimentacoes




Autenticação JWT. Perfis: Gerente,Administrador

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| branchId | path | sim | string; padrão: - |
| page | query | não | integer; padrão: 1 |
| limit | query | não | integer; padrão: 20 |

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **200**.

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000007",
    "stockId": "00000000-0000-0000-0000-000000000005",
    "type": "Entrada",
    "quantity": 50,
    "reason": "Reposição demonstrativa",
    "createdAt": "2026-08-27T12:00:00Z"
  }
]
```

### POST /pedidos

Cria pedido com canal obrigatório, preços calculados e estoque validado.


Autenticação JWT. Perfis: Cliente,Atendente,Gerente,Administrador

Requisição:

```json
{
  "branchId": "00000000-0000-0000-0000-000000000001",
  "canalPedido": "TOTEM",
  "serviceType": "Retirada",
  "items": [
    {
      "productId": "00000000-0000-0000-0000-000000000002",
      "quantity": 1,
      "useReward": false
    }
  ]
}
```

Resposta de sucesso: **201**.

```json
{
  "id": "00000000-0000-0000-0000-000000000004",
  "customerId": "00000000-0000-0000-0000-000000000003",
  "branchId": "00000000-0000-0000-0000-000000000001",
  "canalPedido": "TOTEM",
  "serviceType": "Retirada",
  "status": "AguardandoPagamento",
  "total": 24.9,
  "deliveryAddress": null,
  "createdAt": "2026-08-27T12:00:00Z",
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000006",
      "orderId": "00000000-0000-0000-0000-000000000004",
      "productId": "00000000-0000-0000-0000-000000000002",
      "quantity": 1,
      "unitPrice": 24.9,
      "isReward": false,
      "product": null
    }
  ],
  "payments": []
}
```

### GET /pedidos

Consulta pedidos paginados; cliente só acessa os próprios pedidos.


Autenticação JWT. Qualquer perfil autenticado.

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| canalPedido | query | não | OrderChannel; padrão: - |
| status | query | não | OrderStatus; padrão: - |
| page | query | não | integer; padrão: 1 |
| limit | query | não | integer; padrão: 20 |

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **200**.

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000004",
    "customerId": "00000000-0000-0000-0000-000000000003",
    "branchId": "00000000-0000-0000-0000-000000000001",
    "canalPedido": "TOTEM",
    "serviceType": "Retirada",
    "status": "AguardandoPagamento",
    "total": 24.9,
    "deliveryAddress": null,
    "createdAt": "2026-08-27T12:00:00Z",
    "items": [
      {
        "id": "00000000-0000-0000-0000-000000000006",
        "orderId": "00000000-0000-0000-0000-000000000004",
        "productId": "00000000-0000-0000-0000-000000000002",
        "quantity": 1,
        "unitPrice": 24.9,
        "isReward": false,
        "product": null
      }
    ],
    "payments": []
  }
]
```

### GET /pedidos/{id}




Autenticação JWT. Qualquer perfil autenticado.

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| id | path | sim | string; padrão: - |

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **200**.

```json
{
  "id": "00000000-0000-0000-0000-000000000004",
  "customerId": "00000000-0000-0000-0000-000000000003",
  "branchId": "00000000-0000-0000-0000-000000000001",
  "canalPedido": "TOTEM",
  "serviceType": "Retirada",
  "status": "AguardandoPagamento",
  "total": 24.9,
  "deliveryAddress": null,
  "createdAt": "2026-08-27T12:00:00Z",
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000006",
      "orderId": "00000000-0000-0000-0000-000000000004",
      "productId": "00000000-0000-0000-0000-000000000002",
      "quantity": 1,
      "unitPrice": 24.9,
      "isReward": false,
      "product": null
    }
  ],
  "payments": []
}
```

### POST /pedidos/{id}/pagamentos

Simula pagamento. Aprovação aceita o pedido e baixa estoque atomicamente.


Autenticação JWT. Perfis: Cliente,Atendente,Gerente,Administrador

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| id | path | sim | string; padrão: - |

Requisição:

```json
{
  "idempotencyKey": "pedido-exemplo-001",
  "approve": true,
  "simulateFailure": false
}
```

Resposta de sucesso: **200**.

```json
{
  "id": "00000000-0000-0000-0000-000000000008",
  "orderId": "00000000-0000-0000-0000-000000000004",
  "idempotencyKey": "pedido-exemplo-001",
  "amount": 24.9,
  "status": "Aprovado",
  "providerReference": "mock-exemplo",
  "providerPayload": "{\"request\":{\"approve\":true},\"response\":{\"status\":\"Aprovado\"}}",
  "createdAt": "2026-08-27T12:00:00Z"
}
```

### PATCH /pedidos/{id}/status

Avança o fluxo: Aceito → EmPreparo → Pronto → Finalizado (ou EmRota na entrega).


Autenticação JWT. Perfis: Atendente,Cozinha,Entregador,Gerente,Administrador

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| id | path | sim | string; padrão: - |
| status | query | sim | OrderStatus; padrão: - |

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **200**.

```json
{
  "id": "00000000-0000-0000-0000-000000000004",
  "customerId": "00000000-0000-0000-0000-000000000003",
  "branchId": "00000000-0000-0000-0000-000000000001",
  "canalPedido": "TOTEM",
  "serviceType": "Retirada",
  "status": "EmPreparo",
  "total": 24.9,
  "deliveryAddress": null,
  "createdAt": "2026-08-27T12:00:00Z",
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000006",
      "orderId": "00000000-0000-0000-0000-000000000004",
      "productId": "00000000-0000-0000-0000-000000000002",
      "quantity": 1,
      "unitPrice": 24.9,
      "isReward": false,
      "product": null
    }
  ],
  "payments": []
}
```

### PATCH /pedidos/{id}/cancelar

Cancela antes do preparo, devolvendo estoque, pontos e estornando o pagamento mock.


Autenticação JWT. Perfis: Cliente,Atendente,Gerente,Administrador

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| id | path | sim | string; padrão: - |

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **200**.

```json
{
  "id": "00000000-0000-0000-0000-000000000004",
  "customerId": "00000000-0000-0000-0000-000000000003",
  "branchId": "00000000-0000-0000-0000-000000000001",
  "canalPedido": "TOTEM",
  "serviceType": "Retirada",
  "status": "Cancelado",
  "total": 24.9,
  "deliveryAddress": null,
  "createdAt": "2026-08-27T12:00:00Z",
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000006",
      "orderId": "00000000-0000-0000-0000-000000000004",
      "productId": "00000000-0000-0000-0000-000000000002",
      "quantity": 1,
      "unitPrice": 24.9,
      "isReward": false,
      "product": null
    }
  ],
  "payments": []
}
```

### GET /usuarios/me




Autenticação JWT. Qualquer perfil autenticado.

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **200**.

```json
{
  "id": "00000000-0000-0000-0000-000000000003",
  "name": "Cliente Exemplo",
  "email": "cliente@example.com",
  "role": "Cliente"
}
```

### GET /fidelidade




Autenticação JWT. Qualquer perfil autenticado.

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **200**.

```json
{
  "points": 10,
  "consent": true,
  "updatedAt": "2026-08-27T12:00:00Z"
}
```

### PATCH /fidelidade/consentimento




Autenticação JWT. Qualquer perfil autenticado.

Requisição:

```json
{
  "consent": true
}
```

Resposta de sucesso: **200**.

```json
{
  "points": 10,
  "consent": true,
  "updatedAt": "2026-08-27T12:00:00Z"
}
```

### GET /auditoria




Autenticação JWT. Perfis: Gerente,Administrador

| Parâmetro | Local | Obrigatório | Tipo / padrão |
|---|---|---|---|
| entityId | query | não | string; padrão: - |
| page | query | não | integer; padrão: 1 |
| limit | query | não | integer; padrão: 20 |

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **200**.

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000009",
    "userId": "00000000-0000-0000-0000-000000000003",
    "action": "PEDIDO_CRIADO",
    "entity": "Order",
    "entityId": "00000000-0000-0000-0000-000000000004",
    "createdAt": "2026-08-27T12:00:00Z"
  }
]
```

### GET /health



Rota pública de infraestrutura.

Sem corpo JSON; use os parâmetros da rota/query quando indicados.

Resposta de sucesso: **200**.

## Regras complementares de acesso

Cozinha pode marcar EmPreparo e Pronto; Atendente finaliza retirada/consumo local; Entregador marca EmRota e finaliza Entrega; Gerente e Administrador podem executar as transições válidas. Cliente consulta, paga e cancela apenas seus próprios pedidos. Gerir unidades exige Administrador; produtos e movimentações exigem Gerente/Administrador. Consulte o README para regras de resgate e cancelamento.

## Schemas dos corpos

### ConsentRequest

| Campo | Tipo | Obrigatório | Restrições |
|---|---|---|---|
| consent | boolean | sim | - |

### CreateBranchRequest

| Campo | Tipo | Obrigatório | Restrições |
|---|---|---|---|
| name | string | sim | minLength=0, maxLength=100 |
| address | string | sim | minLength=0, maxLength=300 |

### CreateOrderRequest

| Campo | Tipo | Obrigatório | Restrições |
|---|---|---|---|
| branchId | string | não | - |
| canalPedido | OrderChannel | sim | - |
| serviceType | ServiceType | sim | - |
| deliveryAddress | string | não | minLength=0, maxLength=300 |
| items | array | sim | minItems=1, maxItems=100 |

### CreateProductRequest

| Campo | Tipo | Obrigatório | Restrições |
|---|---|---|---|
| name | string | sim | minLength=0, maxLength=100 |
| description | string | sim | minLength=0, maxLength=500 |
| price | number | não | minimum=0.01, maximum=999999.99 |
| loyaltyEligible | boolean | não | - |

### LoginRequest

| Campo | Tipo | Obrigatório | Restrições |
|---|---|---|---|
| email | string | sim | minLength=1 |
| password | string | sim | minLength=0, maxLength=128 |

### OrderLineRequest

| Campo | Tipo | Obrigatório | Restrições |
|---|---|---|---|
| productId | string | não | - |
| quantity | integer | não | minimum=1, maximum=1000 |
| useReward | boolean | não | - |

### PaymentRequest

| Campo | Tipo | Obrigatório | Restrições |
|---|---|---|---|
| idempotencyKey | string | sim | minLength=8, maxLength=100 |
| approve | boolean | sim | - |
| simulateFailure | boolean | não | - |

### RegisterRequest

| Campo | Tipo | Obrigatório | Restrições |
|---|---|---|---|
| name | string | sim | minLength=2, maxLength=100 |
| email | string | sim | minLength=0, maxLength=254 |
| password | string | sim | minLength=8, maxLength=128 |
| loyaltyConsent | boolean | não | - |

### StockEntryRequest

| Campo | Tipo | Obrigatório | Restrições |
|---|---|---|---|
| productId | string | não | - |
| quantity | integer | não | minimum=1, maximum=1000000 |
| reason | string | sim | minLength=0, maxLength=200 |
