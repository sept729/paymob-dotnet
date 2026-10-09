using System.Text.Json.Serialization;

namespace Paymob.Net;

public sealed record IntentionItem(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("amount")] long AmountCents,
    [property: JsonPropertyName("quantity")] int Quantity = 1,
    [property: JsonPropertyName("description")] string? Description = null);

public sealed class BillingData
{
    [JsonPropertyName("first_name")] public required string FirstName { get; init; }
    [JsonPropertyName("last_name")] public required string LastName { get; init; }
    [JsonPropertyName("phone_number")] public required string PhoneNumber { get; init; }
    [JsonPropertyName("email")] public required string Email { get; init; }
    [JsonPropertyName("street")] public string Street { get; init; } = "NA";
    [JsonPropertyName("building")] public string Building { get; init; } = "NA";
    [JsonPropertyName("apartment")] public string Apartment { get; init; } = "NA";
    [JsonPropertyName("floor")] public string Floor { get; init; } = "NA";
    [JsonPropertyName("city")] public string City { get; init; } = "NA";
    [JsonPropertyName("state")] public string State { get; init; } = "NA";
    [JsonPropertyName("country")] public string Country { get; init; } = "EG";
}

public sealed class IntentionRequest
{
    [JsonPropertyName("amount")] public required long AmountCents { get; init; }
    [JsonPropertyName("currency")] public string Currency { get; init; } = "EGP";
    [JsonPropertyName("payment_methods")] public required IReadOnlyList<int> IntegrationIds { get; init; }
    [JsonPropertyName("items")] public required IReadOnlyList<IntentionItem> Items { get; init; }
    [JsonPropertyName("billing_data")] public required BillingData Billing { get; init; }
    [JsonPropertyName("special_reference")] public string? MerchantReference { get; init; }
    [JsonPropertyName("notification_url")] public string? NotificationUrl { get; init; }
    [JsonPropertyName("redirection_url")] public string? RedirectionUrl { get; init; }
    [JsonPropertyName("extras")] public IReadOnlyDictionary<string, object?>? Extras { get; init; }
}

public sealed class Intention
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("client_secret")] public string ClientSecret { get; init; } = string.Empty;
    [JsonPropertyName("intention_order_id")] public long GatewayOrderId { get; init; }
    [JsonPropertyName("special_reference")] public string? MerchantReference { get; init; }
}

public sealed record PaymobTransaction(
    long Id,
    long GatewayOrderId,
    string? MerchantReference,
    long AmountCents,
    string Currency,
    bool Success,
    bool Pending,
    bool IsRefunded,
    bool IsVoided,
    string? CardBrand,
    string? MaskedPan)
{
    public PaymentOutcome Outcome =>
        IsRefunded || IsVoided ? PaymentOutcome.Reversed
        : Pending ? PaymentOutcome.Pending
        : Success ? PaymentOutcome.Paid
        : PaymentOutcome.Failed;
}

public enum PaymentOutcome
{
    Paid,
    Failed,
    Pending,
    Reversed
}