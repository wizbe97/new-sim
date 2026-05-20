using Project.Progression;

namespace Project.Economy
{
    public interface ICollectableCashSource
    {
        string CashSourceName { get; }

        int AvailableCash { get; }

        bool CanCollectCash { get; }

        CasinoXpSource CollectionXpSource { get; }

        float XpMultiplier { get; }

        bool TryCollectCash(int requestedAmount, out int collectedAmount);
    }
}