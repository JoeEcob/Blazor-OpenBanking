namespace Spendy.Data
{
    using LiteDB;
    using Spendy.Data.Models;
    using System.Linq;
    using System.Threading.Tasks;
    using TrueLayer.API;
    using TrueLayer.API.Models;

    public class BankingService
    {
        private readonly LiteDBDatastore _dataStore;
        private readonly TrueLayerAuth _trueLayerAuth;
        private readonly TrueLayerAPI _trueLayerApi;

        public BankingService(LiteDBDatastore dataStore, TrueLayerAuth trueLayerAuth, TrueLayerAPI trueLayerApi)
        {
            _dataStore = dataStore;
            _trueLayerAuth = trueLayerAuth;
            _trueLayerApi = trueLayerApi;
        }

        public string GetAuthUrl() => _trueLayerAuth.GetAuthUrl();

        public Provider[] GetProviders() => _dataStore.FindAll<Provider>();

        public Provider GetProvider(ObjectId providerId) => _dataStore.FindOne<Provider>(x => x.Id == providerId);

        public async Task GenerateAccessTokenAsync(string code)
        {
            var accessToken = await _trueLayerAuth.GetAccessTokenAsync(code);

            var providerName = await _trueLayerApi.GetProviderName(accessToken.Token);

            SaveAccessToken(providerName, accessToken);
        }

        public async Task<Account[]> GetAccounts(ObjectId providerId)
        {
            var provider = GetProvider(providerId);

            var result = await _trueLayerApi.GetAccounts(provider.AccessToken.Token);

            if (result.ShouldAttemptRefresh)
            {
                // TODO - Do we need refresh elsewhere?
                var accessToken = await _trueLayerAuth.RefreshTokenAsync(provider.AccessToken.RefreshToken);

                // TODO - handle access token not working

                var providerName = await _trueLayerApi.GetProviderName(accessToken.Token);

                SaveAccessToken(providerName, accessToken);

                result = await _trueLayerApi.GetAccounts(accessToken.Token);
            }

            return result.Results;
        }

        public async Task<Balance> GetBalance(ObjectId providerId, string accountId)
        {
            var provider = GetProvider(providerId);

            return (await _trueLayerApi.GetBalance(provider.AccessToken.Token, accountId)).Results.FirstOrDefault();
        }

        public async Task<Transaction[]> GetTransactions(ObjectId providerId, string accountId)
        {
            var provider = GetProvider(providerId);

            return (await _trueLayerApi.GetTransactions(provider.AccessToken.Token, accountId)).Results;
        }

        private void SaveAccessToken(string providerName, AccessToken accessToken)
        {
            var existingRecord = _dataStore.FindOne<Provider>(x => x.Name == providerName);

            if (existingRecord != null)
            {
                _dataStore.Update(existingRecord.Id, new Provider
                {
                    Id = existingRecord.Id,
                    Name = providerName,
                    AccessToken = accessToken
                });
            }
            else
            {
                _dataStore.Insert(new Provider
                {
                    Name = providerName,
                    AccessToken = accessToken
                });
            }
        }
    }
}
