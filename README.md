# Silksong Archipelago

[![Archipelago Multiworld](https://img.shields.io/badge/Archipelago-Multiworld-blue.svg)](https://archipelago.gg/)
[![.NET Standard 2.1](https://img.shields.io/badge/.NET%20Standard-2.1-purple.svg)](https://dotnet.microsoft.com/)
[![BepInEx 5](https://img.shields.io/badge/BepInEx-5.4.21-orange.svg)](https://github.com/BepInEx/BepInEx)

Official repository for the **Hollow Knight: Silksong** multiworld integration for [Archipelago](https://archipelago.gg/).

---

## Repository Structure

```
silksong-archipelago/
├── .agents/                 # AI assistant guidelines and rules
├── cartography/             # Map extraction, taxonomy & dataset generation scripts
├── libs/                    # Reference game assemblies (Assembly-CSharp.dll, UnityEngine.dll)
├── SilksongArchipelago/     # C# BepInEx Client mod
│   ├── Managers/            # ItemManager, LocationManager, SaveManager
│   ├── Networking/          # ArchipelagoClient (WebSocket & AP network layer)
│   ├── Patches/             # Harmony patches for game hooks
│   ├── SilksongArchipelagoPlugin.cs
│   └── SilksongArchipelago.csproj
├── worlds/                  # Archipelago Python World implementation
│   └── silksong/
│       ├── data/            # Canonical regions, locations, and items datasets
│       ├── docs/            # English game info and setup guides
│       ├── test/            # Unit tests and generation tests
│       ├── items.py         # Item definitions and pool balancing
│       ├── locations.py     # Location registration and checks logic
│       ├── options.py       # YAML configuration options and toggles
│       ├── regions.py       # 45-region world graph and entrance logic
│       ├── rules.py         # Movement requirements, key checks and goals
│       └── world.py         # Archipelago World subclass
└── TODO.md                  # Project roadmap and completed tasks
```

---

## 1. Archipelago Python World (`worlds/silksong/`)

### Features
- **Canonical Map Graph**: 45 interconnected regions starting at `Mosslands`.
- **Location Checks**: 857 total indexed locations (411 Core checks active by default, 446 optional sanity checks).
- **Item Pool**: 161 unique progression, useful, and filler items, with balanced filler (`Rosary String`, `Shard Bundle`, `Flea Brew`, `Silkeater`).
- **Flexible Victory Conditions**: Selectable in YAML options:
  - `Act 2 Arrival` (Phantom or Last Judge defeat)
  - `Act 3 Arrival`
  - `True Ending` (Lost Lace defeat)

### Testing Seed Generation
Requires Python 3.11 - 3.13 with Archipelago dependencies:
```bash
py -3.13 -m unittest worlds.silksong.test.test_generation
```

---

## 2. C# Unity Client Mod (`SilksongArchipelago/`)

### Prerequisites
- [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
- Silksong game assemblies: Copy `Assembly-CSharp.dll`, `UnityEngine.dll`, and `UnityEngine.CoreModule.dll` from your game's `Silksong_Data/Managed/` directory into the `libs/` folder.

### Building the Client Mod
```bash
dotnet build SilksongArchipelago/SilksongArchipelago.csproj
```
The compiled plugin will be located at:
`SilksongArchipelago/bin/Debug/netstandard2.1/SilksongArchipelago.dll`

---

## License & Fair Use
Hollow Knight: Silksong and all related assets, names, and code are property of **Team Cherry**. This project is a community-developed mod designed for fair-use interoperability with the open-source Archipelago Multiworld platform.
