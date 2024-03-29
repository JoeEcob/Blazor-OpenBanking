﻿namespace Spendy.Data.Loaders
{
    using Spendy.Data.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TrueLayer.API;
    using TrueLayer.API.Models;

    public class TransactionLoader : Loader<TLTransaction, Transaction>
    {
        public TransactionLoader(AuthService authService, TrueLayerAPI trueLayerApi, LiteDBDatastore dataStore)
            : base(authService, trueLayerApi, dataStore)
        {
        }

        protected override DateTime GetLastUpdateTime(Auth auth, string accountId = null)
        {
            return _dataStore.FindOne<Account>(x => x.AccountId == accountId)?.LastTransactionUpdate ?? DateTime.MinValue;
        }

        protected override Transaction[] FetchDatabaseData(Auth auth, string accountId = null)
        {
            return _dataStore.Find<Transaction>(x => x.AccountId == accountId);
        }

        protected override async Task<TLApiResponse<TLTransaction>> FetchApiData(Auth auth, string accountId = null)
        {
            // If we already have transactions then only fetch since that date
            var currentTransactions = FetchDatabaseData(auth, accountId);
            if (currentTransactions?.Length > 0)
            {
                var mostRecentTransactionDate = currentTransactions.Max(x => x.Timestamp);
                return await _trueLayerApi.GetTransactions(auth.AccessToken, accountId, mostRecentTransactionDate, DateTime.UtcNow);
            }

            // Otherwise we fire off a request to get everything
            return await _trueLayerApi.GetTransactions(auth.AccessToken, accountId);
        }

        protected override Transaction[] MapToClasses(Auth auth, TLTransaction[] data, string accountId = null)
        {
            var newTransactions = new List<Transaction>();
            var currentTransactions = FetchDatabaseData(auth, accountId);
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

        protected override void SaveToDatabase(Auth auth, Transaction[] data, string accountId = null)
        {
            // Since we've done the check for existing transactions, we can insert without deleting
            // as these should all be new.
            _dataStore.InsertMany<Transaction>(data);

            // Update the accounts with the new fetch time
            var account = _dataStore.FindOne<Account>(x => x.AccountId == accountId);
            account.LastTransactionUpdate = DateTime.UtcNow;
            _dataStore.Update(account.Id, account);
        }
    }
}
