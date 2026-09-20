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

## 3. 🚀 Quickstart: Testing In-Game

Follow these simple steps to test Archipelago live in Silksong:

### Step 1: Generate a Seed
```bash
py -3.13 generate_seed.py
```
This command:
- Uses `worlds/silksong/Silksong.yaml` settings.
- Verifies full accessibility and beatability of the goal.
- Generates `AP_<seed>_<player>.archipelago` and a spoiler log `AP_<seed>_<player>_spoiler.txt`.

### Step 2: Start the Archipelago Server
```bash
py -3.13 run_server.py
```
The server will start on port `38281` and listen for connections.

### Step 3: Install the Mod to Silksong
1. Ensure **BepInEx 5** is installed in your Silksong game folder.
2. Copy `SilksongArchipelago/bin/Debug/netstandard2.1/SilksongArchipelago.dll` into your game's `BepInEx/plugins/` directory.
3. Also copy `Archipelago.MultiClient.Net.dll` (from `SilksongArchipelago/bin/Debug/netstandard2.1/`) into `BepInEx/plugins/`.

### Step 4: Play & Connect!
1. Launch Hollow Knight: Silksong.
2. In-game or in the title screen, press **F2** to bring up the **Archipelago Multiworld Menu**.
3. Verify the settings (Default: `Host: localhost`, `Port: 38281`, `Slot: Hornet`).
4. Click **Connect**.
5. Once connected (`● Connected`), enjoy the game! Received items and checked locations appear in real-time on screen as floating toast notifications and are logged to the in-game console.

---

## License & Fair Use
Hollow Knight: Silksong and all related assets, names, and code are property of **Team Cherry**. This project is a community-developed mod designed for fair-use interoperability with the open-source Archipelago Multiworld platform.

