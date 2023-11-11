namespace Spendy.Data.Loaders
{
    using Spendy.Data.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TrueLayer.API;
    using TrueLayer.API.Models;

    public class AccountLoader : Loader<TLAccount, Account>
    {
        private readonly ProviderService _providerService;

        public AccountLoader(AuthService authService, TrueLayerAPI trueLayerApi, LiteDBDatastore dataStore, ProviderService providerService)
            : base(authService, trueLayerApi, dataStore)
        {
            _providerService = providerService;
        }

        public async Task<Account[]> Load()
        {
            var accounts = new List<Account>();

            var auths = _dataStore.FindAll<Auth>();
            foreach (var auth in auths)
            {
                var loadedAccs = await Load(auth.Id);

                // Inject provider metadata
                var providerInfo = await _providerService.GetProvider(auth.ProviderId);
                foreach (var acc in loadedAccs)
                {
                    acc.Provider = providerInfo;
                }

                accounts.AddRange(loadedAccs);
            }

            return accounts.ToArray();
        }

        protected override DateTime GetLastUpdateTime(Auth auth, string accountId = null)
        {
            var allAccounts = _dataStore.Find<Account>(x => x.AuthId == auth.Id);
            return allAccounts?.Length > 0 ? allAccounts.Min(x => x.LastUpdated) : DateTime.MinValue;
        }

        protected override async Task<TLApiResponse<TLAccount>> FetchApiData(Auth auth, string accountId = null)
        {
            var accounts = await _trueLayerApi.GetAccounts(auth.AccessToken);

            if (accounts?.Results?.Length > 0)
            {
                foreach (var account in accounts.Results)
                {
                    account.Balance = (await _trueLayerApi.GetBalance(auth.AccessToken, account.AccountId)).Results.First();
                }
            }

            return accounts;
        }

        protected override Account[] FetchDatabaseData(Auth auth, string accountId = null)
        {
            return _dataStore.Find<Account>(x => x.AuthId == auth.Id);
        }

        protected override Account[] MapToClasses(Auth auth, TLAccount[] data, string accountId = null)
        {
            var newAccounts = new List<Account>();

            foreach (var account in data)
            {
                newAccounts.Add(new Account
                {
                    AuthId = auth.Id,
                    AccountId = account.AccountId,
                    DisplayName = account.DisplayName,
                    AvailableBalance = account.Balance.Available,
                    CurrentBalance = account.Balance.Current,
                    Overdraft = account.Balance.Overdraft,
                    LastUpdated = account.UpdateTimeStamp
                });
            }

            return newAccounts.ToArray();
        }

        protected override void SaveToDatabase(Auth auth, Account[] newAccounts, string accountId = null)
        {
            _dataStore.DeleteMany<Account>(x => x.AuthId == auth.Id);
            _dataStore.InsertMany<Account>(newAccounts.ToArray());
        }
    }
}
