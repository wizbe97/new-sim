namespace Project.Progression
{
    public enum CasinoXpSource
    {
        ItemPlaced = 0,
        ItemPurchased = 1,

        SlotSpin = 2,
        SlotSessionCompleted = 3,

        CustomerCashedOut = 4,

        SlotCashCollected = 5,
        TableCashCollected = 6,
        BarCashCollected = 7,

        ManualDebug = 100
    }
}