namespace Spendy.Data.Loaders
{
    using LiteDB;
    using Spendy.Data.Models;
    using System;
    using System.Threading.Tasks;
    using TrueLayer.API;
    using TrueLayer.API.Models;

    /// <summary>
    /// Template class for fetching API data and saving to DB (when out of date).
    /// </summary>
    /// <typeparam name="T1">The data type we get from the source API.</typeparam>
    /// <typeparam name="T2">The final data type we want to return.</typeparam>
    public abstract class Loader<T1, T2>
    {
        protected readonly AuthService _authService;
        protected readonly TrueLayerAPI _trueLayerApi;
        protected readonly LiteDBDatastore _dataStore;

        public Loader(AuthService authService, TrueLayerAPI trueLayerApi, LiteDBDatastore dataStore)
        {
            _authService = authService;
            _trueLayerApi = trueLayerApi;
            _dataStore = dataStore;
        }

        public async Task<T2[]> Load(ObjectId authId, string accountId = null)
        {
            var provider = _dataStore.FindOne<Auth>(x => x.Id == authId);

            // Return DB data if we don't need to update.
            var lastUpdate = GetLastUpdateTime(provider, accountId);
            if (lastUpdate > DateTime.UtcNow.AddHours(-6))
            {
                return FetchDatabaseData(provider, accountId);
            }

            // Else go off and fetch the latest data from the API.
            TLApiResponse<T1> response = await FetchApiData(provider, accountId);

            if (response.ShouldAttemptRefresh)
            {
                provider = await _authService.RefreshAccessToken(provider);
                response = await FetchApiData(provider, accountId);
            }

            var translatedData = MapToClasses(provider, response.Results, accountId);

            SaveToDatabase(provider, translatedData, accountId);

            // We need to return DB data because sometimes we only fetch partial data e.g. transactions
            return FetchDatabaseData(provider, accountId);
        }

        // TODO - clean up optional parameters
        protected abstract DateTime GetLastUpdateTime(Auth auth, string accountId = null);

        protected abstract T2[] FetchDatabaseData(Auth auth, string accountId = null);

        protected abstract Task<TLApiResponse<T1>> FetchApiData(Auth auth, string accountId = null);

        protected abstract T2[] MapToClasses(Auth auth, T1[] data, string accountId = null);

        protected abstract void SaveToDatabase(Auth auth, T2[] data, string accountId = null);
    }
}
