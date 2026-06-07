namespace TrueLayer.API
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using TrueLayer.API.Models;

    public class TrueLayerAPI(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<TrueLayerAPI> logger = null)
    {
        private readonly HttpClient _apiClient = httpClientFactory.CreateClient();
        private readonly string ApiURL = config["TrueLayer:ApiUrl"];
        protected readonly ILogger _logger = logger;

        public async Task<TLAccessTokenMetadata> GetTokenMetadata(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiURL}/data/v1/me");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _apiClient.SendAsync(request);

            var tokenInfo = await HandleResponse<TLAccessTokenMetadata>(response);

            return tokenInfo.Results.Single();
        }

        public async Task<TLApiResponse<TLAccount>> GetAccounts(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiURL}/data/v1/accounts");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _apiClient.SendAsync(request);

            return await HandleResponse<TLAccount>(response);
        }

        public async Task<TLApiResponse<TLCard>> GetCards(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiURL}/data/v1/cards");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _apiClient.SendAsync(request);

            return await HandleResponse<TLCard>(response);
        }

        public async Task<TLApiResponse<TLBalance>> GetBalance(string accessToken, string accountId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiURL}/data/v1/accounts/{accountId}/balance");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _apiClient.SendAsync(request);

            return await HandleResponse<TLBalance>(response);
        }

        public async Task<TLApiResponse<TLCardBalance>> GetCardBalance(string accessToken, string accountId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiURL}/data/v1/cards/{accountId}/balance");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _apiClient.SendAsync(request);

            return await HandleResponse<TLCardBalance>(response);
        }

        public async Task<TLApiResponse<TLTransaction>> GetTransactions(string accessToken, string accountId, DateTime? from = null, DateTime? to = null)
        {
            var dateFilter = from != null && to != null ? $"?from={from:s}&to={to:s}" : "";
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiURL}/data/v1/accounts/{accountId}/transactions{dateFilter}");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _apiClient.SendAsync(request);

            return await HandleResponse<TLTransaction>(response);
        }

        public async Task<TLApiResponse<TLTransaction>> GetCardTransactions(string accessToken, string accountId, DateTime? from = null, DateTime? to = null)
        {
            var dateFilter = from != null && to != null ? $"?from={from:s}&to={to:s}" : "";
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiURL}/data/v1/cards/{accountId}/transactions{dateFilter}");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _apiClient.SendAsync(request);

            return await HandleResponse<TLTransaction>(response);
        }

        private async Task<TLApiResponse<T>> HandleResponse<T>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                var apiResponse = await JsonSerializer.DeserializeAsync<TLApiResponse<T>>(responseStream);
                return apiResponse; // TODO - Log this (redact secrets?)
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new TLApiResponse<T>
                {
                    ShouldAttemptRefresh = true
                };
            }
            else
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                var error = await JsonSerializer.DeserializeAsync<TLError>(responseStream);

                if (_logger?.IsEnabled(LogLevel.Error) ?? false)
                    _logger.LogError("Failed to fetch TrueLayer response: {ErrorDescription}", error.ErrorDescription);

                return null;
            }
        }
    }
}
