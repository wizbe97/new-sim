namespace Project.SlotMachines
{
    public readonly struct SlotSpinResult
    {
        public static SlotSpinResult Empty => new SlotSpinResult(0, 0, 0);

        public readonly int BetAmount;
        public readonly int PayoutAmount;
        public readonly int RemainingCredit;

        public bool HasBet => BetAmount > 0;
        public bool IsWin => PayoutAmount > BetAmount;
        public bool IsLoss => PayoutAmount == 0;

        public SlotSpinResult(int betAmount, int payoutAmount, int remainingCredit)
        {
            BetAmount = betAmount;
            PayoutAmount = payoutAmount;
            RemainingCredit = remainingCredit;
        }
    }
}