using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paymob.Net;

public sealed class PaymobClient
{
    private static readonly JsonSerializerOptions Json = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly PaymobOptions _options;

    public PaymobClient(HttpClient http, PaymobOptions options)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _http = http;
        _options = options;
    }

    public async Task<Intention> CreateIntentionAsync(IntentionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.ResolveBaseUrl(), "v1/intention/"))
        {
            Content = JsonContent.Create(request, options: Json)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Token", _options.SecretKey);

        using var response = await _http.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new PaymobApiException(response.StatusCode, body);

        Intention? intention;
        try
        {
            intention = JsonSerializer.Deserialize<Intention>(body, Json);
        }
        catch (JsonException ex)
        {
            throw new PaymobException("Paymob returned a response that isn't valid JSON.", ex);
        }

        if (intention is null || string.IsNullOrEmpty(intention.ClientSecret))
            throw new PaymobException("Paymob's response didn't include a client secret.");
        return intention;
    }

    public Uri GetCheckoutUrl(string clientSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        var query = $"publicKey={Uri.EscapeDataString(_options.PublicKey)}&clientSecret={Uri.EscapeDataString(clientSecret)}";
        return new Uri(_options.ResolveBaseUrl(), $"unifiedcheckout/?{query}");
    }

    private static void Validate(IntentionRequest request)
    {
        if (request.AmountCents <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(request));
        if (request.IntegrationIds.Count == 0)
            throw new ArgumentException("At least one integration ID is required.", nameof(request));
        if (request.Items.Count == 0)
            throw new ArgumentException("At least one item is required.", nameof(request));

        var itemsTotal = request.Items.Sum(i => i.AmountCents * i.Quantity);
        if (itemsTotal != request.AmountCents)
            throw new ArgumentException($"Items add up to {itemsTotal} but the amount is {request.AmountCents}. Paymob rejects intentions where they differ.", nameof(request));
    }
}