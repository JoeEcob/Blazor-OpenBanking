namespace Spendy.Data
{
    using Spendy.Data.Models;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TrueLayer.API;
    using TrueLayer.API.Models;

    public class ProviderService
    {
        private readonly LiteDBDatastore _dataStore;
        private readonly TrueLayerAuth _trueLayerAuth;

        public ProviderService(LiteDBDatastore dataStore, TrueLayerAuth trueLayerAuth)
        {
            _dataStore = dataStore;
            _trueLayerAuth = trueLayerAuth;
        }

        // Lazy load our list of providers. If we haven't populated the DB yet, go and fetch the data.
        public async Task<Provider> GetProvider(string providerId)
        {
            // TODO - Implement provider updating after a period of time.
            var provider = _dataStore.FindOne<Provider>(x => x.ProviderId == providerId);
            if (provider != null)
            {
                return provider;
            }

            // We haven't found it so go and fetch from the API.
            var providers = await _trueLayerAuth.GetProviders();

            // Save to DB
            var convertedProviders = new List<Provider>();
            foreach (var item in providers)
            {
                convertedProviders.Add(new Provider()
                {
                    ProviderId = item.ProviderId,
                    DisplayName = item.DisplayName,
                    Country = item.Country,
                    LogoUrl = item.LogoUrl,
                });
            }
            _dataStore.InsertMany<Provider>(convertedProviders.ToArray());

            return convertedProviders.FirstOrDefault(x => x.ProviderId == providerId);

        }
    }
}
