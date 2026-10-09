# Paymob.Net

[![CI](https://github.com/sept729/paymob-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/sept729/paymob-dotnet/actions/workflows/ci.yml)

An unofficial .NET 8 client for [Paymob](https://paymob.com)'s Intention API and Unified Checkout, with signature verification for transaction callbacks.

I wrote this while adding card payments to [SEPT](https://sept.cloud), where each store owner connects their own Paymob account. The two parts that took the most care were building the intention correctly and verifying Paymob's HMAC, so both live here as a small library with no dependencies outside the .NET base library.

## What it does

- Creates a payment intention (`POST /v1/intention/`) and gives you the Unified Checkout URL to send the customer to.
- Verifies the HMAC-SHA512 signature on Paymob's transaction callback (POST) and on the redirect back to your site (GET).
- Turns a verified transaction into one outcome: `Paid`, `Failed`, `Pending` or `Reversed`.
- Fails early on the mistakes Paymob rejects anyway, such as item amounts that don't add up to the total.

Card details never touch your server. The customer pays on Paymob's hosted page.

## Quick start

```csharp
builder.Services.AddSingleton(new PaymobOptions
{
    SecretKey = builder.Configuration["Paymob:SecretKey"]!,
    PublicKey = builder.Configuration["Paymob:PublicKey"]!,
    HmacSecret = builder.Configuration["Paymob:HmacSecret"]!
});
builder.Services.AddHttpClient<PaymobClient>();
```

Create an intention and redirect the customer:

```csharp
var cents = Money.ToCents(order.Total);

var intention = await paymob.CreateIntentionAsync(new IntentionRequest
{
    AmountCents = cents,
    IntegrationIds = new[] { 4512345 },
    Items = new[] { new IntentionItem($"Order #{order.Id}", cents) },
    Billing = new BillingData
    {
        FirstName = "Ahmed",
        LastName = "Ali",
        PhoneNumber = "+201000000000",
        Email = "ahmed@example.com"
    },
    MerchantReference = $"order-{order.Id}",
    NotificationUrl = "https://api.example.com/paymob/webhook",
    RedirectionUrl = "https://api.example.com/paymob/return"
});

order.GatewayOrderId = intention.GatewayOrderId;
return Results.Redirect(paymob.GetCheckoutUrl(intention.ClientSecret).ToString());
```

Handle the callback:

```csharp
app.MapPost("/paymob/webhook", async (HttpRequest request) =>
{
    var body = await new StreamReader(request.Body).ReadToEndAsync();

    if (!PaymobHmac.TryVerifyCallback(body, request.Query["hmac"], options.HmacSecret, out var tx))
        return Results.Unauthorized();

    var order = await orders.FindByGatewayOrderAsync(tx!.GatewayOrderId);
    if (order is not null && tx.Outcome == PaymentOutcome.Paid && tx.AmountCents == Money.ToCents(order.Total))
        await orders.MarkPaidAsync(order, tx.Id);

    return Results.Ok();
});
```

## Things worth knowing

**Match orders by `GatewayOrderId`, not `MerchantReference`.** Paymob's signature covers `order.id` but not `merchant_order_id`. The redirect URL is visible to the customer, so someone who paid for a cheap order could change `merchant_order_id` and replay it against a different order. The signature would still pass. Store `intention.GatewayOrderId` when you create the intention and look the order up by it. There is a test for exactly this case.

**Check the amount yourself.** A valid signature proves Paymob sent the transaction. It doesn't prove the customer paid what the order costs.

**Only trust the server side.** The redirect is convenient for showing the customer a result, but a customer can close the tab before it happens. The callback is what should update your order.

**`MerchantReference` must be unique per intention.** If a customer retries a payment, add a timestamp or counter so the second intention doesn't collide with the first.

## Regions

`PaymobRegion.Egypt` is the default (`accept.paymob.com`). Saudi Arabia, the UAE and Oman are available through `PaymobOptions.Region`, or you can set `BaseUrl` directly. I have only run this against the Egypt endpoints.

## Sample

[`samples/Paymob.Sample.Api`](samples/Paymob.Sample.Api) is a minimal API with an in-memory order store that creates an order, sends you to checkout and handles both the callback and the redirect.

```bash
cd samples/Paymob.Sample.Api
dotnet user-secrets set "Paymob:SecretKey" "egy_sk_test_..."
dotnet user-secrets set "Paymob:PublicKey" "egy_pk_test_..."
dotnet user-secrets set "Paymob:HmacSecret" "..."
dotnet user-secrets set "Paymob:IntegrationIds:0" "4512345"
dotnet user-secrets set "Paymob:PublicBaseUrl" "https://your-tunnel.example.com"
dotnet run
```

Paymob has to reach your callback URL, so use a tunnel such as `ngrok` or Cloudflare Tunnel when running locally.

## Tests

```bash
dotnet test
```

The HMAC tests use a signature computed independently, outside this library, so a mistake in the field order or in how booleans are written would fail them.

## Status

Early version. It covers what I needed in production: intentions, checkout and callback verification. Refunds, voids and saved cards aren't wrapped yet.

This project isn't affiliated with Paymob.

## License

MIT