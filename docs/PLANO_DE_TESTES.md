# Plano de testes da coleção Postman

Execute a coleção inteira, na ordem. A preparação cria dados isolados e captura tokens/IDs; a API precisa do seed de demonstração. Entradas completas estão no JSON da coleção. A cada erro, validam-se também os seis campos do envelope padronizado.

| ID | Cenário | Método e rota | Esperado | Evidência |
|---|---|---|---|---|
| T01 | Login administrador | `POST /auth/login` | 200; Token recebido | 01 Preparação/T01 - Login administrador |
| T02 | Criar unidade exclusiva da execução | `POST /unidades` | 201; resposta JSON | 01 Preparação/T02 - Criar unidade exclusiva da execução |
| T03 | Criar produto exclusivo | `POST /produtos` | 201; resposta JSON | 01 Preparação/T03 - Criar produto exclusivo |
| T04 | Entrada de estoque | `POST /estoque/{{branchId}}/entradas` | 200; Saldo inicial | 01 Preparação/T04 - Entrada de estoque |
| T05 | Login cliente | `POST /auth/login` | 200; resposta JSON | 02 Cliente e pedido/T05 - Login cliente |
| T06 | Consultar cardápio por unidade | `GET /unidades/{{branchId}}/cardapio` | 200; Produto disponível | 02 Cliente e pedido/T06 - Consultar cardápio por unidade |
| T07 | Criar pedido multicanal | `POST /pedidos` | 201; Canal e total | 02 Cliente e pedido/T07 - Criar pedido multicanal |
| T08 | Pagamento recusado | `POST /pedidos/{{orderId}}/pagamentos` | 200; Recusado | 03 Pagamento/T08 - Pagamento recusado |
| T09 | Pedido aguarda nova tentativa | `GET /pedidos/{{orderId}}` | 200; Status preservado | 03 Pagamento/T09 - Pedido aguarda nova tentativa |
| T10 | Falha simulada do gateway | `POST /pedidos/{{orderId}}/pagamentos` | 503; resposta JSON | 03 Pagamento/T10 - Falha simulada do gateway |
| T11 | Pagamento aprovado | `POST /pedidos/{{orderId}}/pagamentos` | 200; Aprovado e payload | 03 Pagamento/T11 - Pagamento aprovado |
| T12 | Repetir mesma chave sem cobrar novamente | `POST /pedidos/{{orderId}}/pagamentos` | 200; Mesmo pagamento | 03 Pagamento/T12 - Repetir mesma chave sem cobrar novamente |
| T13 | Confirmar pedido aceito | `GET /pedidos/{{orderId}}` | 200; Aceito | 03 Pagamento/T13 - Confirmar pedido aceito |
| T14 | Cliente não pode preparar pedido | `PATCH /pedidos/{{orderId}}/status?status=EmPreparo` | 403; resposta JSON | 04 Status e auditoria/T14 - Cliente não pode preparar pedido |
| T15 | Preparar | `PATCH /pedidos/{{orderId}}/status?status=EmPreparo` | 200; Em preparo | 04 Status e auditoria/T15 - Preparar |
| T16 | Pronto | `PATCH /pedidos/{{orderId}}/status?status=Pronto` | 200; Pronto | 04 Status e auditoria/T16 - Pronto |
| T17 | Finalizar retirada | `PATCH /pedidos/{{orderId}}/status?status=Finalizado` | 200; Finalizado | 04 Status e auditoria/T17 - Finalizar retirada |
| T18 | Consultar auditoria do pedido | `GET /auditoria?entityId={{orderId}}` | 200; Criação e finalização auditadas | 04 Status e auditoria/T18 - Consultar auditoria do pedido |
| T19 | Filtrar canal | `GET /pedidos?canalPedido=TOTEM&page=1&limit=100` | 200; Apenas TOTEM | 04 Status e auditoria/T19 - Filtrar canal |
| T20 | Conferir baixa única no estoque | `GET /estoque/{{branchId}}` | 200; Saldo 19 | 04 Status e auditoria/T20 - Conferir baixa única no estoque |
| T21 | Sem token | `GET /pedidos` | 401; resposta JSON | 05 Erros/T21 - Sem token |
| T22 | Login inválido | `POST /auth/login` | 401; resposta JSON | 05 Erros/T22 - Login inválido |
| T23 | Canal ausente | `POST /pedidos` | 400; resposta JSON | 05 Erros/T23 - Canal ausente |
| T24 | Canal inválido | `POST /pedidos` | 400; resposta JSON | 05 Erros/T24 - Canal inválido |
| T25 | Quantidade negativa | `POST /pedidos` | 400; resposta JSON | 05 Erros/T25 - Quantidade negativa |
| T26 | Unidade inexistente | `POST /pedidos` | 404; resposta JSON | 05 Erros/T26 - Unidade inexistente |
| T27 | Produto inexistente | `POST /pedidos` | 404; resposta JSON | 05 Erros/T27 - Produto inexistente |
| T28 | Estoque insuficiente | `POST /pedidos` | 409; resposta JSON | 05 Erros/T28 - Estoque insuficiente |
| T29 | Transição inválida | `PATCH /pedidos/{{orderId}}/status?status=EmPreparo` | 409; resposta JSON | 05 Erros/T29 - Transição inválida |
| T30 | E-mail inválido | `POST /auth/register` | 400; resposta JSON | 05 Erros/T30 - E-mail inválido |
| T31 | Criar pedido para cancelamento | `POST /pedidos` | 201; resposta JSON | 06 Cancelamento/T31 - Criar pedido para cancelamento |
| T32 | Pagar pedido cancelável | `POST /pedidos/{{cancelOrderId}}/pagamentos` | 200; resposta JSON | 06 Cancelamento/T32 - Pagar pedido cancelável |
| T33 | Cancelar e estornar | `PATCH /pedidos/{{cancelOrderId}}/cancelar` | 200; Cancelado e estornado | 06 Cancelamento/T33 - Cancelar e estornar |
| T34 | Conferir devolução de estoque | `GET /estoque/{{branchId}}` | 200; Saldo restaurado a 19 | 06 Cancelamento/T34 - Conferir devolução de estoque |
| T35 | Editar unidade | `PUT /unidades/{{branchId}}` | 200; resposta JSON | 07 Gestão/T35 - Editar unidade |
| T36 | Editar produto | `PUT /produtos/{{productId}}` | 200; resposta JSON | 07 Gestão/T36 - Editar produto |
| T37 | Saída manual por perda | `POST /estoque/{{branchId}}/saidas` | 200; Saldo 17 | 07 Gestão/T37 - Saída manual por perda |
| T38 | Saída acima do saldo | `POST /estoque/{{branchId}}/saidas` | 409; resposta JSON | 07 Gestão/T38 - Saída acima do saldo |
| T39 | Histórico de estoque | `GET /estoque/{{branchId}}/movimentacoes` | 200; Entrada, saída e estorno | 07 Gestão/T39 - Histórico de estoque |
| T40 | Desativar produto | `DELETE /produtos/{{productId}}` | 204; resposta JSON | 07 Gestão/T40 - Desativar produto |
| T41 | Produto desativado indisponível | `GET /produtos/{{productId}}` | 404; resposta JSON | 07 Gestão/T41 - Produto desativado indisponível |
| T42 | Desativar unidade | `DELETE /unidades/{{branchId}}` | 204; resposta JSON | 07 Gestão/T42 - Desativar unidade |
| T43 | Unidade desativada indisponível | `GET /unidades/{{branchId}}` | 404; resposta JSON | 07 Gestão/T43 - Unidade desativada indisponível |
| T44 | Histórico do pedido preservado | `GET /pedidos/{{orderId}}` | 200; Preço original | 07 Gestão/T44 - Histórico do pedido preservado |

## Pré-condições e entradas

T01: API rodando com banco inicializado e Demo__Seed=true. T02–T44: executar os anteriores, sem alterar as variáveis capturadas. Bodies, headers, parâmetros e asserções são parte executável da coleção, não exemplos soltos.

## Testes automatizados adicionais

`dotnet test` cobre regras de domínio, acesso de outro cliente, revogação do consentimento, resgate/cancelamento, idempotência, validação dos contratos, esquema Swagger e duas compras simultâneas disputando o mesmo estoque. Cada teste de integração usa seu próprio SQLite temporário, sem modificar o banco da demonstração.
