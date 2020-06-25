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
        public AccountLoader(AuthService authService, TrueLayerAPI trueLayerApi, LiteDBDatastore dataStore) : base(authService, trueLayerApi, dataStore)
        {
        }

        public async Task<Account[]> Load()
        {
            var accounts = new List<Account>();

            var providers = _dataStore.FindAll<Provider>();
            foreach (var provider in providers)
            {
                accounts.AddRange(await Load(provider.Id));
            }

            return accounts.ToArray();
        }

        protected override DateTime GetLastUpdateTime(Provider provider, string accountId = null)
        {
            return _dataStore.FindAll<Account>().Min(x => x.LastUpdated);
        }

        protected override async Task<TLApiResponse<TLAccount>> FetchApiData(Provider provider, string accountId = null)
        {
            return await _trueLayerApi.GetAccounts(provider.AccessToken);
        }

        protected override Account[] FetchDatabaseData(Provider provider, string accountId = null)
        {
            return _dataStore.Find<Account>(x => x.ProviderId == provider.Id);
        }

        protected override async Task<Account[]> MapToClasses(Provider provider, TLAccount[] data, string accountId = null)
        {
            var newAccounts = new List<Account>();

            foreach (var account in data)
            {
                var balance = (await _trueLayerApi.GetBalance(provider.AccessToken, account.AccountId)).Results.First();

                newAccounts.Add(new Account
                {
                    ProviderId = provider.Id,
                    AccountId = account.AccountId,
                    DisplayName = account.DisplayName,
                    LogoUri = account.Provider.LogoUri,
                    AvailableBalance = balance.Available,
                    CurrentBalance = balance.Current,
                    Overdraft = balance.Overdraft,
                    LastUpdated = account.UpdateTimeStamp
                });
            }

            return newAccounts.ToArray();
        }

        protected override void SaveToDatabase(Provider provider, Account[] newAccounts, string accountId = null)
        {
            _dataStore.DeleteMany<Account>(x => x.ProviderId == provider.Id);
            _dataStore.InsertMany<Account>(newAccounts.ToArray());
        }
    }
}
