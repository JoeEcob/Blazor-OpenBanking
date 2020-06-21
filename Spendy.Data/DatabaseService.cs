namespace Spendy.Data
{
    using LiteDB;
    using Spendy.Data.Models;
    using System.Linq;

    public class DatabaseService
    {
        private readonly LiteDBDatastore _dataStore;

        public DatabaseService(LiteDBDatastore dataStore)
        {
            _dataStore = dataStore;
        }

        public Account[] GetAccounts() =>
            _dataStore.FindAll<Account>()
            .OrderBy(x => x.ProviderId)
            .ToArray();

        public Transaction[] GetTransactions(string accountId) =>
            _dataStore.Find<Transaction>(x => x.AccountId == accountId)
            .OrderByDescending(x => x.Timestamp)
            .ToArray();
    }
}
