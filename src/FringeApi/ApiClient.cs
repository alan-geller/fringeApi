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
    public int RetryLimit { get; set; } = 5;
    public int RetryDelayMilliseconds { get; set; } = 1000;
    public TimeSpan Timeout { 
        get { return httpClient.Timeout; } 
        set { httpClient.Timeout = value; } 
    }

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
        for (int i = 0; i < RetryLimit; i++)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                // Handle the case where the charset is "utf8", which .NET doesn't like, and change it to "utf-8"
                if (response.Content.Headers.ContentType?.CharSet == "utf8")
                {
                    response.Content.Headers.ContentType.CharSet = "utf-8";
                }
                var jsonString = await response.Content.ReadAsStringAsync();
                return jsonString;
            }
            catch (HttpRequestException ex)
            {
                switch (ex.HttpRequestError)
                {
                    case HttpRequestError.UserAuthenticationError:
                        // Unrecoverable so rethrow the exception
                        throw;
                    default:
                        // Retry on other errors, after a delay
                        if (RetryDelayMilliseconds > 0)
                        {
                            await Task.Delay(RetryDelayMilliseconds);
                        }
                        continue;
                }
            }
        }
        throw new HttpRequestException($"Request to {url} failed after {RetryLimit} attempts.");
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

    /// <summary>
    /// Retrieves a paged JSON array from the specified endpoint and processes each item into a 
    /// list of results.
    /// </summary>
    /// <typeparam name="T">The type produced by the item-processing callback.</typeparam>
    /// <param name="endpoint">The API endpoint path to request.</param>
    /// <param name="filter">Additional query-string prefix to include before the pagination parameters.</param>
    /// <param name="processUpdates">A callback that transforms the JSON document into a result item.</param>
    /// <returns>A list containing all non-null values returned by <paramref name="processUpdates"/>.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP response indicates a failure status code.</exception>
    /// <exception cref="JsonException">Thrown when the response content is not valid JSON.</exception>
    internal async Task<List<T>> ProcessPagedJsonAsync<T>(string endpoint, string filter, 
        Func<JsonDocument, T?> processUpdates)
    {
        var start = 0;
        var chunkSize = 100;
        List<T> result = new List<T>();
        int thisCount;
        do
        {
            var argsWithPagination = $"{filter}from={start}&size={chunkSize}";
            using (var json = await GetJsonAsync(endpoint, argsWithPagination))
            {
                foreach (var item in json.RootElement.EnumerateArray())
                {
                    var processed = processUpdates(json);
                    if (processed != null)
                    {
                        result.Add(processed);
                    }
                }
                start += chunkSize;
                thisCount = json.RootElement.GetArrayLength();
            }
        } while (thisCount == chunkSize);
        return result;
    }

    /// <summary>
    /// Retrieves a paged JSON array from the specified endpoint and invokes a callback 
    /// for each element.
    /// </summary>
    /// <param name="endpoint">The API endpoint path to request.</param>
    /// <param name="filter">Additional query-string prefix to include before the pagination parameters.</param>
    /// <param name="processUpdates">A callback invoked once for each item in the results.</param>
    /// <returns>The total number of items processed across all pages.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP response indicates a failure status code.</exception>
    /// <exception cref="JsonException">Thrown when the response content is not valid JSON.</exception>
    internal async Task<int> ProcessPagedJsonVoidAsync(string endpoint, string filter, 
        Action<JsonElement> processUpdates)
    {
        var start = 0;
        var chunkSize = 100;
        int thisCount;
        do
        {
            var argsWithPagination = $"{filter}from={start}&size={chunkSize}";
            using (var json = await GetJsonAsync(endpoint, argsWithPagination))
            {
                foreach (var item in json.RootElement.EnumerateArray())
                {
                    processUpdates(item);
                }
                thisCount = json.RootElement.GetArrayLength();
                start += thisCount;
            }
        } while (thisCount == chunkSize);
        return start;
    }
}
