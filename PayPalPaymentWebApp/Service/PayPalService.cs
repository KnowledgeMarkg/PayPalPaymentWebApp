using Newtonsoft.Json;
using PayPalPaymentWebApp.Models;
using System.Net.Http.Headers;

public class PayPalService
{
    private readonly string _clientId;
    private readonly string _secret;
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public PayPalService(string clientId, string secret, string apiUrl)
    {
        _clientId = clientId;
        _secret = secret;
        _apiUrl = apiUrl;
        _httpClient = new HttpClient();
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var authToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_clientId}:{_secret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

        var requestBody = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" }
        };

        try
        {
            var response = await _httpClient.PostAsync($"{_apiUrl}", new FormUrlEncodedContent(requestBody));
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<PayPalTokenResponse>(responseContent);

            return tokenResponse.access_token;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Invalid PayPal credentials. Please verify your client ID and secret.", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obtaining PayPal access token: {ex.Message}");
            throw; // Optionally, rethrow the exception to be handled higher up
        }
    }

    public async Task<PayPalPaymentResponse> CreatePaymentAsync(string accessToken, object paymentRequest)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var content = new StringContent(JsonConvert.SerializeObject(paymentRequest), System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("https://api.paypal.com/v1/payments/payment", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PayPalPaymentResponse>(responseContent);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP error creating PayPal payment: {ex.Message}");
            throw; // Optionally, rethrow the exception to be handled higher up
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General error creating PayPal payment: {ex.Message}");
            throw; // Optionally, rethrow the exception to be handled higher up
        }
    }
}

public class PayPalTokenResponse
{
    public string scope { get; set; }
    public string access_token { get; set; }
    public string token_type { get; set; }
    public int expires_in { get; set; }
    public string app_id { get; set; }
}
