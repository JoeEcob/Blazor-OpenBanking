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

        public async Task<Provider> RefreshAccessToken(Provider provider)
        {
            var accessToken = await _trueLayerAuth.RefreshTokenAsync(provider.RefreshToken);
            return await SaveAccessToken(accessToken);
        }

        private async Task<Provider> SaveAccessToken(TLAccessToken accessToken)
        {
            var newProvider = new Provider
            {
                Name = await _trueLayerApi.GetProviderName(accessToken.Token),
                AccessToken = accessToken.Token,
                RefreshToken = accessToken.RefreshToken
            };

            var existingRecord = _dataStore.FindOne<Provider>(x => x.Name == newProvider.Name);
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
