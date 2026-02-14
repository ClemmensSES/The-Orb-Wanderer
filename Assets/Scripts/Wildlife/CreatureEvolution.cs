using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;

namespace OrbWanderer.Wildlife
{
    /// <summary>
    /// Creature evolution and leveling system.
    /// Creatures gain XP by following the player, fetching orbs, smashing obstacles.
    /// At certain XP thresholds they evolve into bigger/better forms.
    /// </summary>
    [System.Serializable]
    public class CreatureEvolution
    {
        [Header("Level")]
        public int currentLevel = 1;
        public int maxLevel = 10;
        public float currentXP;
        public float xpToNextLevel = 100f;

        [Header("Evolution")]
        public int evolutionStage = 0;      // 0=Baby, 1=Juvenile, 2=Adult, 3=Elder, 4=Mythic
        public int maxEvolutionStage = 4;
        public string[] stageNames = { "Baby", "Juvenile", "Adult", "Elder", "Mythic" };
        public int[] evolveAtLevel = { 1, 3, 5, 8, 10 };

        [Header("Stats")]
        public float speedBonus;
        public float orbDetectionBonus;
        public float orbFetchRange;
        public float specialAbilityPower;

        public System.Action<int> OnLevelUp;
        public System.Action<int> OnEvolve;

        public string StageName => stageNames[Mathf.Clamp(evolutionStage, 0, stageNames.Length - 1)];

        public void AddXP(float amount)
        {
            if (currentLevel >= maxLevel) return;

            currentXP += amount;

            while (currentXP >= xpToNextLevel && currentLevel < maxLevel)
            {
                currentXP -= xpToNextLevel;
                currentLevel++;
                xpToNextLevel = GetXPForLevel(currentLevel);

                // Stat increases per level
                speedBonus += 0.15f;
                orbDetectionBonus += 0.5f;
                orbFetchRange += 0.3f;
                specialAbilityPower += 0.1f;

                OnLevelUp?.Invoke(currentLevel);

                // Check for evolution
                CheckEvolution();
            }
        }

        private float GetXPForLevel(int level)
        {
            return 100f * Mathf.Pow(1.3f, level - 1);
        }

        private void CheckEvolution()
        {
            for (int stage = maxEvolutionStage; stage >= 0; stage--)
            {
                if (currentLevel >= evolveAtLevel[stage] && evolutionStage < stage)
                {
                    evolutionStage = stage;
                    OnEvolve?.Invoke(stage);
                    break;
                }
            }
        }

        public float GetSizeMultiplier()
        {
            return 1f + evolutionStage * 0.25f;
        }

        public float GetSpeedMultiplier()
        {
            return 1f + speedBonus;
        }

        public float GetOrbDetectionRange()
        {
            return 3f + orbDetectionBonus;
        }

        public float GetOrbFetchRange()
        {
            return orbFetchRange;
        }
    }

    /// <summary>
    /// XP source types — creatures earn XP from different activities.
    /// </summary>
    public static class CreatureXPRewards
    {
        public const float FollowingPlayer = 1f;         // Per second while following
        public const float FetchedOrb = 25f;              // Each orb fetched
        public const float FetchedRareOrb = 50f;          // Rare or better orb
        public const float SmashedObstacle = 35f;         // Broke through obstacle
        public const float BuiltBridge = 40f;             // Spider bridge building
        public const float ReachedHighOrb = 30f;          // Jellyfish floating up
        public const float CrossedOcean = 20f;            // Per ocean crossing
        public const float DiscoveredNewArea = 60f;       // First time entering region
        public const float FedOrb = 15f;                  // Player feeding orb
    }
}
