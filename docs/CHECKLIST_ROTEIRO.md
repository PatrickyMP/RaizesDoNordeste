# Checklist do roteiro

| Exigência | Evidência |
|---|---|
| RF/RNF e prioridades | `docs/ARQUITETURA.md` |
| Cadastro, login e perfis | JWT, hash e roles nos controllers |
| Unidades, produtos e cardápio | CRUD lógico e consulta por unidade |
| `canalPedido` obrigatório e filtro | Enum, validação, persistência e testes |
| Fluxo crítico | Pedido, mock, estoque e status ponta a ponta |
| Pagamento mock | Aprovação, recusa, falha e idempotência |
| Estoque | Entrada, saída, baixa, estorno e histórico |
| Fidelidade | Consentimento, pontos e resgate |
| Promoções | Proposta documentada; não implementada |
| Camadas | Domain, Application, Infrastructure e API |
| PostgreSQL/migration/seed | Migration versionada e seed demonstrativo |
| Swagger/OpenAPI | Contrato real, Bearer e exemplos |
| Coleção Postman | 44 requisições organizadas |
| Plano mínimo de 10 cenários | `docs/PLANO_DE_TESTES.md` |
| Testes automatizados | 36 testes aprovados |
| Segurança/LGPD/auditoria | `docs/SEGURANCA.md` |
| DER | `docs/der.png` e `docs/der.svg` |
| Casos de uso | `docs/casos-de-uso.png` |
| Classes | `docs/classes.png` |
| Sequência | `docs/sequencia.png` |
| README reproduzível | Raiz do repositório |
| `.env.example` | Raiz, sem segredos reais |
| Repositório público | https://github.com/PatrickyMP/RaizesDoNordeste |
| PDF único | Gerado em `output/pdf` após identificação acadêmica |

Hospedagem e Docker são opcionais. Nome, RU, curso, instituição e cidade precisam ser confirmados pelo aluno antes de gerar o PDF identificado.
