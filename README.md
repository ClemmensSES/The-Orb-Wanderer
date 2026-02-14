# The Orb Wanderer

A mobile exploration/collection game built with Unity where players traverse a vast world, collecting elemental orbs, befriending wildlife, and unlocking new regions.

## Game Concept

You begin in a small green meadow. Opening the world map reveals a massive landscape — oceans, mountains, caves, deserts, frozen tundras, volcanic depths, and floating sky islands — all grayed out and waiting to be discovered.

Your goal: **collect orbs** from the environment and wildlife in unlocked areas, then **spend those orbs** to unlock new regions. Each region requires specific combinations of orb types to unlock, creating natural progression through the world.

## Core Systems

### Orb Collection
- **10 orb types**: Nature, Terra, Aqua, Ember, Zephyr, Frost, Shadow, Radiant, Storm, Prismatic
- **5 rarity tiers**: Common → Legendary
- Orbs spawn from trees, rocks, water, and the environment in each region
- Rarer orbs appear in more dangerous/remote regions

### Satchel (Inventory)
- Orbs are stored in your satchel with a base capacity of 50
- Capacity can be expanded via crafted equipment
- Orbs are spent to unlock regions, befriend wildlife, and craft equipment

### World Map & Fog of War
- **10 distinct regions** arranged on a grid map
- Regions start **locked** (grayed out on map)
- Adjacent regions to unlocked areas become **discovered** (visible but locked)
- Each region requires a specific orb recipe to unlock
- Starting region: Verdant Meadows (free, unlocked at start)

### Wildlife & Companions
- **9 wildlife species** across 4 types (Land, Aquatic, Aerial, Subterranean)
- Each has unique behavior: Passive, Timid, Neutral, or Aggressive
- **Befriending** requires orb combinations — rarer wildlife need rarer orbs
- Befriended wildlife become **companions** with friendship progression
- **Key rule**: harder-to-obtain wildlife provide better orb-finding bonuses

### Mounting System
- Rideable wildlife serve as mounts with different capabilities:
  - **Aquatic mounts**: traverse ocean regions (smaller = slower without saddle)
  - **Aerial mounts**: fly over terrain
  - **Climbing mounts**: scale cliffs and mountains
- **Equipment gating**: you need saddles to ride larger wildlife
  - No saddle → only Tiny mounts
  - Rope Saddle → Small mounts
  - Leather Saddle → Medium mounts
  - Reinforced Saddle → Large mounts
  - Master Saddle → Massive mounts

### Equipment Crafting
- **12 equipment items** across multiple types
- Crafted by spending orbs
- Types: Saddles, Satchel Upgrades, Orb Magnets, Diving Suit, Climbing Gear, Heat Shield, Frost Cloak

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/           # GameManager, GameEnums, CameraController
│   ├── Data/           # ScriptableObject definitions (OrbData, WildlifeData, RegionData, EquipmentData)
│   │   └── ScriptableObjects/  # Default data generator
│   ├── Inventory/      # SatchelManager
│   ├── World/          # WorldMapManager, RegionController, CollectibleOrb
│   ├── Wildlife/       # WildlifeController, CompanionManager
│   ├── Player/         # PlayerController, OrbDetector
│   ├── Equipment/      # EquipmentManager
│   ├── UI/             # WorldMapUI, SatchelUI, HUDManager, MobileInputUI, RegionUnlockUI
│   └── Utils/          # Singleton base class
├── Scenes/
├── Prefabs/            # Wildlife/, Orbs/, UI/, Player/, Environment/
├── Materials/
├── Sprites/            # Orbs/, Wildlife/, UI/, Map/, Player/, Environment/
├── Audio/              # Music/, SFX/
├── Resources/
└── StreamingAssets/
```

## Getting Started (Unity Setup)

1. Open the project in **Unity 6.x** (6.3 recommended) or Unity 2022.3+
2. Go to **OrbWanderer > Create Default Data** in the Unity menu bar to generate all ScriptableObject assets
3. Create a new scene and add these GameObjects:
   - `GameManager` with GameManager component
   - `WorldMapManager` with WorldMapManager component
   - `SatchelManager` with SatchelManager component
   - `CompanionManager` with CompanionManager component
   - `EquipmentManager` with EquipmentManager component
   - `Player` (tag: "Player") with PlayerController and OrbDetector components
   - `Main Camera` with CameraController component
   - `Canvas` with HUDManager, WorldMapUI, SatchelUI, MobileInputUI components
4. Assign the generated ScriptableObject data to the managers
5. Set build target to **iOS** or **Android**

## Regions & Progression

| Region | Type | Unlock Cost (example) |
|--------|------|----------------------|
| Verdant Meadows | Grassland | Free (starting area) |
| Whispering Woods | Forest | 5 Nature |
| Azure Coast | Ocean | 5 Nature + 3 Terra |
| Murkmire Swamp | Swamp | 5 Aqua + 5 Nature |
| Stonepeak Mountains | Mountain | 8 Terra + 3 Zephyr |
| Crystal Caverns | Cave | 5 Terra + 5 Shadow |
| Ember Wastes | Desert | 8 Ember + 3 Radiant |
| Frozen Reach | Tundra | 8 Frost + 5 Zephyr |
| Magma Depths | Volcano | 10 Ember + 5 Terra |
| Celestial Isles | Sky Islands | 5 of every type |

## Wildlife Guide

| Wildlife | Difficulty | Type | Rideable | Special |
|----------|-----------|------|----------|---------|
| Meadow Hopper | 1 | Land | Yes (slow) | Finds Nature orbs |
| Bark Beetle | 1 | Land | No | Orb detector |
| River Otter | 3 | Aquatic | Yes (swim) | Finds Aqua orbs |
| Stone Elk | 4 | Land | Yes (climb) | All-terrain land mount |
| Dune Lizard | 5 | Land | Yes (fast) | Finds Ember orbs |
| Leviathan Turtle | 7 | Aquatic | Yes (swim) | Best ocean mount |
| Cave Wyrm | 8 | Subterranean | Yes (climb) | Finds Shadow + Ember |
| Storm Hawk | 8 | Aerial | Yes (fly) | Fastest mount, finds Storm orbs |
| Celestial Stag | 10 | Land | Yes (all-terrain) | Legendary, finds all orbs |

## Mobile Controls
- **Left side**: Virtual joystick for movement
- **Right side**: Action buttons (Interact, Mount, Map, Satchel)
- **Pinch**: Zoom camera / Zoom map
- **Keyboard** (editor): WASD movement, M (map), Tab (satchel), E (mount)

## Build Target
- iOS and Android
- Target frame rate: 60 FPS
- 2D orthographic camera with smooth follow
