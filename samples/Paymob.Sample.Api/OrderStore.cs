using System.Collections.Concurrent;
using Paymob.Net;

namespace Paymob.Sample.Api;

public sealed class Order
{
    public required int Id { get; init; }
    public required decimal Total { get; init; }
    public required string CustomerName { get; init; }
    public required string CustomerPhone { get; init; }
    public long? GatewayOrderId { get; set; }
    public string Status { get; set; } = "awaiting_payment";
    public long? TransactionId { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}

public sealed class OrderStore
{
    private readonly ConcurrentDictionary<int, Order> _orders = new();
    private int _next = 1000;

    public Order Create(decimal total, string name, string phone)
    {
        var order = new Order { Id = Interlocked.Increment(ref _next), Total = total, CustomerName = name, CustomerPhone = phone };
        _orders[order.Id] = order;
        return order;
    }

    public Order? Find(int id) => _orders.TryGetValue(id, out var o) ? o : null;

    public Order? FindByGatewayOrder(long gatewayOrderId) =>
        _orders.Values.FirstOrDefault(o => o.GatewayOrderId == gatewayOrderId);

    public Order? Apply(PaymobTransaction tx)
    {
        var order = FindByGatewayOrder(tx.GatewayOrderId);
        if (order is null) return null;

        lock (order)
        {
            var amountMatches = tx.AmountCents == Money.ToCents(order.Total) && tx.Currency == "EGP";
            switch (tx.Outcome)
            {
                case PaymentOutcome.Paid when !amountMatches:
                    order.Status = "amount_mismatch";
                    break;
                case PaymentOutcome.Paid:
                    order.Status = "paid";
                    order.TransactionId = tx.Id;
                    order.PaidAt ??= DateTimeOffset.UtcNow;
                    break;
                case PaymentOutcome.Failed when order.Status != "paid":
                    order.Status = "payment_failed";
                    break;
                case PaymentOutcome.Reversed when order.Status == "paid":
                    order.Status = "refunded";
                    break;
            }
        }

        return order;
    }
}