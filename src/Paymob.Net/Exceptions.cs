using System.Net;

namespace Paymob.Net;

public class PaymobException : Exception
{
    public PaymobException(string message, Exception? inner = null) : base(message, inner) { }
}

public sealed class PaymobConfigurationException : PaymobException
{
    public PaymobConfigurationException(string message) : base(message) { }
}

public sealed class PaymobApiException : PaymobException
{
    public PaymobApiException(HttpStatusCode statusCode, string responseBody)
        : base($"Paymob returned {(int)statusCode} {statusCode}.")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }

    public string ResponseBody { get; }
}