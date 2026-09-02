using RaizesDoNordeste.Application;
using RaizesDoNordeste.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace RaizesDoNordeste.Infrastructure
{
    public sealed class MockPaymentGateway : IPaymentGateway
    {
        public Task<PaymentResult> PayAsync(
            Guid orderId,
            decimal amount,
            bool approve,
            bool simulateFailure)
        {
            if (simulateFailure)
            {
                throw new BusinessException(
                    503,
                    "GATEWAY_INDISPONIVEL",
                    "Falha simulada. Tente novamente com a mesma chave; nenhum pagamento foi registrado.");
            }

            var reference = $"mock_{Guid.NewGuid():N}";

            var status = approve
                ? PaymentStatus.Aprovado
                : PaymentStatus.Recusado;

            var payload = JsonSerializer.Serialize(new
            {
                request = new
                {
                    orderId,
                    amount,
                    provider = "MOCK"
                },
                response = new
                {
                    reference,
                    status = status.ToString(),
                    message = approve
                        ? "Pagamento simulado aprovado."
                        : "Pagamento simulado recusado."
                }
            });

            return Task.FromResult(
                new PaymentResult(status, reference, payload));
        }
    }
}
