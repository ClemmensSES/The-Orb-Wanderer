using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;

namespace OrbWanderer.Data.Defaults
{
    /// <summary>
    /// Helper to create default game data assets in the Unity Editor.
    /// Use the Unity menu: OrbWanderer > Create Default Data to generate
    /// all starting ScriptableObject assets.
    /// </summary>
    #if UNITY_EDITOR
    public static class DefaultGameDataCreator
    {
        [UnityEditor.MenuItem("OrbWanderer/Create Default Data")]
        public static void CreateAllDefaultData()
        {
            CreateDefaultOrbs();
            CreateDefaultRegions();
            CreateDefaultWildlife();
            CreateDefaultEquipment();
            Debug.Log("Default game data created! Check Assets/Resources/GameData/");
        }

        private static void CreateDefaultOrbs()
        {
            string path = "Assets/Resources/GameData/Orbs/";
            EnsureFolderExists(path);

            CreateOrb(path, "Green Wisp", OrbType.Nature, OrbRarity.Common,
                new[] { RegionType.Grassland, RegionType.Forest }, 0.6f,
                new Color(0.2f, 0.8f, 0.3f), "A gentle green orb found among grasses and trees.");

            CreateOrb(path, "Earth Shard", OrbType.Terra, OrbRarity.Common,
                new[] { RegionType.Grassland, RegionType.Mountain, RegionType.Cave }, 0.5f,
                new Color(0.6f, 0.4f, 0.2f), "A sturdy brown orb pulled from the earth.");

            CreateOrb(path, "Tide Pearl", OrbType.Aqua, OrbRarity.Uncommon,
                new[] { RegionType.Ocean, RegionType.Swamp }, 0.4f,
                new Color(0.2f, 0.5f, 0.9f), "A shimmering blue orb found near water.");

            CreateOrb(path, "Cinder Core", OrbType.Ember, OrbRarity.Uncommon,
                new[] { RegionType.Volcano, RegionType.Desert }, 0.35f,
                new Color(0.9f, 0.3f, 0.1f), "A warm orb pulsing with inner fire.");

            CreateOrb(path, "Breeze Wisp", OrbType.Zephyr, OrbRarity.Uncommon,
                new[] { RegionType.Mountain, RegionType.SkyIslands }, 0.35f,
                new Color(0.7f, 0.9f, 1f), "A weightless orb that drifts on the wind.");

            CreateOrb(path, "Frost Crystal", OrbType.Frost, OrbRarity.Rare,
                new[] { RegionType.Tundra, RegionType.Mountain }, 0.25f,
                new Color(0.6f, 0.8f, 1f), "A cold orb that never melts.");

            CreateOrb(path, "Void Mote", OrbType.Shadow, OrbRarity.Rare,
                new[] { RegionType.Cave, RegionType.Swamp }, 0.2f,
                new Color(0.3f, 0.1f, 0.4f), "A dark orb that absorbs light around it.");

            CreateOrb(path, "Sun Fragment", OrbType.Radiant, OrbRarity.Rare,
                new[] { RegionType.Desert, RegionType.SkyIslands }, 0.2f,
                new Color(1f, 0.95f, 0.5f), "A brilliant orb of captured sunlight.");

            CreateOrb(path, "Thunder Essence", OrbType.Storm, OrbRarity.Epic,
                new[] { RegionType.Mountain, RegionType.Ocean }, 0.1f,
                new Color(0.5f, 0.3f, 0.8f), "A crackling orb born from lightning strikes.");

            CreateOrb(path, "Prismatic Heart", OrbType.Prismatic, OrbRarity.Legendary,
                new[] { RegionType.SkyIslands }, 0.05f,
                Color.white, "The rarest orb, containing all elements in harmony.");
        }

