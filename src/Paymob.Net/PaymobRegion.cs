namespace Paymob.Net;

public enum PaymobRegion
{
    Egypt,
    SaudiArabia,
    UnitedArabEmirates,
    Oman
}

internal static class PaymobRegions
{
    public static Uri BaseUrl(PaymobRegion region) => region switch
    {
        PaymobRegion.Egypt => new Uri("https://accept.paymob.com/"),
        PaymobRegion.SaudiArabia => new Uri("https://ksa.paymob.com/"),
        PaymobRegion.UnitedArabEmirates => new Uri("https://uae.paymob.com/"),
        PaymobRegion.Oman => new Uri("https://oman.paymob.com/"),
        _ => throw new ArgumentOutOfRangeException(nameof(region), region, "Unknown Paymob region.")
    };
}