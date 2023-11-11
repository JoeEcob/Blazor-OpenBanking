namespace Spendy.Data
{
    using Spendy.Data.Models;
    using System.Threading.Tasks;
    using TrueLayer.API;
    using TrueLayer.API.Models;

    public class AuthService
    {
        private readonly LiteDBDatastore _dataStore;
        private readonly TrueLayerAuth _trueLayerAuth;
        private readonly TrueLayerAPI _trueLayerApi;

        public AuthService(LiteDBDatastore dataStore, TrueLayerAuth trueLayerAuth, TrueLayerAPI trueLayerApi)
        {
            _dataStore = dataStore;
            _trueLayerAuth = trueLayerAuth;
            _trueLayerApi = trueLayerApi;
        }

        public string GetAuthUrl() => _trueLayerAuth.GetAuthUrl();

        public async Task GenerateAccessTokenAsync(string code)
        {
            var accessToken = await _trueLayerAuth.GetAccessTokenAsync(code);
            await SaveAccessToken(accessToken);
        }

        public async Task<Auth> RefreshAccessToken(Auth auth)
        {
            var accessToken = await _trueLayerAuth.RefreshTokenAsync(auth.RefreshToken);
            return await SaveAccessToken(accessToken);
        }

        private async Task<Auth> SaveAccessToken(TLAccessToken accessToken)
        {
            // TODO - handle access token failures
            var tokenInfo = await _trueLayerApi.GetTokenMetadata(accessToken.Token);

            var newProvider = new Auth
            {
                ProviderId = tokenInfo.Provider.ProviderId,
                AccessToken = accessToken.Token,
                RefreshToken = accessToken.RefreshToken
            };

            var existingRecord = _dataStore.FindOne<Auth>(x => x.ProviderId == newProvider.ProviderId);
            if (existingRecord != null)
            {
                newProvider.Id = existingRecord.Id;
                _dataStore.Update(existingRecord.Id, newProvider);
            }
            else
            {
                newProvider.Id = _dataStore.Insert(newProvider);
            }

            return newProvider;
        }
    }
}
