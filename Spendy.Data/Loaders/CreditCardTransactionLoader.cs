namespace Spendy.Data.Loaders
{
    using Spendy.Data.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TrueLayer.API;
    using TrueLayer.API.Models;

    public class CreditCardTransactionLoader : Loader<TLTransaction, Transaction>
    {
        public CreditCardTransactionLoader(AuthService authService, TrueLayerAPI trueLayerApi, LiteDBDatastore dataStore)
            : base(authService, trueLayerApi, dataStore)
        {
        }

        protected override DateTime GetLastUpdateTime(Provider provider, string accountId = null)
        {
            return _dataStore.FindOne<Card>(x => x.AccountId == accountId).LastTransactionUpdate;
        }

        protected override Transaction[] FetchDatabaseData(Provider provider, string accountId = null)
        {
            return _dataStore.Find<Transaction>(x => x.AccountId == accountId);
        }

        protected override async Task<TLApiResponse<TLTransaction>> FetchApiData(Provider provider, string accountId = null)
        {
            // If we already have transactions then only fetch since that date
            var currentTransactions = FetchDatabaseData(provider, accountId);
            if (currentTransactions?.Length > 0)
            {
                var mostRecentTransactionDate = currentTransactions.Max(x => x.Timestamp);
                return await _trueLayerApi.GetCardTransactions(provider.AccessToken, accountId, mostRecentTransactionDate, DateTime.UtcNow);
            }

            // Otherwise we fire off a request to get everything
            return await _trueLayerApi.GetCardTransactions(provider.AccessToken, accountId);
        }

        protected override Transaction[] MapToClasses(Provider provider, TLTransaction[] data, string accountId = null)
        {
            var newTransactions = new List<Transaction>();
            var currentTransactions = FetchDatabaseData(provider, accountId);
            foreach (var transaction in data)
            {
                if (currentTransactions.Any(x => x.TransactionId == transaction.TransactionId))
                {
                    continue;
                }

                newTransactions.Add(new Transaction
                {
                    AccountId = accountId,
                    TransactionId = transaction.TransactionId,
                    Timestamp = transaction.Timestamp,
                    Description = transaction.Description,
                    Amount = transaction.Amount
                });
            }

            return newTransactions.ToArray();
        }

        protected override void SaveToDatabase(Provider provider, Transaction[] data, string accountId = null)
        {
            // Since we've done the check for existing transactions, we can insert without deleting
            // as these should all be new.
            _dataStore.InsertMany<Transaction>(data);

            // Update the accounts with the new fetch time
            var account = _dataStore.FindOne<Card>(x => x.AccountId == accountId);
            account.LastTransactionUpdate = DateTime.UtcNow;
            _dataStore.Update(account.Id, account);
        }
    }
}
