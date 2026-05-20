using UnityEngine;

namespace Project.Progression
{
    [CreateAssetMenu(
        fileName = "CasinoLevelConfig",
        menuName = "Project/Progression/Casino Level Config")]
    public sealed class CasinoLevelConfigSO : ScriptableObject
    {
        [Header("Level Formula")]
        [Tooltip("XP required to go from level 1 to level 2.")]
        [SerializeField, Min(1)] private int baseXpForLevelTwo = 100;

        [Tooltip("Multiplier applied to each level's XP requirement. Example: 1.5 means each new level takes 50% more XP than the previous level.")]
        [SerializeField, Min(1f)] private float levelXpMultiplier = 1.5f;

        [Header("XP Rewards")]
        [SerializeField, Min(0)] private int itemPlacedXp = 15;
        [SerializeField, Min(0)] private int itemPurchasedXp = 5;
        [SerializeField, Min(0)] private int slotSpinXp = 1;
        [SerializeField, Min(0)] private int slotSessionCompletedXp = 10;
        [SerializeField, Min(0)] private int customerCashedOutXp = 20;
        [SerializeField, Min(0)] private int slotCashCollectedXp = 1;
        [SerializeField, Min(0)] private int tableCashCollectedXp = 1;
        [SerializeField, Min(0)] private int barCashCollectedXp = 1;
        [SerializeField, Min(0)] private int manualDebugXp = 100;

        [Header("Staff XP")]
        [Tooltip("Multiplier applied to XP caused by staff actions. 1 = full XP, 0.5 = half XP, 0 = no XP.")]
        [SerializeField, Range(0f, 1f)] private float staffXpMultiplier = 0.5f;

        [Header("Safety")]
        [Tooltip("Maximum loop iterations used when calculating a level from very high XP values.")]
        [SerializeField, Min(100)] private int maxLevelCalculationIterations = 10000;

        public int BaseXpForLevelTwo => baseXpForLevelTwo;
        public float LevelXpMultiplier => levelXpMultiplier;

        public int ItemPlacedXp => itemPlacedXp;
        public int ItemPurchasedXp => itemPurchasedXp;
        public int SlotSpinXp => slotSpinXp;
        public int SlotSessionCompletedXp => slotSessionCompletedXp;
        public int CustomerCashedOutXp => customerCashedOutXp;
        public int SlotCashCollectedXp => slotCashCollectedXp;
        public int TableCashCollectedXp => tableCashCollectedXp;
        public int BarCashCollectedXp => barCashCollectedXp;
        public int ManualDebugXp => manualDebugXp;

        public float StaffXpMultiplier => staffXpMultiplier;

        public int GetLevelForTotalXp(int totalXp)
        {
            totalXp = Mathf.Max(0, totalXp);

            int level = 1;
            int remainingXp = totalXp;

            for (int i = 0; i < maxLevelCalculationIterations; i++)
            {
                int xpRequiredForNextLevel = GetXpRequiredToAdvanceFromLevel(level);

                if (remainingXp < xpRequiredForNextLevel)
                {
                    return level;
                }

                remainingXp -= xpRequiredForNextLevel;
                level++;
            }

            Debug.LogWarning(
                $"{nameof(CasinoLevelConfigSO)} reached the max level calculation iteration limit. " +
                $"Returned level {level}. Consider increasing {nameof(maxLevelCalculationIterations)} if this is expected.",
                this);

            return level;
        }

        public int GetRequiredTotalXpForLevel(int level)
        {
            level = Mathf.Max(1, level);

            if (level <= 1)
            {
                return 0;
            }

            int requiredTotalXp = 0;

            for (int currentLevel = 1; currentLevel < level; currentLevel++)
            {
                requiredTotalXp = AddSafe(
                    requiredTotalXp,
                    GetXpRequiredToAdvanceFromLevel(currentLevel));
            }

            return requiredTotalXp;
        }

        public bool TryGetRequiredTotalXpForLevel(int level, out int requiredTotalXp)
        {
            level = Mathf.Max(1, level);
            requiredTotalXp = GetRequiredTotalXpForLevel(level);
            return true;
        }

        public bool HasNextLevel(int currentLevel)
        {
            return true;
        }