        private static void CreateDefaultRegions()
        {
            string path = "Assets/Resources/GameData/Regions/";
            EnsureFolderExists(path);

            // Starting region - no unlock cost
            var grassland = CreateRegion(path, "Verdant Meadows", RegionType.Grassland,
                new Vector2Int(5, 5), Color.green,
                "The peaceful green land where your journey begins. Rich in Nature and Terra orbs.");

            CreateRegion(path, "Whispering Woods", RegionType.Forest,
                new Vector2Int(4, 6), new Color(0.1f, 0.5f, 0.2f),
                "A dense forest teeming with wildlife. Nature orbs are plentiful here.");

            CreateRegion(path, "Stonepeak Mountains", RegionType.Mountain,
                new Vector2Int(6, 7), new Color(0.5f, 0.5f, 0.5f),
                "Towering peaks where Zephyr and Terra orbs swirl among the heights.");

            CreateRegion(path, "Crystal Caverns", RegionType.Cave,
                new Vector2Int(7, 5), new Color(0.3f, 0.2f, 0.4f),
                "Deep underground caves filled with Shadow orbs and strange creatures.");

            CreateRegion(path, "Azure Coast", RegionType.Ocean,
                new Vector2Int(3, 4), new Color(0.1f, 0.4f, 0.8f),
                "Vast ocean waters. Ride aquatic wildlife to explore and find Aqua orbs.");

            CreateRegion(path, "Ember Wastes", RegionType.Desert,
                new Vector2Int(8, 4), new Color(0.9f, 0.7f, 0.3f),
                "A scorching desert where Ember and Radiant orbs shimmer in the heat.");

            CreateRegion(path, "Frozen Reach", RegionType.Tundra,
                new Vector2Int(6, 9), new Color(0.8f, 0.9f, 1f),
                "An icy tundra at the world's edge. Frost orbs crystallize in the cold air.");

            CreateRegion(path, "Magma Depths", RegionType.Volcano,
                new Vector2Int(9, 3), new Color(0.7f, 0.2f, 0.1f),
                "An active volcanic region. Ember orbs are abundant but danger is high.");

            CreateRegion(path, "Murkmire Swamp", RegionType.Swamp,
                new Vector2Int(3, 6), new Color(0.3f, 0.4f, 0.2f),
                "A foggy marshland where Shadow and Aqua orbs lurk in the mist.");

            CreateRegion(path, "Celestial Isles", RegionType.SkyIslands,
                new Vector2Int(5, 10), new Color(0.9f, 0.8f, 1f),
                "Floating islands in the sky. The only place to find Prismatic Hearts.");
        }

        private static void CreateDefaultWildlife()
        {
            string path = "Assets/Resources/GameData/Wildlife/";
            EnsureFolderExists(path);

            // Easy starters
            CreateWildlife(path, "Meadow Hopper", WildlifeType.Land, WildlifeBehavior.Passive,
                WildlifeSize.Small, 1, true, 3f, false, false, false,
                new[] { RegionType.Grassland }, 1.2f, 0.05f,
                "A friendly rabbit-like creature. Easy to befriend, helps find Nature orbs.");

            CreateWildlife(path, "Bark Beetle", WildlifeType.Land, WildlifeBehavior.Passive,
                WildlifeSize.Tiny, 1, false, 0f, false, false, false,
                new[] { RegionType.Forest }, 1.1f, 0.03f,
                "A small insect companion. Points toward nearby orbs with its antennae.");

            // Medium difficulty
            CreateWildlife(path, "Stone Elk", WildlifeType.Land, WildlifeBehavior.Timid,
                WildlifeSize.Medium, 4, true, 6f, false, false, true,
                new[] { RegionType.Mountain, RegionType.Grassland }, 1.5f, 0.1f,
                "A majestic elk with stone-like antlers. Can climb steep terrain when mounted.");

            CreateWildlife(path, "River Otter", WildlifeType.Aquatic, WildlifeBehavior.Timid,
                WildlifeSize.Small, 3, true, 4f, true, false, false,
                new[] { RegionType.Ocean, RegionType.Swamp }, 1.3f, 0.08f,
                "A playful aquatic companion. Slow in water but great at finding Aqua orbs.");

            CreateWildlife(path, "Dune Lizard", WildlifeType.Land, WildlifeBehavior.Neutral,
                WildlifeSize.Medium, 5, true, 7f, false, false, false,
                new[] { RegionType.Desert }, 1.4f, 0.12f,
                "A heat-resistant lizard. Fast across sand and skilled at finding Ember orbs.");

            // Hard to obtain
            CreateWildlife(path, "Leviathan Turtle", WildlifeType.Aquatic, WildlifeBehavior.Neutral,
                WildlifeSize.Large, 7, true, 8f, true, false, false,
                new[] { RegionType.Ocean }, 2.0f, 0.2f,
                "A massive sea turtle. Provides excellent ocean traversal and orb detection.");

            CreateWildlife(path, "Cave Wyrm", WildlifeType.Subterranean, WildlifeBehavior.Aggressive,
                WildlifeSize.Large, 8, true, 9f, false, false, true,
                new[] { RegionType.Cave, RegionType.Volcano }, 2.5f, 0.25f,
                "A fearsome underground dragon. Exceptional at finding Shadow and Ember orbs.");

            CreateWildlife(path, "Storm Hawk", WildlifeType.Aerial, WildlifeBehavior.Aggressive,
                WildlifeSize.Medium, 8, true, 12f, false, true, false,
                new[] { RegionType.Mountain, RegionType.SkyIslands }, 3.0f, 0.3f,
                "A lightning-quick raptor. The fastest mount and best at finding Storm orbs.");

            // Legendary
            CreateWildlife(path, "Celestial Stag", WildlifeType.Land, WildlifeBehavior.Timid,
                WildlifeSize.Large, 10, true, 15f, true, true, true,
                new[] { RegionType.SkyIslands }, 4.0f, 0.4f,
                "A mythical stag that walks on air. Can traverse any terrain and finds all orb types.");
        }

