# Raízes do Nordeste API

API REST acadêmica para uma rede de lanchonetes, construída em **.NET 10**, **ASP.NET Core**, **Entity Framework Core** e **PostgreSQL**. O fluxo principal cobre pedido multicanal, pagamento simulado, baixa de estoque, preparo, finalização, fidelidade e auditoria.

> Use somente dados fictícios. O pagamento é um mock: não existe cobrança real nem coleta de cartão.

## Evidências principais

- Repositório: https://github.com/PatrickyMP/RaizesDoNordeste
- Swagger local: `http://localhost:5287/swagger`
- OpenAPI: `http://localhost:5287/swagger/v1/swagger.json`
- Saúde: `http://localhost:5287/health`
- Coleção: [`postman/RaizesDoNordeste.postman_collection.json`](postman/RaizesDoNordeste.postman_collection.json)
- DER: [`docs/der.png`](docs/der.png)
- Plano de testes: [`docs/PLANO_DE_TESTES.md`](docs/PLANO_DE_TESTES.md)

Hospedagem online não é necessária. O avaliador clona o repositório e executa localmente pelas instruções abaixo.

## Pré-requisitos

- Git
- .NET SDK 10
- PostgreSQL 17 ou 18
- Postman (para a coleção; opcional se usar somente Swagger e `dotnet test`)

Docker é opcional. O projeto funciona com PostgreSQL instalado diretamente no computador.

## 1. Clonar e restaurar

```powershell
git clone https://github.com/PatrickyMP/RaizesDoNordeste.git
cd RaizesDoNordeste
dotnet tool restore
dotnet restore
```

## 2. Criar usuário e banco PostgreSQL

Abra o SQL Shell (`psql`) como administrador do PostgreSQL e execute, escolhendo uma senha local:

```sql
CREATE ROLE root WITH LOGIN PASSWORD 'SUA_SENHA_LOCAL';
CREATE DATABASE raizes_nordeste OWNER root;
```

Se o usuário ou banco já existirem, não os recrie. Use as credenciais locais existentes.

## 3. Configurar segredos locais

O arquivo `.env.example` documenta as variáveis, mas o .NET não carrega `.env` automaticamente. Para desenvolvimento local, use User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=raizes_nordeste;Username=root;Password=SUA_SENHA_LOCAL" --project .\RaizesDoNordeste.API\RaizesDoNordeste.API.csproj
```

Gere uma chave JWT sem imprimi-la:

```powershell
$bytes = New-Object byte[] 48
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$jwtKey = [Convert]::ToBase64String($bytes)
$rng.Dispose()
dotnet user-secrets set "Jwt:Key" "$jwtKey" --project .\RaizesDoNordeste.API\RaizesDoNordeste.API.csproj
dotnet user-secrets set "Demo:Seed" "true" --project .\RaizesDoNordeste.API\RaizesDoNordeste.API.csproj
```

Nunca coloque senha do banco, chave JWT ou token no Git.

## 4. Aplicar migration e executar

```powershell
dotnet ef database update --project .\RaizesDoNordeste.Infrastructure\RaizesDoNordeste.Infrastructure.csproj --startup-project .\RaizesDoNordeste.API\RaizesDoNordeste.API.csproj --context AppDbContext
dotnet run --project .\RaizesDoNordeste.API\RaizesDoNordeste.API.csproj
```

A API também aplica migrations pendentes ao iniciar. Acesse `http://localhost:5287/swagger`.

## Contas fictícias do seed

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador | `admin@raizes.local` | `Admin@123` |
| Cliente | `cliente@raizes.local` | `Cliente@123` |
| Atendente | `atendente@raizes.local` | `Atendente@123` |
| Cozinha | `cozinha@raizes.local` | `Cozinha@123` |
| Entregador | `entregador@raizes.local` | `Entregador@123` |

Essas credenciais são públicas e exclusivas da demonstração. O cadastro público sempre cria um Cliente.

## Fluxo principal pelo Swagger

1. Faça `POST /auth/login` como Cliente e copie somente `accessToken`.
2. Clique em **Authorize** e cole o token.
3. Consulte `GET /unidades` e `GET /unidades/{branchId}/cardapio`.
4. Crie `POST /pedidos` com IDs reais:

