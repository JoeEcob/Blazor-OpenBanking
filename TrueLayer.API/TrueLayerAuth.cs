namespace TrueLayer.API
{
    using Microsoft.Extensions.Configuration;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using TrueLayer.API.Models;

    public class TrueLayerAuth
    {
        private readonly HttpClient _authClient;
        private static readonly string AuthURL = "https://auth.truelayer-sandbox.com";
        private static readonly string RedirectURL = "https://localhost:44353/truelayer/oauth";
        private static readonly bool EnableMock = true;
        private readonly string ClientId;
        private readonly string ClientSecret;

        public TrueLayerAuth(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _authClient = httpClientFactory.CreateClient();
            ClientId = config["TrueLayer:ClientId"];
            ClientSecret = config["TrueLayer:ClientSecret"];
        }

        public string GetAuthUrl()
            => AuthURL + $"?response_type=code&client_id={ClientId}"
            + "&scope=info%20accounts%20balance%20cards%20transactions%20direct_debits%20standing_orders%20offline_access"
            + $"&redirect_uri={RedirectURL}"
            + "&providers=uk-ob-all%20uk-oauth-all"
            + (EnableMock ? "%20uk-cs-mock" : "");

        public async Task<AccessToken> GetAccessTokenAsync(string code)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent("authorization_code"), "grant_type" },
                { new StringContent(ClientId), "client_id" },
                { new StringContent(ClientSecret), "client_secret" },
                { new StringContent(code), "code" },
                { new StringContent(RedirectURL), "redirect_uri" }
            };

            var response = await _authClient.PostAsync($"{AuthURL}/connect/token", content);

            return await HandleResponse(response);
        }

        public async Task<AccessToken> RefreshTokenAsync(string refreshToken)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent("refresh_token"), "grant_type" },
                { new StringContent(ClientId), "client_id" },
                { new StringContent(ClientSecret), "client_secret" },
                { new StringContent(refreshToken), "refresh_token" },
            };

            var response = await _authClient.PostAsync($"{AuthURL}/connect/token", content);

            return await HandleResponse(response);
        }

        private async Task<AccessToken> HandleResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<AccessToken>(responseStream);
            }
            else
            {
                // TODO - log error
                return default;
            }
        }
    }
}