        private static void CreateDefaultEquipment()
        {
            string path = "Assets/Resources/GameData/Equipment/";
            EnsureFolderExists(path);

            // Saddles - determine what size wildlife you can ride
            CreateEquipment(path, "Rope Saddle", EquipmentType.Saddle, EquipmentTier.Basic,
                WildlifeSize.Small, 1f, 0f, 0, "A basic rope saddle. Allows riding small wildlife.");

            CreateEquipment(path, "Leather Saddle", EquipmentType.Saddle, EquipmentTier.Improved,
                WildlifeSize.Medium, 1.1f, 0f, 0, "A sturdy leather saddle for medium-sized mounts.");

            CreateEquipment(path, "Reinforced Saddle", EquipmentType.Saddle, EquipmentTier.Advanced,
                WildlifeSize.Large, 1.2f, 0f, 0, "A reinforced saddle built for large wildlife.");

            CreateEquipment(path, "Master Saddle", EquipmentType.Saddle, EquipmentTier.Master,
                WildlifeSize.Massive, 1.3f, 0f, 0, "The finest saddle. Ride any wildlife in the world.");

            // Satchel upgrades
            CreateEquipment(path, "Expanded Pouch", EquipmentType.SatchelUpgrade, EquipmentTier.Basic,
                WildlifeSize.Tiny, 1f, 0f, 20, "Adds 20 slots to your satchel.");

            CreateEquipment(path, "Explorer's Pack", EquipmentType.SatchelUpgrade, EquipmentTier.Improved,
                WildlifeSize.Tiny, 1f, 0f, 50, "A large pack. Adds 50 slots to your satchel.");

            // Orb Magnet - increases detection
            CreateEquipment(path, "Orb Compass", EquipmentType.OrbMagnet, EquipmentTier.Basic,
                WildlifeSize.Tiny, 1f, 3f, 0, "Points toward nearby orbs. +3 detection range.");

            CreateEquipment(path, "Orb Beacon", EquipmentType.OrbMagnet, EquipmentTier.Advanced,
                WildlifeSize.Tiny, 1f, 8f, 0, "Greatly increases orb detection range. +8 range.");

            // Survival gear
            CreateEquipment(path, "Diving Suit", EquipmentType.DivingSuit, EquipmentTier.Basic,
                WildlifeSize.Tiny, 0.9f, 0f, 0, "Allows underwater exploration without a mount.");

            CreateEquipment(path, "Climbing Gear", EquipmentType.ClimbingGear, EquipmentTier.Basic,
                WildlifeSize.Tiny, 0.9f, 0f, 0, "Iron spikes and ropes for scaling cliffs.");

            CreateEquipment(path, "Heat Shield", EquipmentType.HeatShield, EquipmentTier.Basic,
                WildlifeSize.Tiny, 1f, 0f, 0, "Protects from extreme heat in volcanic regions.");

            CreateEquipment(path, "Frost Cloak", EquipmentType.FrostCloak, EquipmentTier.Basic,
                WildlifeSize.Tiny, 1f, 0f, 0, "Insulated cloak for surviving the frozen tundra.");
        }

