using System.Security.Cryptography;
using System.Text.Json;

namespace FringeApi;

/// <summary>
/// Provides a typed client for calling the Edinburgh Festival City API with a user-scoped API key and HMAC signature.
/// </summary>
/// <remarks>
/// This client builds the required festival URL, appends the signature query parameter, and wraps the HTTP response
/// so callers can retrieve either raw JSON text or a parsed <see cref="JsonDocument"/>.
/// </remarks>
public sealed class ApiClient
{
    private static readonly HttpClient httpClient = new HttpClient();
    private readonly string userId;
    private readonly string apiKey;
    private readonly string festival;

    const string BaseUrl = "https://api.edinburghfestivalcity.com";

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClient"/> class.
    /// </summary>
    /// <param name="userId">The festival API user identifier used in the signed request.</param>
    /// <param name="apiKey">The shared secret used to generate the HMAC signature.</param>
    /// <param name="festival">The festival name to include in the request. The default value is "demofringe".</param>
    public ApiClient(string userId, string apiKey, string festival = "demofringe")
    {
        this.userId = userId;
        this.apiKey = apiKey;
        this.festival = festival;
    }

    /// <summary>
    /// Builds the full API URL for the requested endpoint, including the festival, user key, and HMAC signature.
    /// </summary>
    /// <param name="endpoint">The API endpoint path to call, without the base URL.</param>
    /// <param name="args">Additional query-string arguments to append to the request, excluding the leading ampersand.</param>
    /// <returns>The fully qualified signed API URL.</returns>
    private string BuildUrl(string endpoint, string args)
    {
        string argsForQueryString = string.IsNullOrEmpty(args) ? "" : $"&{args}";
        string unsignedUrl = $"/{endpoint}?festival={festival}{argsForQueryString}&key={userId}";
        string hash = HmacSha1(unsignedUrl, apiKey);
        return $"{BaseUrl}{unsignedUrl}&signature={hash}";
    }

    /// <summary>
    /// Computes an HMAC-SHA1 hash for the given text using the provided key.
    /// </summary>
    /// <param name="text">The text to sign.</param>
    /// <param name="key">The secret key used to create the HMAC hash.</param>
    /// <returns>The lowercase hexadecimal representation of the HMAC-SHA1 hash.</returns>
    private static string HmacSha1(string text, string key)
    {
        using (var hmac = new HMACSHA1(System.Text.Encoding.ASCII.GetBytes(key)))
        {
            byte[] hashBytes =
                hmac.ComputeHash(System.Text.Encoding.ASCII.GetBytes(text));
            return Convert.ToHexStringLower(hashBytes);
        }
    }

    /// <summary>
    /// Sends a GET request to the specified API endpoint and returns the raw response body as a string.
    /// </summary>
    /// <param name="endpoint">The API endpoint path to request.</param>
    /// <param name="args">Additional query-string parameters to append to the request.</param>
    /// <returns>The raw JSON payload returned by the API.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP response indicates a failure status code.</exception>
    private async Task<string> GetDataAsync(string endpoint, string args)
    {
        string url = BuildUrl(endpoint, args);
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        // Handle the case where the charset is "utf8", which .NET doesn't like, and change it to "utf-8"
        if (response.Content.Headers.ContentType?.CharSet == "utf8")
        {
            response.Content.Headers.ContentType.CharSet = "utf-8";
        }
        string jsonString = await response.Content.ReadAsStringAsync();
        return jsonString;
    }

    /// <summary>
    /// Sends a GET request to the specified API endpoint and parses the response body into a <see cref="JsonDocument"/>.
    /// </summary>
    /// <param name="endpoint">The API endpoint path to request.</param>
    /// <param name="args">Additional query-string parameters to append to the request.</param>
    /// <returns>A <see cref="JsonDocument"/> containing the parsed API response.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP response indicates a failure status code.</exception>
    /// <exception cref="JsonException">Thrown when the response content is not valid JSON.</exception>
    internal async Task<JsonDocument> GetJsonAsync(string endpoint, string args)
    {
        string jsonString = await GetDataAsync(endpoint, args);
        return JsonDocument.Parse(jsonString);
    }
}
