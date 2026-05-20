namespace Project.Economy
{
    public interface ICollectableCashSource
    {
        string CashSourceName { get; }

        int AvailableCash { get; }

        bool CanCollectCash { get; }

        float XpMultiplier { get; }

        bool TryCollectCash(int requestedAmount, out int collectedAmount);
    }
}