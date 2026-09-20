# Silksong Multiworld Setup Guide

## Required Software

- [Archipelago](https://github.com/ArchipelagoMW/Archipelago/releases) (latest release)
- Silksong (Steam or GOG version)
- The Silksong Archipelago BepInEx mod (see below)

## Installation

### 1. Install BepInEx

1. Download **BepInEx 6.x** (IL2CPP build) from the
   [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases).
2. Extract the contents into your Silksong game directory
   (the folder containing `Silksong.exe`).
3. Run Silksong once to let BepInEx generate its folder structure, then close
   the game.

### 2. Install the Archipelago Mod

1. Download the latest release of `SilksongArchipelago.dll` from the
   [releases page](https://github.com/Ulyxx3/silksong-archipelago/releases).
2. Place the `.dll` file into `BepInEx/plugins/` inside your Silksong directory.

### 3. Generate a Multiworld

1. Put your YAML configuration file into the Archipelago `Players/` folder.
2. Run `ArchipelagoGenerate.exe` (or use the website) to generate the multiworld.
3. Host the resulting `.archipelago` file with `ArchipelagoServer.exe` or
   upload it to [archipelago.gg](https://archipelago.gg).

### 4. Connect

1. Launch Silksong — the mod will show a connection overlay.
2. Enter the server address, port, slot name, and (optionally) the password.
3. Press **Connect** and start playing!

## Troubleshooting

- **Mod not loading?** — Make sure BepInEx 6.x (IL2CPP) is correctly installed.
  Check `BepInEx/LogOutput.log` for errors.
- **Connection refused?** — Verify the server address and port. Check that no
  firewall is blocking the connection.
