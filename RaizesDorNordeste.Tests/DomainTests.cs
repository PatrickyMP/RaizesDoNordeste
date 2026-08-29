using RaizesDoNordeste.Domain;

namespace RaizesDorNordeste.Tests
{
    public class DomainTests
    {
        [Fact]
        public void Pedido_novo_inicia_aguardando_pagamento()
        {
            var order = new Order();
            Assert.Equal(OrderStatus.AguardandoPagamento, order.Status);
        }

        [Theory]
        [InlineData(OrderStatus.Aceito, OrderStatus.EmPreparo, ServiceType.Retirada, true)]
        [InlineData(OrderStatus.AguardandoPagamento, OrderStatus.Pronto, ServiceType.Retirada, false)]
        [InlineData(OrderStatus.Pronto, OrderStatus.EmRota, ServiceType.Retirada, false)]
        [InlineData(OrderStatus.Pronto, OrderStatus.EmRota, ServiceType.Entrega, true)]
        [InlineData(OrderStatus.Pronto, OrderStatus.Finalizado, ServiceType.Entrega, false)]
        [InlineData(OrderStatus.Finalizado, OrderStatus.Finalizado, ServiceType.Retirada, false)]
        [InlineData(OrderStatus.EmPreparo, OrderStatus.Pronto, ServiceType.Retirada, true)]
        [InlineData(OrderStatus.Pronto, OrderStatus.Finalizado, ServiceType.Retirada, true)]
        [InlineData(OrderStatus.Pronto, OrderStatus.Finalizado, ServiceType.ConsumoLocal, true)]
        [InlineData(OrderStatus.EmRota, OrderStatus.Finalizado, ServiceType.Entrega, true)]
        [InlineData(OrderStatus.Aceito, OrderStatus.Finalizado, ServiceType.Retirada, false)]



        public void Transicoes_respeitam_fluxo_e_tipo_servico(OrderStatus current, OrderStatus next, ServiceType service, bool expected)
        {
            var resultado = OrderRules.CanTransition(current, next, service);

            Assert.Equal(expected, resultado);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(9, 0)]
        [InlineData(10, 1)]
        [InlineData(24, 2)]
        [InlineData(99, 9)]
        public void Pontos_sao_calculados_a_cada_dez_reais(int total, int esperado)
        {
            var resultado = OrderRules.EarnedPoints(total);

            Assert.Equal(esperado, resultado);
        }
    }
}
