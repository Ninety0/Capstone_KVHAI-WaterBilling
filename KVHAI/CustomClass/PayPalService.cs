using KVHAI.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KVHAI.CustomClass
{
    public class PayPalService
    {
        private readonly HttpClient _httpClient;
        private readonly PayPalSettings _settings;

        public PayPalService(HttpClient httpClient, PayPalSettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var authToken = Encoding.ASCII.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/v1/oauth2/token", new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded"));
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var accessToken = JsonSerializer.Deserialize<JsonDocument>(json).RootElement.GetProperty("access_token").GetString();

            return accessToken;
        }

        public async Task<string> CreatePaymentAsync(decimal amount, string returnUrl, string cancelUrl)
        {
            var accessToken = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var paymentData = new
            {
                intent = "sale",
                payer = new { payment_method = "paypal" },
                transactions = new[]
                {
                new
                {
                    amount = new { total = amount.ToString("F2"), currency = "USD" },
                    description = "Transaction description"
                }
            },
                redirect_urls = new
                {
                    return_url = returnUrl,
                    cancel_url = cancelUrl
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(paymentData), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/v1/payments/payment", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var approvalUrl = JsonSerializer.Deserialize<JsonDocument>(json).RootElement
                .GetProperty("links")
                .EnumerateArray()
                .FirstOrDefault(link => link.GetProperty("rel").GetString() == "approval_url")
                .GetProperty("href")
                .GetString();

            return approvalUrl;
        }
    }
}