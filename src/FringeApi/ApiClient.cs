using System.Security.Cryptography;
using System.Text.Json;

namespace FringeApi;

public class ApiClient
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly string _userId;
    private readonly string _apiKey;
    private readonly string _festival;

    const string BaseUrl = "https://api.edinburghfestivalcity.com";

    public ApiClient(string userId, string apiKey, string festival = "demofringe")
    {
        _userId = userId;
        _apiKey = apiKey;
        _festival = festival;
    }

    private string BuildUrl(string endpoint, string args)
    {
        string _args = string.IsNullOrEmpty(args) ? "" : $"&{args}";
        string unsignedUrl = $"/{endpoint}?festival={_festival}{_args}&key={_userId}";
        string hash = HmacSha1(unsignedUrl, _apiKey);
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

    public List<T> DeserializeJson<T>(string jsonString)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        options.Converters.Add(new DateTimeConverter());

        List<T> data = JsonSerializer.Deserialize<List<T>>(jsonString, options) ?? new List<T>();
        return data;
    }
    
    public async Task<List<T>> GetAndDeserializeAsync<T>(string endpoint, string args)
    {
        string jsonString = await GetDataAsync(endpoint, args);
        return DeserializeJson<T>(jsonString);
    }
}
