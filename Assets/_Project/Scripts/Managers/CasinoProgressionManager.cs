using System;
using Project.Progression;
using UnityEngine;

namespace Project.Managers
{
    public sealed class CasinoProgressionManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CasinoLevelConfigSO levelConfig;

        [Header("Starting State")]
        [SerializeField, Min(1)] private int startingLevelFallback = 1;
        [SerializeField, Min(0)] private int startingTotalXp;

        [Header("Debug")]
        [SerializeField] private bool logXpChanges;

        private bool isInitialized;
        private int currentLevel;
        private int currentXp;

        public int CurrentLevel => currentLevel;

        public int CurrentXp => currentXp;

        public int CurrentLevelXp
        {
            get
            {
                if (levelConfig == null)
                {
                    return 0;
                }

                return levelConfig.GetCurrentLevelXp(currentXp);
            }
        }

        public int XpRequiredForNextLevel
        {
            get
            {
                if (levelConfig == null)
                {
                    return 0;
                }

                return levelConfig.GetXpRequiredForNextLevel(currentXp);
            }
        }

        public float NormalizedLevelProgress
        {
            get
            {
                int requiredForNextLevel = XpRequiredForNextLevel;

                if (requiredForNextLevel <= 0)
                {
                    return 1f;
                }

                return Mathf.Clamp01((float)CurrentLevelXp / requiredForNextLevel);
            }
        }

        public bool IsInitialized => isInitialized;
        public bool HasLevelConfig => levelConfig != null;

        public event Action<int, int> XpChanged;
        public event Action<int> LevelChanged;
        public event Action<int, CasinoXpSource> XpGained;

        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            currentXp = Mathf.Max(0, startingTotalXp);
            currentLevel = ResolveLevel(currentXp);

            isInitialized = true;

            XpChanged?.Invoke(CurrentLevelXp, XpRequiredForNextLevel);
            LevelChanged?.Invoke(currentLevel);

            if (logXpChanges)
            {
                Debug.Log(
                    $"[{nameof(CasinoProgressionManager)}] Initialized at level {currentLevel} with {currentXp} total XP.",
                    this);
            }
        }

        public void AddConfiguredXp(CasinoXpSource source)
        {
            AddConfiguredXp(source, quantity: 1, multiplier: 1f);
        }

        public void AddConfiguredXp(CasinoXpSource source, int quantity)
        {
            AddConfiguredXp(source, quantity, multiplier: 1f);
        }

        public void AddConfiguredXp(CasinoXpSource source, float multiplier)
        {
            AddConfiguredXp(source, quantity: 1, multiplier);
        }

        public void AddConfiguredXp(CasinoXpSource source, int quantity, float multiplier)
        {
            if (levelConfig == null)
            {
                Debug.LogError(
                    $"{nameof(CasinoProgressionManager)} cannot add configured XP because CasinoLevelConfigSO is missing.",
                    this);

                return;
            }

            quantity = Mathf.Max(0, quantity);
            multiplier = Mathf.Max(0f, multiplier);

            if (quantity <= 0 || multiplier <= 0f)
            {
                return;
            }

            int baseAmount = levelConfig.GetXpReward(source);

            if (baseAmount <= 0)
            {
                return;
            }

            double calculatedAmount = baseAmount * (double)quantity * multiplier;

            if (calculatedAmount >= int.MaxValue)
            {
                AddXp(int.MaxValue, source);
                return;
            }

            int finalAmount = Mathf.CeilToInt((float)calculatedAmount);
            AddXp(finalAmount, source);
        }

        public void AddXp(int amount, CasinoXpSource source)
        {
            if (!isInitialized)
            {
                Debug.LogWarning(
                    $"[{nameof(CasinoProgressionManager)}] Tried to add XP before Initialize was called.",
                    this);
                return;
            }

            if (amount <= 0)
            {
                return;
            }

            int previousLevel = currentLevel;

            currentXp = Mathf.Max(0, currentXp + amount);
            currentLevel = ResolveLevel(currentXp);

            XpGained?.Invoke(amount, source);
            XpChanged?.Invoke(CurrentLevelXp, XpRequiredForNextLevel);

            if (currentLevel != previousLevel)
            {
                LevelChanged?.Invoke(currentLevel);
            }

            if (logXpChanges)
            {
                Debug.Log(
                    $"[{nameof(CasinoProgressionManager)}] Gained {amount} XP from {source}. Total XP: {currentXp}. Level: {currentLevel}.",
                    this);
            }
        }

        public bool CanUseLevel(int requiredLevel)
        {
            requiredLevel = Mathf.Max(1, requiredLevel);
            return currentLevel >= requiredLevel;
        }

        public void SetTotalXpForDebug(int totalXp)
        {
            if (!isInitialized)
            {
                Debug.LogWarning(
                    $"[{nameof(CasinoProgressionManager)}] Tried to set debug XP before Initialize was called.",
                    this);
                return;
            }

            totalXp = Mathf.Max(0, totalXp);

            int previousLevel = currentLevel;

            currentXp = totalXp;
            currentLevel = ResolveLevel(currentXp);

            XpChanged?.Invoke(CurrentLevelXp, XpRequiredForNextLevel);

            if (currentLevel != previousLevel)
            {
                LevelChanged?.Invoke(currentLevel);
            }

            if (logXpChanges)
            {
                Debug.Log(
                    $"[{nameof(CasinoProgressionManager)}] Debug total XP set to {currentXp}. Level: {currentLevel}.",
                    this);
            }
        }

        private int ResolveLevel(int totalXp)
        {
            if (levelConfig == null)
            {
                return Mathf.Max(1, startingLevelFallback);
            }

            return levelConfig.GetLevelForTotalXp(totalXp);
        }
    }
}