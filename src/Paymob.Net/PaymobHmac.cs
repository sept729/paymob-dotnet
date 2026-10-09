using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Paymob.Net;

public static class PaymobHmac
{
    private static readonly string[] TransactionFields =
    {
        "amount_cents", "created_at", "currency", "error_occured", "has_parent_transaction", "id",
        "integration_id", "is_3d_secure", "is_auth", "is_capture", "is_refunded", "is_standalone_payment",
        "is_voided", "order.id", "owner", "pending", "source_data.pan", "source_data.sub_type",
        "source_data.type", "success"
    };

    public static bool TryVerifyCallback(string body, string? hmac, string hmacSecret, out PaymobTransaction? transaction)
    {
        transaction = null;
        if (string.IsNullOrWhiteSpace(body) || string.IsNullOrWhiteSpace(hmac) || string.IsNullOrEmpty(hmacSecret))
            return false;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return false;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;
            if (root.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String && type.GetString() != "TRANSACTION")
                return false;
            if (!root.TryGetProperty("obj", out var obj) || obj.ValueKind != JsonValueKind.Object)
                return false;

            if (!Matches(hmacSecret, hmac, TransactionFields.Select(f => Read(obj, f))))
                return false;

            transaction = new PaymobTransaction(
                Id: Long(Read(obj, "id")),
                GatewayOrderId: Long(Read(obj, "order.id")),
                MerchantReference: NullIfEmpty(Read(obj, "order.merchant_order_id")),
                AmountCents: Long(Read(obj, "amount_cents")),
                Currency: Read(obj, "currency"),
                Success: Read(obj, "success") == "true",
                Pending: Read(obj, "pending") == "true",
                IsRefunded: Read(obj, "is_refunded") == "true",
                IsVoided: Read(obj, "is_voided") == "true",
                CardBrand: NullIfEmpty(Read(obj, "source_data.sub_type")),
                MaskedPan: NullIfEmpty(Read(obj, "source_data.pan")));
            return true;
        }
    }

    public static bool TryVerifyRedirect(IReadOnlyDictionary<string, string> query, string hmacSecret, out PaymobTransaction? transaction)
    {
        ArgumentNullException.ThrowIfNull(query);
        transaction = null;
        string Q(string key) => query.TryGetValue(key, out var v) ? v : string.Empty;

        if (!Matches(hmacSecret, Q("hmac"), TransactionFields.Select(f => f == "order.id" ? Q("order") : Q(f))))
            return false;

        transaction = new PaymobTransaction(
            Id: Long(Q("id")),
            GatewayOrderId: Long(Q("order")),
            MerchantReference: NullIfEmpty(Q("merchant_order_id")),
            AmountCents: Long(Q("amount_cents")),
            Currency: Q("currency"),
            Success: Q("success") == "true",
            Pending: Q("pending") == "true",
            IsRefunded: Q("is_refunded") == "true",
            IsVoided: Q("is_voided") == "true",
            CardBrand: NullIfEmpty(Q("source_data.sub_type")),
            MaskedPan: NullIfEmpty(Q("source_data.pan")));
        return true;
    }

    public static string Compute(string hmacSecret, IEnumerable<string> values)
    {
        ArgumentException.ThrowIfNullOrEmpty(hmacSecret);
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(hmacSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(string.Concat(values)));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool Matches(string secret, string? given, IEnumerable<string> values)
    {
        if (string.IsNullOrWhiteSpace(given) || string.IsNullOrEmpty(secret)) return false;
        var expected = Encoding.ASCII.GetBytes(Compute(secret, values));
        var actual = Encoding.ASCII.GetBytes(given.Trim().ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private static string Read(JsonElement obj, string path)
    {
        var current = obj;
        foreach (var part in path.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out var next))
                return string.Empty;
            current = next;
        }

        return current.ValueKind switch
        {
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.String => current.GetString() ?? string.Empty,
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            _ => current.GetRawText()
        };
    }

    private static long Long(string value) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;

    private static string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;
}