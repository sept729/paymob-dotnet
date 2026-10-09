namespace Paymob.Net.Tests;

internal static class Fixtures
{
    public const string HmacSecret = "test_hmac_secret_123";

    public const string CallbackHmac =
        "6d5904e591d1875918bf3a7c1889c0457d44c723c145aebdd41367c4d2c210862221f6f25198caaa7340fb2278fb04aa41929c7cc4e800470be084e837fd26b6";

    public const string CallbackBody = """
    {
      "type": "TRANSACTION",
      "obj": {
        "id": 192837465,
        "pending": false,
        "amount_cents": 36000,
        "success": true,
        "is_auth": false,
        "is_capture": false,
        "is_standalone_payment": true,
        "is_voided": false,
        "is_refunded": false,
        "is_3d_secure": true,
        "integration_id": 4512345,
        "has_parent_transaction": false,
        "order": { "id": 247254613, "merchant_order_id": "order-1005" },
        "created_at": "2026-10-09T13:05:12.123456",
        "currency": "EGP",
        "error_occured": false,
        "owner": 302852,
        "source_data": { "pan": "2346", "type": "card", "sub_type": "MasterCard" }
      }
    }
    """;

    public static Dictionary<string, string> RedirectQuery() => new()
    {
        ["id"] = "192837465",
        ["pending"] = "false",
        ["amount_cents"] = "36000",
        ["success"] = "true",
        ["is_auth"] = "false",
        ["is_capture"] = "false",
        ["is_standalone_payment"] = "true",
        ["is_voided"] = "false",
        ["is_refunded"] = "false",
        ["is_3d_secure"] = "true",
        ["integration_id"] = "4512345",
        ["has_parent_transaction"] = "false",
        ["order"] = "247254613",
        ["merchant_order_id"] = "order-1005",
        ["created_at"] = "2026-10-09T13:05:12.123456",
        ["currency"] = "EGP",
        ["error_occured"] = "false",
        ["owner"] = "302852",
        ["source_data.pan"] = "2346",
        ["source_data.type"] = "card",
        ["source_data.sub_type"] = "MasterCard",
        ["hmac"] = CallbackHmac,
    };
}