        public int GetCurrentLevelXp(int totalXp)
        {
            totalXp = Mathf.Max(0, totalXp);

            int currentLevel = GetLevelForTotalXp(totalXp);
            int currentLevelRequiredTotalXp = GetRequiredTotalXpForLevel(currentLevel);

            return Mathf.Max(0, totalXp - currentLevelRequiredTotalXp);
        }

        public int GetXpRequiredForNextLevel(int totalXp)
        {
            totalXp = Mathf.Max(0, totalXp);

            int currentLevel = GetLevelForTotalXp(totalXp);
            return GetXpRequiredToAdvanceFromLevel(currentLevel);
        }

        public int GetRemainingXpForNextLevel(int totalXp)
        {
            totalXp = Mathf.Max(0, totalXp);

            int currentLevel = GetLevelForTotalXp(totalXp);
            int currentLevelRequiredTotalXp = GetRequiredTotalXpForLevel(currentLevel);
            int currentLevelXp = Mathf.Max(0, totalXp - currentLevelRequiredTotalXp);
            int requiredForNextLevel = GetXpRequiredToAdvanceFromLevel(currentLevel);

            return Mathf.Max(0, requiredForNextLevel - currentLevelXp);
        }

        public int GetXpRequiredToAdvanceFromLevel(int level)
        {
            level = Mathf.Max(1, level);

            if (level == 1)
            {
                return baseXpForLevelTwo;
            }

            double requiredXp = baseXpForLevelTwo;

            for (int currentLevel = 1; currentLevel < level; currentLevel++)
            {
                requiredXp *= levelXpMultiplier;

                if (requiredXp >= int.MaxValue)
                {
                    return int.MaxValue;
                }
            }

            return Mathf.Max(1, Mathf.CeilToInt((float)requiredXp));
        }

        public int GetXpReward(CasinoXpSource source)
        {
            switch (source)
            {
                case CasinoXpSource.ItemPlaced:
                    return itemPlacedXp;

                case CasinoXpSource.ItemPurchased:
                    return itemPurchasedXp;

                case CasinoXpSource.SlotSpin:
                    return slotSpinXp;

                case CasinoXpSource.SlotSessionCompleted:
                    return slotSessionCompletedXp;

                case CasinoXpSource.CustomerCashedOut:
                    return customerCashedOutXp;

                case CasinoXpSource.SlotCashCollected:
                    return slotCashCollectedXp;

                case CasinoXpSource.TableCashCollected:
                    return tableCashCollectedXp;

                case CasinoXpSource.BarCashCollected:
                    return barCashCollectedXp;

                case CasinoXpSource.ManualDebug:
                    return manualDebugXp;

                default:
                    Debug.LogWarning(
                        $"{nameof(CasinoLevelConfigSO)} has no XP reward mapping for source '{source}'. Returning 0 XP.",
                        this);

                    return 0;
            }
        }

        private static int AddSafe(int a, int b)
        {
            if (a >= int.MaxValue - b)
            {
                return int.MaxValue;
            }

            return a + b;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            baseXpForLevelTwo = Mathf.Max(1, baseXpForLevelTwo);
            levelXpMultiplier = Mathf.Max(1f, levelXpMultiplier);

            itemPlacedXp = Mathf.Max(0, itemPlacedXp);
            itemPurchasedXp = Mathf.Max(0, itemPurchasedXp);
            slotSpinXp = Mathf.Max(0, slotSpinXp);
            slotSessionCompletedXp = Mathf.Max(0, slotSessionCompletedXp);
            customerCashedOutXp = Mathf.Max(0, customerCashedOutXp);
            slotCashCollectedXp = Mathf.Max(0, slotCashCollectedXp);
            tableCashCollectedXp = Mathf.Max(0, tableCashCollectedXp);
            barCashCollectedXp = Mathf.Max(0, barCashCollectedXp);
            manualDebugXp = Mathf.Max(0, manualDebugXp);

            staffXpMultiplier = Mathf.Clamp01(staffXpMultiplier);

            maxLevelCalculationIterations = Mathf.Max(100, maxLevelCalculationIterations);
        }
#endif
    }
}