namespace Paymob.Net.Tests;

public class PaymobHmacTests
{
    [Fact]
    public void Callback_with_valid_signature_is_accepted()
    {
        var ok = PaymobHmac.TryVerifyCallback(Fixtures.CallbackBody, Fixtures.CallbackHmac, Fixtures.HmacSecret, out var tx);

        Assert.True(ok);
        Assert.NotNull(tx);
        Assert.Equal(192837465, tx!.Id);
        Assert.Equal(247254613, tx.GatewayOrderId);
        Assert.Equal("order-1005", tx.MerchantReference);
        Assert.Equal(36000, tx.AmountCents);
        Assert.Equal("EGP", tx.Currency);
        Assert.Equal(PaymentOutcome.Paid, tx.Outcome);
        Assert.Equal("MasterCard", tx.CardBrand);
    }

    [Fact]
    public void Signature_comparison_ignores_case()
    {
        var ok = PaymobHmac.TryVerifyCallback(Fixtures.CallbackBody, Fixtures.CallbackHmac.ToUpperInvariant(), Fixtures.HmacSecret, out _);

        Assert.True(ok);
    }

    [Fact]
    public void Callback_with_wrong_secret_is_rejected()
    {
        var ok = PaymobHmac.TryVerifyCallback(Fixtures.CallbackBody, Fixtures.CallbackHmac, "another_secret", out var tx);

        Assert.False(ok);
        Assert.Null(tx);
    }

    [Fact]
    public void Tampered_amount_is_rejected()
    {
        var body = Fixtures.CallbackBody.Replace("\"amount_cents\": 36000", "\"amount_cents\": 100");

        Assert.False(PaymobHmac.TryVerifyCallback(body, Fixtures.CallbackHmac, Fixtures.HmacSecret, out _));
    }

    [Fact]
    public void Flipping_success_is_rejected()
    {
        var body = Fixtures.CallbackBody.Replace("\"success\": true", "\"success\": false");

        Assert.False(PaymobHmac.TryVerifyCallback(body, Fixtures.CallbackHmac, Fixtures.HmacSecret, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"type\":\"TOKEN\",\"obj\":{}}")]
    public void Unusable_bodies_are_rejected(string body)
    {
        Assert.False(PaymobHmac.TryVerifyCallback(body, Fixtures.CallbackHmac, Fixtures.HmacSecret, out _));
    }

    [Fact]
    public void Missing_signature_is_rejected()
    {
        Assert.False(PaymobHmac.TryVerifyCallback(Fixtures.CallbackBody, null, Fixtures.HmacSecret, out _));
    }

    [Fact]
    public void Redirect_query_with_valid_signature_is_accepted()
    {
        var ok = PaymobHmac.TryVerifyRedirect(Fixtures.RedirectQuery(), Fixtures.HmacSecret, out var tx);

        Assert.True(ok);
        Assert.Equal(247254613, tx!.GatewayOrderId);
        Assert.Equal(PaymentOutcome.Paid, tx.Outcome);
    }

    [Fact]
    public void Tampered_redirect_query_is_rejected()
    {
        var query = Fixtures.RedirectQuery();
        query["order"] = "999";

        Assert.False(PaymobHmac.TryVerifyRedirect(query, Fixtures.HmacSecret, out _));
    }

    [Fact]
    public void Merchant_reference_is_not_part_of_the_signature()
    {
        var query = Fixtures.RedirectQuery();
        query["merchant_order_id"] = "someone-else";

        Assert.True(PaymobHmac.TryVerifyRedirect(query, Fixtures.HmacSecret, out var tx));
        Assert.Equal("someone-else", tx!.MerchantReference);
        Assert.Equal(247254613, tx.GatewayOrderId);
    }
}