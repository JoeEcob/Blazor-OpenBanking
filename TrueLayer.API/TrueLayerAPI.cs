namespace TrueLayer.API
{
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using TrueLayer.API.Models;

    public class TrueLayerAPI
    {
        private readonly HttpClient _apiClient;
        private readonly string ApiURL;

        public TrueLayerAPI(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _apiClient = httpClientFactory.CreateClient();
            ApiURL = config["TrueLayer:ApiUrl"];
        }

        public async Task<string> GetProviderName(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiURL}/data/v1/me");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _apiClient.SendAsync(request);

            var tokenInfo = await HandleResponse<TLAccessTokenMetadata>(response);

            tokenInfo.Results.Single().Provider.TryGetValue("provider_id", out var providerName);

            return providerName;
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
                return apiResponse;
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
                // TODO - create singleton and pass error object - ErrorResponse.cs
                using var responseStream = await response.Content.ReadAsStreamAsync();
                var error = await JsonSerializer.DeserializeAsync<TLError>(responseStream);
                return null;
            }
        }
    }
}
