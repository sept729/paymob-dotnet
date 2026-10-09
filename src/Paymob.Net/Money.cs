namespace Paymob.Net;

public static class Money
{
    public static long ToCents(decimal amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount can't be negative.");
        return (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
    }

    public static decimal FromCents(long cents) => cents / 100m;
}