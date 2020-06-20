namespace Spendy.Data
{
    using LiteDB;
    using Spendy.Data.Models;
    using System.Collections;
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

        public Provider GetProvider(string providerId) => _dataStore.FindOne<Provider>(x => x.ProviderId == providerId);

        public async Task GenerateAccessTokenAsync(string code)
        {
            var accessToken = await _trueLayerAuth.GetAccessTokenAsync(code);

            var providerId = await _trueLayerApi.GetProviderId(accessToken.Token);

            _dataStore.UpdateInCollection(providerId, new Provider
            {
                ProviderId = providerId,
                AccessToken = accessToken
            });
        }

        public async Task<Account[]> GetAccounts(string providerId)
        {
            var provider = GetProvider(providerId);

            var result = await _trueLayerApi.GetAccounts(provider.AccessToken.Token);

            if (result.ShouldAttemptRefresh)
            {
                // TODO - remove duplication + do we need refresh elsewhere?
                var accessToken = await _trueLayerAuth.RefreshTokenAsync(provider.AccessToken.RefreshToken);

                var updatedProviderId = await _trueLayerApi.GetProviderId(accessToken.Token);

                _dataStore.UpdateInCollection(updatedProviderId, new Provider
                {
                    ProviderId = updatedProviderId,
                    AccessToken = accessToken
                });

                result = await _trueLayerApi.GetAccounts(accessToken.Token);
            }

            return result.Results;
        }

        public async Task<Balance> GetBalance(string providerId, string accountId)
        {
            var provider = GetProvider(providerId);

            return (await _trueLayerApi.GetBalance(provider.AccessToken.Token, accountId)).Results.FirstOrDefault();
        }

        public async Task<Transaction[]> GetTransactions(string providerId, string accountId)
        {
            var provider = GetProvider(providerId);

            return (await _trueLayerApi.GetTransactions(provider.AccessToken.Token, accountId)).Results;
        }
    }
}
