using System.Security.Cryptography;
using System.Text.Json;

namespace FringeApi;

public sealed class ApiClient
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly string userId;
    private readonly string apiKey;
    private readonly string festival;

    const string BaseUrl = "https://api.edinburghfestivalcity.com";

    public ApiClient(string userId, string apiKey, string festival = "demofringe")
    {
        this.userId = userId;
        this.apiKey = apiKey;
        this.festival = festival;
    }

    private string BuildUrl(string endpoint, string args)
    {
        string argsForQueryString = string.IsNullOrEmpty(args) ? "" : $"&{args}";
        string unsignedUrl = $"/{endpoint}?festival={festival}{argsForQueryString}&key={userId}";
        string hash = HmacSha1(unsignedUrl, apiKey);
        return $"{BaseUrl}{unsignedUrl}&signature={hash}";
    }

    private static string HmacSha1(string text, string key)
    {
        using (var hmac = new HMACSHA1(System.Text.Encoding.ASCII.GetBytes(key)))
        {
            byte[] hashBytes = 
                hmac.ComputeHash(System.Text.Encoding.ASCII.GetBytes(text));
            return Convert.ToHexStringLower(hashBytes);
        }
    }

    public async Task<string> GetDataAsync(string endpoint, string args)
    {
        string url = BuildUrl(endpoint, args);
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        // Handle the case where the charset is "utf8", which .NET doesn't like, and change it to "utf-8"
        if (response.Content.Headers.ContentType?.CharSet == "utf8")
        {
            response.Content.Headers.ContentType.CharSet = "utf-8";
        }
        string jsonString = await response.Content.ReadAsStringAsync();
        return jsonString;
    }

    public async Task<JsonDocument> GetJsonAsync(string endpoint, string args)
    {
        string jsonString = await GetDataAsync(endpoint, args);
        return JsonDocument.Parse(jsonString);
    }
}
