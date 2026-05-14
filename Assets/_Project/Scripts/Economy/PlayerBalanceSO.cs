using UnityEngine;

namespace Project.Economy
{
    [CreateAssetMenu(
        fileName = "PlayerBalance",
        menuName = "Project/Economy/Player Balance")]
    public sealed class PlayerBalanceSO : ScriptableObject
    {
        [Header("Starting Funds")]
        [SerializeField, Min(0)] private int startingCasinoFunds = 500;
        [SerializeField, Min(0)] private int startingReserveFund = 500;
        [SerializeField, Min(0)] private int startingPlayerBalance;

        [Header("Reserve Fund")]
        [Tooltip("Minimum reserve required per casino level. Example: 500 means level 1 requires 500, level 2 requires 1000, level 3 requires 1500.")]
        [SerializeField, Min(0)] private int baseMinimumReserveFundPerCasinoLevel = 500;

        [Tooltip("Percentage of collected profit sent to the reserve fund while the reserve is below its required minimum.")]
        [SerializeField, Range(0f, 1f)] private float reserveTopUpPercentageWhenBelowMinimum = 0.1f;

        public int StartingCasinoFunds => startingCasinoFunds;
        public int StartingReserveFund => startingReserveFund;
        public int StartingPlayerBalance => startingPlayerBalance;
        public int BaseMinimumReserveFundPerCasinoLevel => baseMinimumReserveFundPerCasinoLevel;
        public float ReserveTopUpPercentageWhenBelowMinimum => reserveTopUpPercentageWhenBelowMinimum;

        public int StartingBalance => startingCasinoFunds;

#if UNITY_EDITOR
        private void OnValidate()
        {
            startingCasinoFunds = Mathf.Max(0, startingCasinoFunds);
            startingReserveFund = Mathf.Max(0, startingReserveFund);
            startingPlayerBalance = Mathf.Max(0, startingPlayerBalance);
            baseMinimumReserveFundPerCasinoLevel = Mathf.Max(0, baseMinimumReserveFundPerCasinoLevel);
            reserveTopUpPercentageWhenBelowMinimum = Mathf.Clamp01(reserveTopUpPercentageWhenBelowMinimum);
        }
#endif
    }
}