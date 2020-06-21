namespace Spendy.Data
{
    using LiteDB;
    using Spendy.Data.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TrueLayer.API;
    using TrueLayer.API.Models;

    public class BankingUpdateService
    {
        private readonly LiteDBDatastore _dataStore;
        private readonly TrueLayerAuth _trueLayerAuth;
        private readonly TrueLayerAPI _trueLayerApi;

        public BankingUpdateService(LiteDBDatastore dataStore, TrueLayerAuth trueLayerAuth, TrueLayerAPI trueLayerApi)
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

        public async Task FetchLatestAccountInfo()
        {
            var providers = _dataStore.FindAll<Provider>();
            foreach (var provider in providers)
            {
                await UpdateAccounts(provider);
            }
        }

        private async Task UpdateAccounts(Provider provider)
        {
            var newAccountInfo = await _trueLayerApi.GetAccounts(provider.AccessToken);

            // TODO - cleanup refresh
            if (newAccountInfo.ShouldAttemptRefresh)
            {
                var newProvider = await RefreshAccessToken(provider);
                newAccountInfo = await _trueLayerApi.GetAccounts(newProvider.AccessToken);
            }

            var newAccounts = new List<Account>();

            foreach (var account in newAccountInfo.Results)
            {
                var balance = (await _trueLayerApi.GetBalance(provider.AccessToken, account.AccountId)).Results.First();

                newAccounts.Add(new Account
                {
                    ProviderId = provider.Id,
                    AccountId = account.AccountId,
                    DisplayName = account.DisplayName,
                    AvailableBalance = balance.Available,
                    CurrentBalance = balance.Current,
                    Overdraft = balance.Overdraft
                });
            }

            _dataStore.DeleteMany<Account>(x => x.ProviderId == provider.Id);
            _dataStore.InsertMany<Account>(newAccounts.ToArray());
        }

        public async Task UpdateTransactions(ObjectId providerId, string accountId)
        {
            var provider = _dataStore.FindOne<Provider>(x => x.Id == providerId);

            var transactionsToMap = new List<TLTransaction>();

            var currentTransctions = _dataStore.Find<Transaction>(x => x.AccountId == accountId);
            if (currentTransctions == null || currentTransctions.Length == 0)
            {
                var allTransactions = await _trueLayerApi.GetTransactions(provider.AccessToken, accountId);
                // TODO - cleanup refresh
                if (allTransactions.ShouldAttemptRefresh)
                {
                    var newProvider = await RefreshAccessToken(provider);
                    allTransactions = await _trueLayerApi.GetTransactions(newProvider.AccessToken, accountId);
                }
                transactionsToMap.AddRange(allTransactions.Results);
            }
            else
            {
                var latestTransaction = currentTransctions.OrderByDescending(x => x.Timestamp).First();
                var recentTransactions = await _trueLayerApi.GetTransactions(provider.AccessToken, accountId, latestTransaction.Timestamp, DateTime.Now);
                // TODO - cleanup refresh
                if (recentTransactions.ShouldAttemptRefresh)
                {
                    var newProvider = await RefreshAccessToken(provider);
                    recentTransactions = await _trueLayerApi.GetTransactions(newProvider.AccessToken, accountId, latestTransaction.Timestamp, DateTime.Now); ;
                }
                transactionsToMap.AddRange(recentTransactions.Results);
            }

            // Begin mapping
            var newTransactions = transactionsToMap.Select(x => new Transaction
            {
                AccountId = accountId,
                TransactionId = x.TransactionId,
                Timestamp = x.Timestamp,
                Description = x.Description,
                Amount = x.Amount
            }).ToArray();

            // Since we've done the check for existing transactions, we can insert without deleting
            // as these should all be new.
            _dataStore.InsertMany<Transaction>(newTransactions);
        }

        private async Task<Provider> RefreshAccessToken(Provider provider)
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