```json
{
  "branchId": "ID_DA_UNIDADE",
  "canalPedido": "TOTEM",
  "serviceType": "Retirada",
  "deliveryAddress": null,
  "items": [
    {
      "productId": "ID_DO_PRODUTO",
      "quantity": 1,
      "useReward": false
    }
  ]
}
```

5. Pague em `POST /pedidos/{id}/pagamentos`:

```json
{
  "idempotencyKey": "pedido-swagger-001",
  "approve": true,
  "simulateFailure": false
}
```

6. Como Cozinha, altere para `EmPreparo` e `Pronto`.
7. Como Atendente, finalize uma retirada com `Finalizado`.
8. Volte ao Cliente e consulte `/fidelidade`; como Administrador, consulte `/auditoria`.

Para uma Entrega, informe endereço e use o Entregador para `EmRota` e `Finalizado`.

## Regras implementadas

- `canalPedido`: `APP`, `TOTEM`, `BALCAO`, `PICKUP` ou `WEB`; é obrigatório e filtrável.
- Pedido começa em `AguardandoPagamento`; o preço sempre vem do banco.
- Aprovação do mock baixa o estoque e move para `Aceito`; recusa mantém o pedido pendente.
- `simulateFailure: true` devolve 503 e não grava pagamento.
- A chave idempotente impede registro duplicado e conflito entre pedidos.
- Itens repetidos são somados na validação do estoque.
- Cancelamento antes do preparo devolve estoque/pontos e estorna o pagamento mock.
- Com consentimento, cada R$ 10 finalizados gera 1 ponto; um item elegível resgatado consome 10 pontos.
- Cliente acessa somente os próprios pedidos. Perfis operacionais possuem permissões distintas.
- Listagens usam `page >= 1` e `1 <= limit <= 100`.
- Erros têm o formato uniforme `error`, `message`, `details`, `timestamp`, `path` e `requestId`.

Promoções/campanhas foram documentadas como evolução: regras por unidade/canal, vigência, prioridade e limite de uso. Não são apresentadas como implementadas.

## Testes automatizados

```powershell
dotnet test
```

Os testes de integração usam um SQLite temporário isolado e não alteram o PostgreSQL da API. A validação local atual possui **36 testes aprovados**.

## Coleção Postman

1. Inicie a API na porta 5287.
2. Importe `postman/RaizesDoNordeste.postman_collection.json`.
3. Abra o Collection Runner e execute a coleção completa, na ordem.

Ela possui 44 requisições e 79 asserções, executadas com zero falhas no PostgreSQL durante a revisão. As pastas cobrem Auth/preparação, pedidos, pagamento, status/auditoria, erros, cancelamento e gestão. Tokens e IDs são capturados automaticamente. Cada execução cria uma unidade e um produto próprios.

## Docker + PostgreSQL (opcional)

Copie `.env.example` para `.env`, substitua os valores e execute:

```powershell
docker compose up --build -d
docker compose logs -f api
```

O PostgreSQL fica em um container separado e o Swagger permanece em `http://localhost:5287/swagger`. Docker requer virtualização/WSL funcionando no Windows.

## Estrutura

```text
RaizesDoNordeste.API/             Controllers, contratos, JWT, erros e Swagger
RaizesDoNordeste.Application/     Casos de uso e interfaces
RaizesDoNordeste.Domain/          Entidades, enums e regras puras
RaizesDoNordeste.Infrastructure/  EF Core, migration e gateway mock
RaizesDoNordeste.Tests/           Testes de domínio e integração
docs/                             Diagramas e documentação técnica
postman/                          Coleção executável
.github/workflows/                Integração contínua
```

## Documentação

- [Requisitos e arquitetura](docs/ARQUITETURA.md)
- [Banco e migration](docs/BANCO.md)
- [Endpoints e contratos](docs/ENDPOINTS.md)
- [Segurança e LGPD](docs/SEGURANCA.md)
- [Plano de testes](docs/PLANO_DE_TESTES.md)
- [Checklist do roteiro](docs/CHECKLIST_ROTEIRO.md)
- [Verificação executada](docs/VERIFICACAO.md)
- [Orientações do relatório](docs/RELATORIO.md)
## Limitações declaradas

Não há pagamento real, refresh/logout com revogação, expiração automática de pedidos, anonimização automática, vínculo do funcionário a uma única unidade, teste de carga ou pentest. Hospedagem online e Docker não são necessários para o MVP local e permanecem opcionais.
