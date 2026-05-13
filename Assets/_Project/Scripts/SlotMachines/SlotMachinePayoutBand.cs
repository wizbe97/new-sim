using System;
using UnityEngine;

namespace Project.SlotMachines
{
    [Serializable]
    public sealed class SlotMachinePayoutBand
    {
        [Tooltip("Minimum payout multiplier. Use 0 for dead spins.")]
        [SerializeField, Min(0f)] private float minMultiplier = 0f;

        [Tooltip("Maximum payout multiplier. Use 0 for dead spins.")]
        [SerializeField, Min(0f)] private float maxMultiplier = 0f;

        [Tooltip("Relative chance of this band being selected.")]
        [SerializeField, Min(0f)] private float relativeWeight = 1f;

        public float MinMultiplier => minMultiplier;
        public float MaxMultiplier => maxMultiplier;
        public float RelativeWeight => relativeWeight;

        public float AverageMultiplier => (minMultiplier + maxMultiplier) * 0.5f;

        public bool IsDeadSpin => Mathf.Approximately(minMultiplier, 0f) && Mathf.Approximately(maxMultiplier, 0f);

        public int GetPayout(int betAmount, int maximumMultiplier)
        {
            if (betAmount <= 0 || IsDeadSpin)
            {
                return 0;
            }

            float safeMin = Mathf.Min(minMultiplier, maxMultiplier);
            float safeMax = Mathf.Max(minMultiplier, maxMultiplier);

            float multiplier = UnityEngine.Random.Range(safeMin, safeMax);
            multiplier = Mathf.Clamp(multiplier, 0f, maximumMultiplier);

            int payout = Mathf.RoundToInt(betAmount * multiplier);
            int maximumPayout = betAmount * maximumMultiplier;

            return Mathf.Clamp(payout, 0, maximumPayout);
        }
    }
}