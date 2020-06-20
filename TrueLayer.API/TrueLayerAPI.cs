namespace TrueLayer.API
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using TrueLayer.API.Models;

    public class TrueLayerAPI
    {
        private readonly HttpClient _apiClient;
        private static readonly string ApiURL = "https://api.truelayer-sandbox.com";

        public TrueLayerAPI(IHttpClientFactory httpClientFactory)
        {
            _apiClient = httpClientFactory.CreateClient();
        }

        public async Task<string> GetProviderId(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiURL}/data/v1/me");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _apiClient.SendAsync(request);

            var tokenInfo = await HandleResponse<AccessTokenMetadata>(response);

            tokenInfo.Results.Single().Provider.TryGetValue("provider_id", out var providerId);

            return providerId;
        }

        public async Task<ApiResponse<Account>> GetAccounts(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiURL}/data/v1/accounts");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _apiClient.SendAsync(request);

            return await HandleResponse<Account>(response);
        }

        public async Task<ApiResponse<Balance>> GetBalance(string accessToken, string accountId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiURL}/data/v1/accounts/{accountId}/balance");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _apiClient.SendAsync(request);

            return await HandleResponse<Balance>(response);
        }

        public async Task<ApiResponse<Transaction>> GetTransactions(string accessToken, string accountId, DateTime? from = null, DateTime? to = null)
        {
            var dateFilter = from != null && to != null ? $"?from={from}&to={to}" : "";
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiURL}/data/v1/accounts/{accountId}/transactions{dateFilter}");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _apiClient.SendAsync(request);

            return await HandleResponse<Transaction>(response);
        }

        private async Task<ApiResponse<T>> HandleResponse<T>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponse<T>>(responseStream);
                return apiResponse;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiResponse<T>
                {
                    ShouldAttemptRefresh = true
                };
            }
            else
            {
                // TODO - create singleton and pass error object - ErrorResponse.cs
                return null;
            }
        }
    }
}
