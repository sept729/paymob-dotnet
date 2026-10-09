namespace Paymob.Net.Tests;

public class OutcomeTests
{
    private static PaymobTransaction Tx(bool success, bool pending = false, bool refunded = false, bool voided = false) =>
        new(1, 2, null, 100, "EGP", success, pending, refunded, voided, null, null);

    [Fact]
    public void Successful_settled_payment_is_paid() => Assert.Equal(PaymentOutcome.Paid, Tx(success: true).Outcome);

    [Fact]
    public void Declined_payment_is_failed() => Assert.Equal(PaymentOutcome.Failed, Tx(success: false).Outcome);

    [Fact]
    public void Pending_wins_over_success() => Assert.Equal(PaymentOutcome.Pending, Tx(success: true, pending: true).Outcome);

    [Fact]
    public void Refund_is_reversed() => Assert.Equal(PaymentOutcome.Reversed, Tx(success: true, refunded: true).Outcome);

    [Fact]
    public void Void_is_reversed() => Assert.Equal(PaymentOutcome.Reversed, Tx(success: true, voided: true).Outcome);

    [Theory]
    [InlineData(360, 36000)]
    [InlineData(99.99, 9999)]
    [InlineData(10.005, 1001)]
    [InlineData(0.01, 1)]
    public void Money_converts_to_cents(double amount, long cents) => Assert.Equal(cents, Money.ToCents((decimal)amount));

    [Fact]
    public void Money_rejects_negative_amounts() => Assert.Throws<ArgumentOutOfRangeException>(() => Money.ToCents(-1m));
}