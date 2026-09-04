# Segurança e privacidade da demonstração

Este é um protótipo acadêmico. Os controles abaixo não representam certificação ou garantia de conformidade legal.

## Dados e finalidade técnica

| Dado | Finalidade | Controle aplicado |
|---|---|---|
| Nome/e-mail | Identificação e login | Retornados no cadastro/perfil próprio; não no JWT |
| Senha | Autenticação | Apenas hash com PasswordHasher/PBKDF2; hash excluído de JSON |
| Endereço | Entrega | Exigido só para Entrega; descartado do contrato para retirada/consumo local |
| Pedido e pagamento mock | Operação e rastreabilidade | Cliente só acessa o próprio pedido; perfis operacionais podem consultar |
| Pontos/consentimento | Fidelização opcional | Valor, data de alteração e evento de auditoria; revogação disponível |
| UserId/EntityId/ação/data | Auditoria | Consulta restrita a Gerente/Administrador; sem senha/token/corpo da requisição |

Para a demonstração, use exclusivamente dados fictícios. O consentimento implementado refere-se apenas à fidelidade; não se presume consentimento genérico para qualquer finalidade.

## Proposta acadêmica de bases e retenção

Como hipótese a validar pelo controlador: nome, contato, pedido e endereço necessário à entrega se relacionam à execução do contrato (art. 7º, V); a fidelidade opcional depende de consentimento específico (art. 7º, I). Auditoria exige definição da finalidade e base aplicável, não uma autorização genérica para guardar tudo. O término do tratamento e as exceções de conservação são tratados nos arts. 15 e 16; acesso, correção e demais direitos constam do art. 18. Esta proposta não é parecer jurídico. [LGPD, texto oficial](https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709compilado.htm).

No protótipo, a política proposta é eliminar dados fictícios ao encerrar a avaliação. Para uso real, o responsável deve aprovar uma tabela de retenção por finalidade, obrigações aplicáveis e prazos de backup; não há prazo universal definido neste projeto. A anonimização proposta remove nome, e-mail e endereço, impede reidentificação nas estatísticas e considera cópias de segurança. A rotina e o canal de atendimento ao titular permanecem como evolução documentada.

## Implementado

- JWT com assinatura HMAC SHA-256, emissor, público destinatário e validade de uma hora; verificação de assinatura e prazo em todas as rotas protegidas.
- Cadastro público fixa perfil Cliente; privilégios operacionais não vêm do body enviado pelo usuário.
- Autorização antes de consultar/retornar pagamento por chave de idempotência.
- Validações de contrato, limitação de 30 requisições/minuto por IP no módulo auth e erros JSON sem stack traces.
- Logs sem payloads e sem dados pessoais; auditoria de login, cadastro, consulta de perfil/pedido, estoque, pagamento, status, cancelamento e consentimento.
- Preço calculado pelo servidor; transações serializáveis, tokens de concorrência e restrições de saldo não negativo.
- Chave privada obrigatória fora de Development; `.env` e bancos locais excluídos do Git.

## Limitações explícitas

- Contas administrativas de demonstração são públicas. Qualquer pessoa com essas credenciais pode modificar dados fictícios. Desabilitar o seed não remove contas que já existem; não reutilize esse banco em produção.
- Não há refresh/logout com revogação imediata, escopo de funcionário por unidade, trilha de consentimento com versão de política, gestão administrativa de usuários ou exclusão/anonimização automática.
- Logs/auditoria não têm retenção automática. A proposta é anonimizar identificação e endereços quando cessar a finalidade e expirar o prazo aprovado pelo responsável, mantendo apenas estatísticas necessárias; a rotina não foi implementada.
- O limitador é por processo/IP. Proxy reverso, múltiplas réplicas e encaminhamento confiável de IP exigem configuração específica no deploy.
- TLS deve ser fornecido pela hospedagem; a porta HTTP local serve somente ao desenvolvimento. Não habilitar CORS irrestrito nem enviar dados de cartão.
- Não há avaliação jurídica, teste de invasão ou teste de carga.
