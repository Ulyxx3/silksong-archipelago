# Silksong Cartography Data & Archipelago Extraction

This folder contains processed data extracted from the interactive map (`data.json`, 1.16 MB, 1,939 markers).
The raw data has been parsed, cleaned, and categorized into structured datasets by `process_cartography.py`.

---

## Generated Datasets

| File | Size | Description |
|---|---|---|
| `categories.json` | ~5 KB | Full taxonomy of all 46 map categories grouped by type with marker counts. |
| `regions.json` | ~14 KB | 46 distinct Pharloom regions with coordinate centroids, benches, and bellways. |
| `checks_core.json` | ~195 KB | 441 high-priority randomizer checks (abilities, crests, tools, bosses, etc.). |
| `checks_optional.json` | ~170 KB | 446 optional sanity checks (benches, fast travel, maps, lore, journal). |
| `checks_filler.json` | ~84 KB | 252 minor currency and shard pickups (rosaries, shell shards, bundles). |
| `logic_obstacles.json` | ~291 KB | 800 world obstacles (locked doors, levers, breakable walls, silk walls). |
| `items_catalog.json` | ~40 KB | 272 unique items cataloged with classifications (Progression, Useful, Filler). |

---

## Category Taxonomy Overview

- **Equipment (94 checks)**: Abilities (11), Crests (6), Silk Skills (6), Tools (56), Upgrades (15).
- **Collectibles (123 checks)**: Mask Shards (20), Spool Fragments (18), Memory Lockets (20), Lost Fleas (30),
  Silk Hearts (3), Mementos (11), Psalm Cylinders (6), Bone Scrolls (4), Choral Commandments (4), Rune Harps (3),
  Weaver Effigies (3), Arcane Egg (1).
- **Enemies (313 checks)**: Bosses (46), Arena Battles (30), Journal Entries (237).
- **Quests & NPCs (135 checks)**: Wishes (48), Quest Items (66), Objectives (21).
- **Points of Interest (289 checks)**: Benches (81), Vendors (23), Bellway Stations (12), Ventrica Stations (7),
  Plasmium Cocoons (4), Void Mass (46), NPCs (146).
- **Items & Currency (263 checks)**: Area Maps (29), Craftmetal (8), Pale Oil (3), Rosaries (107),
  Rosary Strings (34), Shell Shards (79), Shard Bundles (22), Silkeaters (10).
- **World & Navigation (622 checks)**: Breakable Surfaces (216), Locked Doors (89), Lore Tablets (80),
  Levers (58), Shortcuts (26), Needolin Doors (5), Silk Walls (2), Achievements (52), Miscellaneous (119).

---

## How to Regenerate

Run the extraction script from the repository root:

```bash
python cartography/process_cartography.py
```
