namespace Paymob.Net;

public sealed class PaymobOptions
{
    public string SecretKey { get; set; } = string.Empty;

    public string PublicKey { get; set; } = string.Empty;

    public string HmacSecret { get; set; } = string.Empty;

    public PaymobRegion Region { get; set; } = PaymobRegion.Egypt;

    public Uri? BaseUrl { get; set; }

    public bool IsTestMode => SecretKey.Contains("_test_", StringComparison.Ordinal);

    internal Uri ResolveBaseUrl() => BaseUrl ?? PaymobRegions.BaseUrl(Region);

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
            throw new PaymobConfigurationException("SecretKey is required.");
        if (string.IsNullOrWhiteSpace(PublicKey))
            throw new PaymobConfigurationException("PublicKey is required.");
    }
}