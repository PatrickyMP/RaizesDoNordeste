# Banco de dados e migration

## Tecnologia

A API usa PostgreSQL com Entity Framework Core e o provedor Npgsql. `AppDbContext` representa a unidade de trabalho. A migration inicial está versionada em `RaizesDoNordeste.Infrastructure/Migrations`.

Os testes de integração selecionam SQLite apenas no ambiente `Testing`, criam um arquivo temporário por teste com `EnsureCreatedAsync()` e o removem ao finalizar. Esse banco isolado não substitui nem altera a migration PostgreSQL.

## Esquema

O modelo possui `Users`, `Branches`, `Products`, `Stocks`, `StockMovements`, `Orders`, `OrderItems`, `Payments` e `AuditLogs`. O DER compatível está em `docs/der.png` e `docs/der.svg`.

Restrições principais:

- e-mail único;
- chave de idempotência única;
- apenas um estoque para cada combinação unidade/produto;
- estoque e pontos não negativos;
- precisão monetária `(12,2)`;
- FKs para usuário, unidade, produto, pedido e estoque;
- tokens de concorrência em usuário, estoque e pedido.

## Configuração local

```text
Host=localhost;Port=5432;Database=raizes_nordeste;Username=root;Password=SUA_SENHA
```

Configure por User Secrets, conforme o README. Não publique a senha.

## Comandos

Restaurar ferramenta local:

```powershell
dotnet tool restore
```

Aplicar migrations:

```powershell
dotnet ef database update --project .\RaizesDoNordeste.Infrastructure\RaizesDoNordeste.Infrastructure.csproj --startup-project .\RaizesDoNordeste.API\RaizesDoNordeste.API.csproj --context AppDbContext
```

Verificar alterações pendentes no modelo:

```powershell
dotnet ef migrations has-pending-model-changes --project .\RaizesDoNordeste.Infrastructure\RaizesDoNordeste.Infrastructure.csproj --startup-project .\RaizesDoNordeste.API\RaizesDoNordeste.API.csproj --context AppDbContext
```

A inicialização normal chama `MigrateAsync()` antes do seed. Em banco vazio, `Demo:Seed=true` cria apenas contas e catálogo fictícios. O seed é idempotente: se já houver usuários, ele não repete os dados.

## Consistência transacional

Criação, pagamento, baixa/estorno de estoque, cancelamento, pontos e auditoria usam transações. O pagamento aprovado confirma novamente o saldo dentro da transação. A chave idempotente evita repetir o registro. Conflitos de concorrência retornam HTTP 409.

Não use `EnsureCreated` no PostgreSQL normal e não apague o banco como estratégia de atualização. Mudanças futuras no modelo devem gerar migrations adicionais versionadas.
