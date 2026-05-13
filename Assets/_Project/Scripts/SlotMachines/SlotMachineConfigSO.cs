using System.Collections.Generic;
using UnityEngine;

namespace Project.SlotMachines
{
    [CreateAssetMenu(
        fileName = "SlotMachineConfig",
        menuName = "Project/Slot Machines/Slot Machine Config")]
    public sealed class SlotMachineConfigSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string machineName = "Starter Slot Machine";

        [Header("Betting")]
        [SerializeField, Min(1)] private int maximumBet = 2;

        [Tooltip("Maximum win multiplier. Starter machine should be 100.")]
        [SerializeField, Min(1)] private int maximumWinMultiplier = 100;

        [Header("RTP")]
        [Tooltip("Target return-to-player. This is used as a balancing reference. The actual RTP comes from the weighted payout bands.")]
        [SerializeField, Range(0.01f, 0.99f)] private float targetRtp = 0.86f;

        [Header("Behaviour")]
        [Tooltip("Higher values make NPCs more willing to choose this machine.")]
        [SerializeField, Range(0f, 1f)] private float attractionScore = 0.5f;

        [Tooltip("Higher values can later be used to make customers lose patience faster on tighter machines.")]
        [SerializeField, Range(0f, 1f)] private float frustrationPressure = 0.25f;

        [Header("Payout Bands")]
        [SerializeField] private List<SlotMachinePayoutBand> payoutBands = new();

        public string MachineName => machineName;
        public int MaximumBet => maximumBet;
        public int MaximumWinMultiplier => maximumWinMultiplier;
        public float TargetRtp => targetRtp;
        public float AttractionScore => attractionScore;
        public float FrustrationPressure => frustrationPressure;
        public IReadOnlyList<SlotMachinePayoutBand> PayoutBands => payoutBands;

        public float EstimatedRtp => CalculateEstimatedRtp();

        public int GeneratePayout(int betAmount)
        {
            int safeBet = Mathf.Clamp(betAmount, 1, maximumBet);

            SlotMachinePayoutBand selectedBand = GetRandomPayoutBand();

            if (selectedBand == null)
            {
                return 0;
            }

            return selectedBand.GetPayout(safeBet, maximumWinMultiplier);
        }

        private SlotMachinePayoutBand GetRandomPayoutBand()
        {
            if (payoutBands == null || payoutBands.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;

            foreach (SlotMachinePayoutBand band in payoutBands)
            {
                if (band == null)
                {
                    continue;
                }

                totalWeight += Mathf.Max(0f, band.RelativeWeight);
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (SlotMachinePayoutBand band in payoutBands)
            {
                if (band == null)
                {
                    continue;
                }

                cumulative += Mathf.Max(0f, band.RelativeWeight);

                if (roll <= cumulative)
                {
                    return band;
                }
            }

            return payoutBands[^1];
        }

        private float CalculateEstimatedRtp()
        {
            if (payoutBands == null || payoutBands.Count == 0)
            {
                return 0f;
            }

            float totalWeight = 0f;
            float weightedMultiplierTotal = 0f;

            foreach (SlotMachinePayoutBand band in payoutBands)
            {
                if (band == null || band.RelativeWeight <= 0f)
                {
                    continue;
                }

                totalWeight += band.RelativeWeight;
                weightedMultiplierTotal += band.AverageMultiplier * band.RelativeWeight;
            }

            if (totalWeight <= 0f)
            {
                return 0f;
            }

            return weightedMultiplierTotal / totalWeight;
        }
    }
}