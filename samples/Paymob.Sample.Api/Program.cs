using Paymob.Net;
using Paymob.Sample.Api;

var builder = WebApplication.CreateBuilder(args);

var paymob = builder.Configuration.GetSection("Paymob");
var options = new PaymobOptions
{
    SecretKey = paymob["SecretKey"] ?? string.Empty,
    PublicKey = paymob["PublicKey"] ?? string.Empty,
    HmacSecret = paymob["HmacSecret"] ?? string.Empty
};
var integrationIds = paymob.GetSection("IntegrationIds").Get<int[]>() ?? Array.Empty<int>();
var publicBaseUrl = (paymob["PublicBaseUrl"] ?? string.Empty).TrimEnd('/');

builder.Services.AddSingleton(options);
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddHttpClient<PaymobClient>(c => c.Timeout = TimeSpan.FromSeconds(20));

var app = builder.Build();

app.MapPost("/orders", async (CreateOrder input, OrderStore orders, PaymobClient client, CancellationToken ct) =>
{
    if (input.Total <= 0) return Results.BadRequest(new { error = "Total must be greater than zero." });

    var order = orders.Create(input.Total, input.Name, input.Phone);
    var cents = Money.ToCents(order.Total);
    var names = input.Name.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

    var intention = await client.CreateIntentionAsync(new IntentionRequest
    {
        AmountCents = cents,
        IntegrationIds = integrationIds,
        Items = new[] { new IntentionItem($"Order #{order.Id}", cents) },
        Billing = new BillingData
        {
            FirstName = names.ElementAtOrDefault(0) ?? "Customer",
            LastName = names.ElementAtOrDefault(1) ?? "NA",
            PhoneNumber = input.Phone,
            Email = input.Email ?? $"order{order.Id}@example.com"
        },
        MerchantReference = $"order-{order.Id}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
        NotificationUrl = $"{publicBaseUrl}/paymob/webhook",
        RedirectionUrl = $"{publicBaseUrl}/paymob/return"
    }, ct);

    order.GatewayOrderId = intention.GatewayOrderId;
    return Results.Ok(new { orderId = order.Id, checkoutUrl = client.GetCheckoutUrl(intention.ClientSecret) });
});

app.MapGet("/orders/{id:int}", (int id, OrderStore orders) =>
    orders.Find(id) is { } order ? Results.Ok(order) : Results.NotFound());

app.MapPost("/paymob/webhook", async (HttpRequest request, OrderStore orders, ILogger<Program> log) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();

    if (!PaymobHmac.TryVerifyCallback(body, request.Query["hmac"], options.HmacSecret, out var tx))
    {
        log.LogWarning("Rejected a Paymob callback with an invalid signature.");
        return Results.Unauthorized();
    }

    var order = orders.Apply(tx!);
    log.LogInformation("Paymob transaction {Tx} for gateway order {Gid}: {Outcome} -> {Status}",
        tx!.Id, tx.GatewayOrderId, tx.Outcome, order?.Status ?? "no matching order");
    return Results.Ok();
});

app.MapGet("/paymob/return", (HttpRequest request, OrderStore orders) =>
{
    var query = request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
    if (!PaymobHmac.TryVerifyRedirect(query, options.HmacSecret, out var tx))
        return Results.BadRequest("This link isn't signed by Paymob.");

    var order = orders.Apply(tx!);
    return Results.Ok(new { orderId = order?.Id, status = order?.Status ?? "unknown", outcome = tx!.Outcome.ToString() });
});

app.Run();

public sealed record CreateOrder(decimal Total, string Name, string Phone, string? Email);