        // --- Helper Methods ---

        private static void CreateOrb(string path, string name, OrbType type, OrbRarity rarity,
            RegionType[] regions, float dropRate, Color color, string desc)
        {
            var orb = ScriptableObject.CreateInstance<OrbData>();
            orb.orbName = name;
            orb.orbType = type;
            orb.rarity = rarity;
            orb.foundInRegions = regions;
            orb.baseDropRate = dropRate;
            orb.glowColor = color;
            orb.description = desc;

            UnityEditor.AssetDatabase.CreateAsset(orb, path + name.Replace(" ", "") + ".asset");
        }

        private static RegionData CreateRegion(string path, string name, RegionType type,
            Vector2Int pos, Color color, string desc)
        {
            var region = ScriptableObject.CreateInstance<RegionData>();
            region.regionName = name;
            region.regionType = type;
            region.mapPosition = pos;
            region.mapColor = color;
            region.description = desc;

            UnityEditor.AssetDatabase.CreateAsset(region, path + name.Replace(" ", "") + ".asset");
            return region;
        }

        private static void CreateWildlife(string path, string name, WildlifeType type,
            WildlifeBehavior behavior, WildlifeSize size, int difficulty, bool rideable,
            float speed, bool canSwim, bool canFly, bool canClimb,
            RegionType[] regions, float detectionMult, float dropBonus, string desc)
        {
            var wildlife = ScriptableObject.CreateInstance<WildlifeData>();
            wildlife.wildlifeName = name;
            wildlife.wildlifeType = type;
            wildlife.behavior = behavior;
            wildlife.size = size;
            wildlife.difficultyRating = difficulty;
            wildlife.isRideable = rideable;
            wildlife.mountSpeed = speed;
            wildlife.canSwim = canSwim;
            wildlife.canFly = canFly;
            wildlife.canClimb = canClimb;
            wildlife.nativeRegions = regions;
            wildlife.orbDetectionMultiplier = detectionMult;
            wildlife.orbDropRateBonus = dropBonus;
            wildlife.description = desc;
            wildlife.interactionsToMax = difficulty * 3;

            UnityEditor.AssetDatabase.CreateAsset(wildlife, path + name.Replace(" ", "") + ".asset");
        }

        private static void CreateEquipment(string path, string name, EquipmentType type,
            EquipmentTier tier, WildlifeSize maxRideable, float speedMult, float detectionBonus,
            int extraSlots, string desc)
        {
            var equip = ScriptableObject.CreateInstance<EquipmentData>();
            equip.equipmentName = name;
            equip.equipmentType = type;
            equip.tier = tier;
            equip.maxRideableSize = maxRideable;
            equip.speedMultiplier = speedMult;
            equip.detectionRangeBonus = detectionBonus;
            equip.extraSatchelSlots = extraSlots;
            equip.description = desc;

            UnityEditor.AssetDatabase.CreateAsset(equip, path + name.Replace(" ", "") + ".asset");
        }

        private static void EnsureFolderExists(string path)
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder(path.TrimEnd('/')))
            {
                string[] parts = path.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    if (string.IsNullOrEmpty(parts[i])) continue;
                    string next = current + "/" + parts[i];
                    if (!UnityEditor.AssetDatabase.IsValidFolder(next))
                    {
                        UnityEditor.AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }

    }
#endif
}
