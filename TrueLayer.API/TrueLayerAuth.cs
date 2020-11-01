namespace TrueLayer.API
{
    using Microsoft.Extensions.Configuration;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using TrueLayer.API.Models;

    public class TrueLayerAuth
    {
        private readonly HttpClient _authClient;
        private readonly string AuthURL;
        private readonly string ClientId;
        private readonly string ClientSecret;
        private readonly bool EnableMock;
        private readonly string RedirectURL;

        public TrueLayerAuth(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _authClient = httpClientFactory.CreateClient();
            AuthURL = config["TrueLayer:AuthUrl"];
            ClientId = config["TrueLayer:ClientId"];
            ClientSecret = config["TrueLayer:ClientSecret"];
            EnableMock = config.GetValue<bool>("TrueLayer:EnableMock");
            RedirectURL = config["TrueLayer:RedirectURL"];
        }

        public string GetAuthUrl()
            => AuthURL + $"?response_type=code&client_id={ClientId}"
            + "&scope=info%20accounts%20balance%20cards%20transactions%20direct_debits%20standing_orders%20offline_access"
            + $"&redirect_uri={RedirectURL}"
            + "&providers=uk-ob-all%20uk-oauth-all"
            + (EnableMock ? "%20uk-cs-mock" : "");

        public async Task<TLAccessToken> GetAccessTokenAsync(string code)
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "code", code },
                { "redirect_uri", RedirectURL }
            });

            var response = await _authClient.PostAsync($"{AuthURL}/connect/token", content);

            return await HandleResponse(response);
        }

        public async Task<TLAccessToken> RefreshTokenAsync(string refreshToken)
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "refresh_token", refreshToken },
            });

            var response = await _authClient.PostAsync($"{AuthURL}/connect/token", content);

            return await HandleResponse(response);
        }

        private async Task<TLAccessToken> HandleResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<TLAccessToken>(responseStream);
            }
            else
            {
                // TODO - log error
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new System.Exception(errorMessage);
            }
        }
    }
}
