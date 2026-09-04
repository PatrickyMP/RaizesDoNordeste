# Verificação executada

Data local da revisão: **02/09/2026**.

| Verificação | Resultado |
|---|---|
| `dotnet build` | Sucesso, zero erros e zero avisos antes da documentação |
| `dotnet test` | 36 aprovados, zero falhas, incluindo testes de integração |
| PostgreSQL e migration | Conexão aprovada; `has-pending-model-changes`: nenhuma alteração pendente |
| `/health` | HTTP 200, `status: ok` |
| Swagger/OpenAPI | HTTP 200; 22 caminhos, 23 schemas, Bearer e exemplos |
| Login JWT | Cliente e perfis operacionais autenticados |
| Fluxo manual | Pedido criado, pagamento mock, preparo, pronto e finalização confirmados |
| Publicação Release | `dotnet publish` concluído sem erro |
| Docker Compose | Sintaxe validada com `docker compose config --quiet`; engine não iniciada |
| Repositório público | Link respondeu HTTP 200 sem autenticação; alterações finais ainda locais |
| Estoque/fidelidade/auditoria | Cobertos pela suíte de integração |

Os testes de integração criam SQLite temporário isolado e verificam autenticação/autorização, contratos, pedidos, pagamento, idempotência, estoque, concorrência, cancelamento, fidelidade, auditoria, paginação e Swagger.

A coleção Postman foi executada no PostgreSQL: 44 requisições e 79 asserções aprovadas, com zero falhas. Evidência: `docs/evidencias/postman-postgres.xml`. Docker permanece opcional e não foi declarado como executado neste computador enquanto a virtualização estiver indisponível.

As verificações não representam teste de carga, pentest, certificação jurídica ou garantia de disponibilidade em produção.
