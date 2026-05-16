namespace Project.NPC.Customer
{
    public enum CustomerState
    {
        Uninitialized,
        GoingToEntrance,
        LookingForSlot,
        GoingToSlot,
        PlayingSlot,
        Wandering,
        StandingIdle,
        GoingToCashDesk,
        WaitingAtCashDesk,
        Leaving,
        Finished
    }
}