namespace OrbWanderer.Core
{
    public enum OrbType
    {
        Nature,      // From trees, plants, grass
        Terra,       // From earth, rocks, soil
        Aqua,        // From water sources, rain
        Ember,       // From volcanic areas, hot springs
        Zephyr,      // From windy peaks, open plains
        Frost,       // From snowy mountains, ice caves
        Shadow,      // From deep caves, dark forests
        Radiant,     // From sunlit clearings, crystals
        Storm,       // Rare - from thunderstorms
        Prismatic    // Ultra rare - from combining all types
    }

    public enum OrbRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum RegionType
    {
        Grassland,
        Forest,
        Mountain,
        Cave,
        Ocean,
        Desert,
        Tundra,
        Volcano,
        Swamp,
        SkyIslands
    }

    public enum RegionState
    {
        Locked,      // Grayed out on map, cannot enter
        Discovered,  // Visible on map but not yet unlocked
        Unlocked,    // Can enter and explore
        Completed    // All orbs collected
    }

    public enum WildlifeType
    {
        Land,
        Aquatic,
        Aerial,
        Subterranean
    }

    public enum WildlifeBehavior
    {
        Passive,     // Won't flee, easy to approach
        Timid,       // Flees when player gets close
        Neutral,     // Ignores player unless provoked
        Aggressive   // Will charge at player
    }

    public enum WildlifeSize
    {
        Tiny,
        Small,
        Medium,
        Large,
        Massive
    }

    public enum FriendshipLevel
    {
        Wild,
        Acquaintance,
        Friendly,
        Companion,
        Bonded
    }

    public enum EquipmentType
    {
        Saddle,
        Reins,
        Lantern,
        Compass,
        DivingSuit,
        ClimbingGear,
        HeatShield,
        FrostCloak,
        OrbMagnet,
        SatchelUpgrade
    }

    public enum EquipmentTier
    {
        Basic,
        Improved,
        Advanced,
        Master
    }
}
