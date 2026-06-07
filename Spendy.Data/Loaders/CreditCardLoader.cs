namespace Spendy.Data.Loaders
{
    using Microsoft.Extensions.Logging;
    using Spendy.Data.Datastore;
    using Spendy.Data.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TrueLayer.API;
    using TrueLayer.API.Models;

    public class CreditCardLoader(AuthService authService, TrueLayerAPI trueLayerApi, IDatastore dataStore, ProviderService providerService, ILogger<CreditCardLoader> logger)
        : Loader<TLCard, Card>(authService, trueLayerApi, dataStore, logger)
    {
        private readonly ProviderService _providerService = providerService;

        public async Task<Card[]> Load()
        {
            var creditCards = new List<Card>();

            var auths = _dataStore.FindAll<Auth>();
            foreach (var auth in auths)
            {
                var loadedCards = await Load(auth.Id);

                // Inject provider metadata
                var providerInfo = await _providerService.GetProvider(auth.ProviderId);
                foreach (var card in loadedCards)
                {
                    card.Provider = providerInfo;
                }

                creditCards.AddRange(loadedCards);
            }

            return [.. creditCards.OrderBy(a => a.DisplayOrder ?? int.MaxValue)];
        }

        protected override DateTime GetLastUpdateTime(Auth auth, string accountId = null)
        {
            var allCards = _dataStore.Find<Card>(x => x.AuthId == auth.Id);
            return allCards?.Length > 0 ? allCards.Min(x => x.LastUpdated) : DateTime.MinValue;
        }

        protected override async Task<TLApiResponse<TLCard>> FetchApiData(Auth auth, string accountId = null)
        {
            var cards = await _trueLayerApi.GetCards(auth.AccessToken);

            if (cards?.Results?.Length > 0)
            {
                foreach (var card in cards.Results)
                {
                    card.Balance = (await _trueLayerApi.GetCardBalance(auth.AccessToken, card.AccountId)).Results.First();
                }
            }

            return cards;
        }

        protected override Card[] FetchDatabaseData(Auth auth, string accountId = null)
        {
            return _dataStore.Find<Card>(x => x.AuthId == auth.Id);
        }

        protected override Card[] MapToClasses(Auth auth, TLCard[] data, string accountId = null)
        {
            var existing = FetchDatabaseData(auth);

            return [.. data.Select(card => new Card
            {
                AuthId = auth.Id,
                AccountId = card.AccountId,
                DisplayName = card.DisplayName,
                PartialCardNumber = card.PartialCardNumber,
                CardType = card.CardType,
                CardNetwork = card.CardNetwork,
                AvailableBalance = card.Balance.Available,
                CurrentBalance = card.Balance.Current,
                CreditLimit = card.Balance.CreditLimit,
                LastStatementBalance = card.Balance.LastStatementBalance,
                LastStatementDate = card.Balance.LastStatementDate,
                PaymentDue = card.Balance.PaymentDue,
                PaymentDueDate = card.Balance.PaymentDueDate,
                LastUpdated = card.UpdateTimestamp,
                CustomDisplayName = existing
                    .FirstOrDefault(e => e.AccountId == card.AccountId)
                    ?.CustomDisplayName,
                DisplayOrder = existing
                    .FirstOrDefault(e => e.AccountId == card.AccountId)
                    ?.DisplayOrder,
            })];
        }

        protected override void SaveToDatabase(Auth auth, Card[] newCards, string accountId = null)
        {
            _dataStore.DeleteMany<Card>(x => x.AuthId == auth.Id);
            _dataStore.InsertMany<Card>([.. newCards]);
        }
    }
}
