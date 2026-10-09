using System.Net;
using System.Text;
using System.Text.Json;

namespace Paymob.Net.Tests;

public class PaymobClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status) { Content = new StringContent(_body, Encoding.UTF8, "application/json") };
        }
    }

    private static PaymobOptions Options() => new()
    {
        SecretKey = "egy_sk_test_abc123",
        PublicKey = "egy_pk_test_xyz789",
        HmacSecret = Fixtures.HmacSecret
    };

    private static IntentionRequest Request() => new()
    {
        AmountCents = 36000,
        IntegrationIds = new[] { 4512345 },
        Items = new[] { new IntentionItem("Order #1005", 36000) },
        Billing = new BillingData { FirstName = "Ahmed", LastName = "Ali", PhoneNumber = "+201000000000", Email = "ahmed@example.com" },
        MerchantReference = "order-1005",
        NotificationUrl = "https://api.example.com/paymob/webhook"
    };

    [Fact]
    public async Task Creates_an_intention_with_the_expected_request()
    {
        var handler = new StubHandler(HttpStatusCode.Created,
            """{"id":"pi_test_1","client_secret":"egy_csk_test_1","intention_order_id":247254613,"special_reference":"order-1005"}""");
        var client = new PaymobClient(new HttpClient(handler), Options());

        var intention = await client.CreateIntentionAsync(Request());

        Assert.Equal("egy_csk_test_1", intention.ClientSecret);
        Assert.Equal(247254613, intention.GatewayOrderId);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://accept.paymob.com/v1/intention/", handler.Request.RequestUri!.ToString());
        Assert.Equal("Token", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("egy_sk_test_abc123", handler.Request.Headers.Authorization.Parameter);

        using var json = JsonDocument.Parse(handler.RequestBody!);
        var root = json.RootElement;
        Assert.Equal(36000, root.GetProperty("amount").GetInt64());
        Assert.Equal("EGP", root.GetProperty("currency").GetString());
        Assert.Equal(4512345, root.GetProperty("payment_methods")[0].GetInt32());
        Assert.Equal("Ahmed", root.GetProperty("billing_data").GetProperty("first_name").GetString());
        Assert.Equal("order-1005", root.GetProperty("special_reference").GetString());
        Assert.False(root.TryGetProperty("redirection_url", out _));
    }

    [Fact]
    public async Task Paymob_errors_surface_with_status_and_body()
    {
        var handler = new StubHandler(HttpStatusCode.BadRequest, """{"detail":"Invalid integration"}""");
        var client = new PaymobClient(new HttpClient(handler), Options());

        var ex = await Assert.ThrowsAsync<PaymobApiException>(() => client.CreateIntentionAsync(Request()));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Contains("Invalid integration", ex.ResponseBody);
    }

    [Fact]
    public async Task Items_that_do_not_add_up_are_rejected_before_calling_paymob()
    {
        var handler = new StubHandler(HttpStatusCode.Created, "{}");
        var client = new PaymobClient(new HttpClient(handler), Options());
        var request = new IntentionRequest
        {
            AmountCents = 36000,
            IntegrationIds = new[] { 1 },
            Items = new[] { new IntentionItem("Shirt", 30000) },
            Billing = new BillingData { FirstName = "A", LastName = "B", PhoneNumber = "1", Email = "a@b.c" }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateIntentionAsync(request));
        Assert.Null(handler.Request);
    }

    [Fact]
    public async Task Missing_client_secret_is_an_error()
    {
        var handler = new StubHandler(HttpStatusCode.Created, """{"id":"pi_test_1"}""");
        var client = new PaymobClient(new HttpClient(handler), Options());

        await Assert.ThrowsAsync<PaymobException>(() => client.CreateIntentionAsync(Request()));
    }

    [Fact]
    public void Builds_the_unified_checkout_url()
    {
        var client = new PaymobClient(new HttpClient(), Options());

        var url = client.GetCheckoutUrl("egy_csk_test_1");

        Assert.Equal("https://accept.paymob.com/unifiedcheckout/?publicKey=egy_pk_test_xyz789&clientSecret=egy_csk_test_1", url.ToString());
    }

    [Fact]
    public void Uses_the_regional_base_url()
    {
        var options = Options();
        options.Region = PaymobRegion.SaudiArabia;
        var client = new PaymobClient(new HttpClient(), options);

        Assert.StartsWith("https://ksa.paymob.com/", client.GetCheckoutUrl("x").ToString());
    }

    [Fact]
    public void Missing_keys_fail_fast() =>
        Assert.Throws<PaymobConfigurationException>(() => new PaymobClient(new HttpClient(), new PaymobOptions()));

    [Fact]
    public void Test_mode_is_detected_from_the_secret_key() => Assert.True(Options().IsTestMode);
}