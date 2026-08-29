namespace RaizesDoNordeste.Domain
{
    public static class OrderRules
    {
        public const int PointsPerReward = 10;
        public static int EarnedPoints(decimal total) => (int)(total / 10m);

        public static bool CanTransition(OrderStatus current, OrderStatus next, ServiceType service) =>
            (current, next) switch
            {
                (OrderStatus.Aceito, OrderStatus.EmPreparo) => true,
                (OrderStatus.EmPreparo, OrderStatus.Pronto) => true,
                (OrderStatus.Pronto, OrderStatus.EmRota) => service == ServiceType.Entrega,
                (OrderStatus.Pronto, OrderStatus.Finalizado) => service != ServiceType.Entrega,
                (OrderStatus.EmRota, OrderStatus.Finalizado) => true,
                _ => false
            };
    }
}
