# Silksong Archipelago — Project Roadmap & TODO List

---

## Current Status

- [x] **Repository Scaffolding**
  - [x] Archipelago Python World structure (`worlds/silksong/`)
  - [x] BepInEx 6 C# Client structure (`SilksongArchipelago/`)
  - [x] Code of Conduct & Development Rules (`.agents/rules/GEMINI.md`)
- [x] **Goal & Victory Conditions**
  - [x] `Goal` option in `options.py` (Act 2 Arrival, Act 3 Arrival, True Ending / Lost Lace)
  - [x] Completion conditions wired into `rules.py`
- [x] **Cartography & World Data**
  - [x] Extraction script `cartography/process_cartography.py`
  - [x] Taxonomy of 46 categories in `categories.json`
  - [x] 46 Pharloom regions with coordinate anchors in `regions.json`
  - [x] 441 Core checks isolated in `checks_core.json`
  - [x] 446 Optional sanity checks in `checks_optional.json`
  - [x] 272 Unique items cataloged with classifications in `items_catalog.json`

---

## Remaining Work

### 1. Archipelago Python World (`worlds/silksong/`)

- [x] **Data Model & Static IDs**
  - [x] Assign permanent unique IDs (`SILKSONG_BASE_ID = 777_000`) for all locations.
  - [x] Assign permanent unique IDs for all items.
  - [x] Created `worlds/silksong/data/` static datasets (`regions_data.py`, `locations_data.py`, `items_data.py`).
- [x] **Regions & World Graph (`regions.py`)**
  - [x] Instantiate the 45 Pharloom regions.
  - [x] Connect `"Menu"` to the starting region (`Mosslands`).
  - [x] Define physical entrances and transitions between adjacent regions (`REGION_CONNECTIONS`).
  - [x] Place victory events according to the selected `Goal` option.
- [x] **Locations (`locations.py`)**
  - [x] Populate `location_name_to_id` with all core and optional locations.
  - [x] Assign locations to their respective regions during world generation based on options.
- [x] **Items & Item Pool (`items.py`)**
  - [x] Populate `item_name_to_id` with all items.
  - [x] Implement `create_all_items`: add progression, useful items, and pad with filler items so `item_count == location_count`.
  - [x] Implement `get_filler_item_name` with non-farmable weighted pool (`Rosary String`, `Shard Bundle`, `Flea Brew`, `Silkeater`).
- [x] **Access Rules (`rules.py`)**
  - [x] Region transition logic (requires Cling Grip, Swift Step, Drifter's Cloak, Faydown Cloak, Needolin, Silk Soar).
  - [x] Location access logic (boss requirements, tool requirements, keys).
  - [x] Goal completion validation (Act 2 arrival, Act 3 arrival, True Ending / Lost Lace).
- [x] **Gameplay & Sanity Options (`options.py`)**
  - [x] Core toggles enabled by default (Abilities, Tools, Skills, Crests, Bosses, Wishes, Fleas, Upgrades, Vendors, Collectibles).
  - [x] Optional sanities disabled by default (Benchsanity, Fast Travel, Mapsanity, Loresanity, Hunter's Journal).
- [x] **Slot Data (`world.py`)**
  - [x] Transmit active options and goal configuration to client in `fill_slot_data`.
- [x] **Unit & Fill Verification (`test/` & test runner)**
  - [x] Seed generation verified.
  - [x] Item pool perfectly balanced with location checks (411 checks == 411 items).
  - [x] Full Archipelago restrictive fill algorithm tested and beatability confirmed.


---

### 2. C# Unity Client (`SilksongArchipelago/`)

- [x] **Network & Protocol (`Networking/ArchipelagoClient.cs`)**
  - [x] Setup `ArchipelagoSession` using `Archipelago.MultiClient.Net`.
  - [x] Implement connection error handling (wss:// and ws://).
  - [x] Implement automatic reconnect logic.
  - [x] Handle server slot data on connection.
- [x] **Location Detection (`Managers/LocationManager.cs` & `Patches/`)**
  - [x] Hook into item pickup triggers (`Patch_CollectableItemPickup_DoPickupAction`).
  - [x] Hook into boss defeat handlers (`Patch_HealthManager_Die`).
  - [x] Generate comprehensive in-game location ID mapping table (`scene + name -> locationId` in `LocationMapping.cs`).
  - [x] Cache unsent location checks when disconnected and send on reconnect.
- [x] **Item Receiving (`Managers/ItemManager.cs`)**
  - [x] Map AP item IDs to game actions (grant abilities, tools, crests, mask shards in `PlayerData`).
  - [x] Thread-safe queue to process items safely on Unity's main thread.
  - [x] Handle receiving duplicate/multiple items safely.
  - [x] Maintain received item index for session resynchronization.
- [x] **Save Management (`Managers/SaveManager.cs`)**
  - [x] Persist connection state, checked locations, and received item index to JSON configuration.
- [x] **Goal Detection (`Patches/Patch_HealthManager_Die.cs`)**
  - [x] Hook Act 2 arrival / Phantom & Last Judge defeat.
  - [ ] Hook Act 3 transition scene event.
  - [x] Hook Lost Lace defeat (True Ending).
  - [x] Send `StatusUpdate(CLIENT_GOAL)` via `SetGoalAchieved()`.
- [x] **In-Game Notifications & UI (`UI/ArchipelagoUI.cs`)**
  - [x] Display connection status HUD overlay (toggleable with `F2`).
  - [x] Interactive connection menu (Host, Port, Slot, Password).
  - [x] Show notification banner / toast when items are sent or received.

---

### 3. Documentation & Packaging

- [x] Complete `docs/en_Silksong.md` with complete options documentation.
- [x] Complete `docs/setup_en.md` with step-by-step installation instructions.
- [x] Add build scripts to generate `.apworld` bundle and BepInEx client archive (`package_release.py`).
