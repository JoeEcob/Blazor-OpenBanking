namespace Spendy.Data.Loaders
{
    using LiteDB;
    using Microsoft.Extensions.Logging;
    using Spendy.Data.Datastore;
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
    public abstract class Loader<T1, T2>(AuthService authService, TrueLayerAPI trueLayerApi, IDatastore dataStore, ILogger logger = null)
    {
        protected readonly AuthService _authService = authService;
        protected readonly TrueLayerAPI _trueLayerApi = trueLayerApi;
        protected readonly IDatastore _dataStore = dataStore;
        protected readonly ILogger _logger = logger;

        public async Task<T2[]> Load(Guid authId, string accountId = null)
        {
            try
            {
                return await LoadInternal(authId, accountId);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error loading {DataTypeName}", typeof(T1).Name);
                return new T2[0];
            }
        }

        private async Task<T2[]> LoadInternal(Guid authId, string accountId)
        {
            var dataTypeName = typeof(T1).Name;
            LogInfo("Loading {DataTypeName} for authId: {AuthId}, accountId: {AccountId}", dataTypeName, authId, accountId);

            var provider = _dataStore.FindOne<Auth>(x => x.Id == authId);
            if (provider == null)
            {
                LogWarning("Auth record with ID {AuthId} not found in database. Returning empty array.", authId);
                return new T2[0];
            }

            // Return DB data if we don't need to update.
            var lastUpdate = GetLastUpdateTime(provider, accountId);
            if (lastUpdate > DateTime.UtcNow.AddHours(-6))
            {
                LogInfo("{DataTypeName} cache is fresh (last update: {LastUpdate}). Returning database data.", dataTypeName, lastUpdate);
                return FetchDatabaseData(provider, accountId);
            }

            LogInfo("{DataTypeName} cache is stale. Fetching from API...", dataTypeName);

            // Else go off and fetch the latest data from the API.
            var response = await FetchApiData(provider, accountId);
            if (response == null)
            {
                LogError(null, "API returned null response for {DataTypeName}. Falling back to database data.", dataTypeName);
                return FetchDatabaseData(provider, accountId) ?? new T2[0];
            }

            if (response.ShouldAttemptRefresh)
            {
                LogInfo("Access token refresh needed for {DataTypeName}. Refreshing token...", dataTypeName);
                provider = await _authService.RefreshAccessToken(provider);
                response = await FetchApiData(provider, accountId);

                if (response == null)
                {
                    LogError(null, "API returned null response after token refresh for {DataTypeName}. Falling back to database data.", dataTypeName);
                    return FetchDatabaseData(provider, accountId) ?? new T2[0];
                }
            }

            if (response.Results == null || response.Results.Length == 0)
            {
                LogInfo("API returned no results for {DataTypeName}.", dataTypeName);
                return new T2[0];
            }

            LogInfo("API returned {ResultCount} results for {DataTypeName}. Mapping to classes...", response.Results.Length, dataTypeName);
            var translatedData = MapToClasses(provider, response.Results, accountId);

            if (translatedData.Length > 0)
            {
                SaveToDatabase(provider, translatedData, accountId);
                LogInfo("Saved {RecordCount} {DataTypeName} records to database.", translatedData.Length, dataTypeName);
            }

            // We need to return DB data because sometimes we only fetch partial data e.g. transactions
            var dbData = FetchDatabaseData(provider, accountId);
            LogInfo("Returning {RecordCount} {DataTypeName} records from database.", dbData?.Length ?? 0, dataTypeName);
            return dbData ?? new T2[0];
        }

        private void LogInfo(string message, params object[] args)
        {
            if (_logger?.IsEnabled(LogLevel.Information) ?? false)
                _logger.LogInformation(message, args);
        }

        private void LogWarning(string message, params object[] args)
        {
            if (_logger?.IsEnabled(LogLevel.Warning) ?? false)
                _logger.LogWarning(message, args);
        }

        private void LogError(Exception ex, string message, params object[] args)
        {
            if (_logger?.IsEnabled(LogLevel.Error) ?? false)
                _logger.LogError(ex, message, args);
        }

        // TODO - clean up optional parameters
        protected abstract DateTime GetLastUpdateTime(Auth auth, string accountId = null);
        protected abstract T2[] FetchDatabaseData(Auth auth, string accountId = null);
        protected abstract Task<TLApiResponse<T1>> FetchApiData(Auth auth, string accountId = null);
        protected abstract T2[] MapToClasses(Auth auth, T1[] data, string accountId = null);
        protected abstract void SaveToDatabase(Auth auth, T2[] data, string accountId = null);
    }
}
