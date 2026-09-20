# Silksong Archipelago — Code of Conduct & Development Rules

> This file is automatically loaded by the AI assistant for every coding session
> in this repository. It defines strict rules that **must never be violated**.

---

## 1. Archipelago Python World Rules

### 1.1 Style & Formatting
- **120 characters per line maximum** for all source files (Python, Markdown, JSON).
- **Double quotes only** for all string literals: `"like this"`, never `'like this'`.
- Inside f-strings, use single quotes for dict keys: `f"Value: {d['key']}"`.
- Use **format string literals** (f-strings) over concatenation.
- Follow **PEP 8** except where Archipelago style overrides it (120 char lines, double quotes).
- Avoid trailing whitespace.

### 1.2 Type Annotations
- Use **modern-style** type annotations everywhere:
  - `dict[str, int]` not `Dict[str, int]`
  - `list[Item]` not `List[Item]`
  - `str | int` not `Union[str, int]`
  - `X | None` not `Optional[X]`
- Annotate function signatures, class members, and local variables where helpful.

### 1.3 Security & Banned Patterns
- **Never use `eval()`** — absolutely forbidden.
- **Never use `yaml.load()` directly** — use `Utils.parse_yaml` instead.
- **Never use `=` to assign to `multiworld.itempool` or `multiworld.regions`** — this
  overwrites data for ALL games. Always use `.append()`, `.extend()`, or `+=`.

### 1.4 Architecture Conventions
- Imports of base Archipelago modules **must be absolute**: `from worlds.AutoWorld import World`.
- Imports of our own world files **must be relative**: `from .items import ...`.
- Every subfolder containing `.py` files **must** have an `__init__.py` (required for frozen builds).
- The `item_name_to_id` and `location_name_to_id` dictionaries must contain **all** items/locations
  regardless of options. Whether they actually appear in a seed is controlled by creation logic.
- Item IDs and Location IDs must be **unique positive integers** (not 0, not None — those are for events).
- Use a consistent ID offset base: `SILKSONG_BASE_ID = 0xSS0000` (to be defined).
- The origin region defaults to `"Menu"` but can be overridden with `origin_region_name`.

### 1.5 World Class Requirements
Every `World` subclass **must** have:
- A unique `game` name string.
- An instantiated `WebWorld` subclass in `web`.
- `item_name_to_id` and `location_name_to_id` class-level dictionaries.
- A working `create_item(name: str)` method.
- At least one `Region` (the origin region) with locations.
- A completion condition set via `multiworld.completion_condition[self.player]`.
- Items count **must equal** locations count (excluding events).

### 1.6 Documentation
- The world folder must contain a `docs/` subdirectory with:
  - At least one game info doc: `en_Silksong.md`
  - At least one setup guide: `setup_en.md`
- Preserve all existing comments and docstrings unless explicitly told to change them.

---

## 2. C# Client (Unity / BepInEx) Rules

### 2.1 Framework
- Target **BepInEx 6.x** (IL2CPP-compatible) with **HarmonyX** for patching.
- Use **`Archipelago.MultiClient.Net`** NuGet package for server communication.
- Target **.NET Standard 2.1** or the framework matching BepInEx 6 requirements.

### 2.2 Code Style
- Use **PascalCase** for public members, **camelCase** for private/local.
- Use C# nullable reference types (`string?`, `Item?`) where applicable.
- Prefix Harmony patch classes with `Patch_` and suffix with the method being patched.
- Keep patches minimal — delegate logic to manager/service classes.

### 2.3 Architecture
- **Plugin entry point**: single `[BepInPlugin]` class inheriting `BasePlugin`.
- **ArchipelagoClient**: handles all network communication (connect, send, receive).
- **ItemManager**: maps AP item IDs to in-game effects.
- **LocationManager**: detects location checks and sends them to the server.
- **SaveManager**: persists received item index for resync across sessions.
- Use **events/delegates** for decoupling (e.g., `OnItemReceived`, `OnLocationChecked`).

### 2.4 Hard Requirements (from Archipelago spec)
- Handle both secure (wss) and unsecure (ws) WebSocket connections.
- Implement reconnection logic.
- Support port changes for saved connection info.
- Send `StatusUpdate` with `CLIENT_GOAL` when the player completes their goal.
- Send location checks on reconnect if they were made while disconnected.
- Maintain an item receive index for resync.
- Handle receiving any item any number of times.
- Handle items with no player/location attribution (admin commands).

---

## 3. General Project Rules

### 3.1 Git Hygiene
- Commit messages follow: `type(scope): description` (e.g., `feat(world): add Greenpatch region`).
- Never commit generated/build artifacts.
- Keep `.gitignore` up to date for both Python and C# artifacts.

### 3.2 Testing
- All world logic should be testable via Archipelago's built-in test framework.
- Test files go in a `test/` subfolder within the world package.

### 3.3 Collaboration
- Before writing any game logic (regions, items, rules), **ask the user** for:
  - The list of regions / areas.
  - The list of items and their classifications.
  - The list of locations and which region they belong to.
  - Decompiled methods or game data needed for the client patches.
- Never invent game content — only use data confirmed by the user.

---

> **Remember**: These rules apply to EVERY file written in this repository.
> When in doubt, refer to the Archipelago docs (`adding games.md`, `style.md`, `world api.md`).
