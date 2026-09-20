"""Generate static Python data structures for worlds/silksong/data/.

Filters out Arenas, NPCs, and farmable Rosaries/Shell Shards.
Assigns permanent unique Archipelago IDs (SILKSONG_BASE_ID = 777_000).
Generates:
  - worlds/silksong/data/__init__.py
  - worlds/silksong/data/regions_data.py
  - worlds/silksong/data/locations_data.py
  - worlds/silksong/data/items_data.py
"""

from __future__ import annotations

import json
import re
from pathlib import Path

SILKSONG_BASE_ID = 777_000

# 45 canonical regions (Slab and The Slab merged)
REGION_ADJACENCY: dict[str, dict[str, str | None]] = {
    # ── Lower Pharloom (Acts 1 & 2) ──────────────────────────────────────────
    "Mosslands": {
        "Mosshome": None,
        "Bone Bottom": None,
        "The Marrow": None,
        "Ruined Chapel": None,
    },
    "Mosshome": {
        "Mosslands": None,
        "Wormways": "Cling Grip",
    },
    "Bone Bottom": {
        "Mosslands": None,
        "The Marrow": None,
        "Deep Docks": None,
        "Weavenest Atla": "Needolin",
    },
    "The Marrow": {
        "Mosslands": None,
        "Bone Bottom": None,
        "Hunter's March": None,
        "Far Fields": "Swift Step",
    },
    "Ruined Chapel": {
        "Mosslands": None,
        "Bone Bottom": None,
        "Wormways": "Cling Grip",
    },
    "Wormways": {
        "Ruined Chapel": "Cling Grip",
        "Mosshome": "Cling Grip",
        "Shellwood": "Drifter's Cloak",
    },
    "Weavenest Atla": {
        "Bone Bottom": "Needolin",
        "The Abyss": "Drifter's Cloak",
    },
    "Deep Docks": {
        "Bone Bottom": None,
        "Far Fields": None,
        "Pilgrim's Rest": None,
        "The Abyss": "Silk Soar",
    },
    "Far Fields": {
        "The Marrow": "Swift Step",
        "Deep Docks": None,
        "Pilgrim's Rest": None,
        "Greymoor": None,
        "Bellhart": None,
    },
    "Pilgrim's Rest": {
        "Far Fields": None,
        "Deep Docks": None,
        "Weavenest Cindril": "Needolin",
    },
    "Weavenest Cindril": {
        "Pilgrim's Rest": "Needolin",
        "Verdania": "Faydown Cloak",
    },
    "Hunter's March": {
        "The Marrow": None,
        "Bellhart": None,
        "Halfway Home": None,
    },
    "Bellhart": {
        "Far Fields": None,
        "Hunter's March": None,
        "Halfway Home": None,
        "Shellwood": None,
        "Flea Caravan": None,
    },
    "Halfway Home": {
        "Bellhart": None,
        "Greymoor": None,
        "Hunter's March": None,
    },
    "Greymoor": {
        "Far Fields": None,
        "Halfway Home": None,
        "Sinner's Road": None,
        "Verdania": "Cling Grip",
    },
    "Verdania": {
        "Greymoor": "Cling Grip",
        "Weavenest Cindril": "Faydown Cloak",
        "Bilewater": "Drifter's Cloak",
    },
    "Shellwood": {
        "Bellhart": None,
        "Wormways": "Drifter's Cloak",
        "Blasted Steps": "Cling Grip",
        "Flea Caravan": None,
    },
    "Flea Caravan": {
        "Bellhart": None,
        "Shellwood": None,
        "Grand Gate": None,
        "Fleatopia": "Rescued 20 Fleas",
    },
    "Sinner's Road": {
        "Greymoor": None,
        "Wisp Thicket": "Swift Step",
        "Exhaust Organ": "Cling Grip",
    },
    "Wisp Thicket": {
        "Sinner's Road": "Swift Step",
        "Bilewater": None,
        "Underworks": "Drifter's Cloak",
    },
    "Blasted Steps": {
        "Shellwood": "Cling Grip",
        "Sands of Karak": None,
        "Grand Gate": None,
        "Coral Tower": "Drifter's Cloak",
    },
    "Coral Tower": {
        "Blasted Steps": "Drifter's Cloak",
        "Mount Fay": "Faydown Cloak",
    },
    "Sands of Karak": {
        "Blasted Steps": None,
        "The Slab": "Cling Grip",
        "Mount Fay": None,
    },
    "Mount Fay": {
        "Coral Tower": "Faydown Cloak",
        "Sands of Karak": None,
        "The Slab": "Drifter's Cloak",
    },
    "Grand Gate": {
        "Blasted Steps": None,
        "Flea Caravan": None,
        "The Slab": "Defeat Last Judge",
        "Underworks": "Defeat Phantom",
    },
    "The Slab": {
        "Grand Gate": None,
        "Sands of Karak": "Cling Grip",
        "Mount Fay": "Drifter's Cloak",
        "Citadel Spa": "Simple Key",
        "Choral Chambers": "Drifter's Cloak",
    },
    "Citadel Spa": {
        "The Slab": "Simple Key",
        "Choral Chambers": None,
    },
    "Exhaust Organ": {
        "Sinner's Road": "Cling Grip",
        "Grand Bellway": None,
        "Underworks": "Drifter's Cloak",
    },
    "Bilewater": {
        "Verdania": "Drifter's Cloak",
        "Wisp Thicket": None,
        "Bilehaven": None,
        "Putrified Ducts": "Cling Grip",
    },
    "Bilehaven": {
        "Bilewater": None,
        "Putrified Ducts": None,
    },
    # ── Upper Pharloom / Citadel (Acts 2 & 3) ────────────────────────────────
    "Underworks": {
        "Grand Gate": "Defeat Phantom",
        "Wisp Thicket": "Drifter's Cloak",
        "Exhaust Organ": "Drifter's Cloak",
        "Whiteward": None,
        "Choral Chambers": "Cling Grip",
    },
    "Grand Bellway": {
        "Exhaust Organ": None,
        "Whiteward": None,
        "Whispering Vaults": "Cling Grip",
    },
    "Whiteward": {
        "Underworks": None,
        "Grand Bellway": None,
        "High Halls": "Drifter's Cloak",
        "Choral Chambers": None,
    },
    "Choral Chambers": {
        "The Slab": "Drifter's Cloak",
        "Citadel Spa": None,
        "Underworks": "Cling Grip",
        "Whiteward": None,
        "Cogwork Core": "Drifter's Cloak",
    },
    "Whispering Vaults": {
        "Grand Bellway": "Cling Grip",
        "Songclave": None,
        "First Shrine": "Needolin",
    },
    "Cogwork Core": {
        "Choral Chambers": "Drifter's Cloak",
        "High Halls": "Cling Grip",
        "Songclave": None,
    },
    "Putrified Ducts": {
        "Bilewater": "Cling Grip",
        "Bilehaven": None,
        "Memorium": "Silk Soar",
    },
    "High Halls": {
        "Whiteward": "Drifter's Cloak",
        "Cogwork Core": "Cling Grip",
        "Memorium": None,
        "The Cradle": "Faydown Cloak",
    },
    "Songclave": {
        "Whispering Vaults": None,
        "Cogwork Core": None,
        "First Shrine": None,
        "The Cradle": "Silk Soar",
    },
    "First Shrine": {
        "Whispering Vaults": "Needolin",
        "Songclave": None,
        "The Cradle": "Faydown Cloak",
    },
    "Memorium": {
        "Putrified Ducts": "Silk Soar",
        "High Halls": None,
        "Terminus": None,
    },
    "Terminus": {
        "Memorium": None,
        "The Cradle": None,
    },
    "The Cradle": {
        "High Halls": "Faydown Cloak",
        "Songclave": "Silk Soar",
        "First Shrine": "Faydown Cloak",
        "Terminus": None,
    },
    "Fleatopia": {
        "Flea Caravan": "Rescued 20 Fleas",
    },
    "The Abyss": {
        "Deep Docks": "Silk Soar",
        "Weavenest Atla": "Drifter's Cloak",
    },
}


def clean_markdown(text: str | None) -> str:
    if not text:
        return ""
    # remove links
    text = re.sub(r"\[([^\]]+)\]\([^\)]+\)", r"\1", text)
    # remove bold / italics
    text = text.replace("**", "").replace("*", "").replace("_", "")
    return text.strip()


def infer_requirements(desc: str, title: str) -> list[str]:
    reqs: list[str] = []
    desc_l = (desc + " " + title).lower()

    if "cling grip" in desc_l:
        reqs.append("Cling Grip")
    if "swift step" in desc_l:
        reqs.append("Swift Step")
    if "drifter's cloak" in desc_l:
        reqs.append("Drifter's Cloak")
    if "faydown cloak" in desc_l:
        reqs.append("Faydown Cloak")
    if "silk soar" in desc_l:
        reqs.append("Silk Soar")
    if "needolin" in desc_l:
        reqs.append("Needolin")
    if "simple key" in desc_l:
        reqs.append("Simple Key")
    if "architect's key" in desc_l:
        reqs.append("Architect's Key")
    if "diving bell key" in desc_l:
        reqs.append("Diving Bell Key")
    if "bellhome key" in desc_l:
        reqs.append("Bellhome Key")
    if "key of apostate" in desc_l:
        reqs.append("Key of Apostate")
    if "key of heretic" in desc_l:
        reqs.append("Key of Heretic")

    return reqs


def main() -> None:
    cart_dir = Path("cartography")
    out_dir = Path("worlds/silksong/data")
    out_dir.mkdir(parents=True, exist_ok=True)

    with open(cart_dir / "data.json", "r", encoding="utf-8") as f:
        raw_data = json.load(f)

    # Categories we include:
    # Core: Ability(13569), Crest(13549), Skill(13568), Tool(13567), Upgrade(13570),
    #       Mask Shard(13545), Spool Fragment(13548), Silk Heart(13550), Memory Locket(13552),
    #       Boss(13587), Lost Flea(13544), Wish(13589), Vendor(13534), Quest Item(13590),
    #       Craftmetal(13561), Pale Oil(13555), Relics(13592, 13581, 13551, 13580, 13594, 13595, 13593)
    # (Notice: 13523 Arena Battle excluded, 13527 NPC excluded, 13554 Rosary excluded, 13560 Shell Shards excluded)
    core_cats = {
        13569: "Ability",
        13549: "Crest",
        13568: "Silk Skill",
        13567: "Tool",
        13570: "Upgrade",
        13545: "Mask Shard",
        13548: "Spool Fragment",
        13550: "Silk Heart",
        13552: "Memory Locket",
        13587: "Boss",
        13544: "Lost Flea",
        13589: "Wish",
        13534: "Vendor",
        13590: "Quest Item",
        13561: "Craftmetal",
        13555: "Pale Oil",
        13592: "Rune Harp",
        13581: "Bone Scroll",
        13551: "Choral Commandment",
        13580: "Weaver Effigy",
        13594: "Psalm Cylinder",
        13595: "Memento",
        13593: "Arcane Egg",
    }

    optional_cats = {
        13533: "Bench",
        13535: "Bellway Station",
        13539: "Ventrica Station",
        13585: "Area Map",
        13573: "Lore",
        13579: "Journal Entry",
    }

    # Load regions list
    with open(cart_dir / "regions.json", "r", encoding="utf-8") as f:
        regions_raw = json.load(f)

    # Process locations
    locations_by_id: dict[int, dict] = {}
    location_names_seen: set[str] = set()

    # Load cleaned checks
    with open(cart_dir / "checks_core.json", "r", encoding="utf-8") as f:
        core_checks_raw = json.load(f)

    with open(cart_dir / "checks_optional.json", "r", encoding="utf-8") as f:
        optional_checks_raw = json.load(f)

    loc_id_counter = SILKSONG_BASE_ID

    all_raw_checks = []
    # Filter core checks (remove Arena 13523)
    for c in core_checks_raw:
        if c["category_id"] in core_cats:
            all_raw_checks.append((c, "core"))

    for c in optional_checks_raw:
        if c["category_id"] in optional_cats:
            all_raw_checks.append((c, "optional_sanity"))

    # Assign region (normalizing Slab -> The Slab)
    def normalize_region(r: str) -> str:
        if r == "Slab":
            return "The Slab"
        if r not in REGION_ADJACENCY:
            return "Mosslands"
        return r

    processed_locations: list[dict] = []
    for raw_loc, tier in all_raw_checks:
        reg = normalize_region(raw_loc.get("region", "Mosslands"))
        raw_title = raw_loc.get("title", "Unknown")
        category = raw_loc.get("category", "General")
        desc = clean_markdown(raw_loc.get("description", ""))

        # Ensure unique location name
        # Ex: "Mask Shard - The Marrow (ID 477850)"
        base_name = f"{raw_title} - {reg}"
        if base_name in location_names_seen:
            unique_name = f"{base_name} ({raw_loc['id']})"
        else:
            unique_name = base_name
        location_names_seen.add(unique_name)

        loc_id_counter += 1
        reqs = infer_requirements(desc, raw_title)

        loc_entry = {
            "id": loc_id_counter,
            "raw_id": raw_loc["id"],
            "name": unique_name,
            "title": raw_title,
            "region": reg,
            "category": category,
            "tier": tier,
            "requirements": reqs,
            "description": desc,
        }
        processed_locations.append(loc_entry)

    print(f"Generated {len(processed_locations)} locations:")
    core_count = sum(1 for l in processed_locations if l["tier"] == "core")
    opt_count = sum(1 for l in processed_locations if l["tier"] == "optional_sanity")
    print(f"  - Core locations: {core_count}")
    print(f"  - Optional sanity locations: {opt_count}")

    # Build items catalog
    # Every unique item pickup from core locations becomes an Archipelago item
    item_id_counter = SILKSONG_BASE_ID + 50_000
    items_by_name: dict[str, dict] = {}

    progression_cats = {"Ability", "Crest", "Silk Skill", "Quest Item"}
    useful_cats = {"Tool", "Upgrade", "Mask Shard", "Spool Fragment", "Silk Heart", "Memory Locket"}

    for loc in processed_locations:
        if loc["tier"] == "core":
            item_name = loc["title"]
            # Exclude non-item names like Bosses / Wishes / Vendors from item names
            cat = loc["category"]
            if cat in ("Boss", "Wish", "Vendor"):
                # Bosses and Wishes grant an item; if the title is the boss, the reward might be an item
                # For boss/wish checks, if it's not an item name, we don't treat the boss name as an item,
                # we will create a filler or progression reward
                continue

            if item_name not in items_by_name:
                item_id_counter += 1
                if cat in progression_cats:
                    classification = "Progression"
                elif cat in useful_cats:
                    classification = "Useful"
                else:
                    classification = "Filler"

                items_by_name[item_name] = {
                    "id": item_id_counter,
                    "name": item_name,
                    "category": cat,
                    "classification": classification,
                    "count": 0,
                }
            items_by_name[item_name]["count"] += 1

    # Add standard Archipelago filler items (non-farmable bundles)
    standard_fillers = [
        ("Rosary String", "Filler", 50),
        ("Shard Bundle", "Filler", 40),
        ("Silkeater", "Useful", 10),
        ("Flea Brew", "Filler", 25),
        ("Pale Oil", "Useful", 3),
        ("Craftmetal", "Useful", 8),
    ]
    for name, cls_type, default_cnt in standard_fillers:
        if name not in items_by_name:
            item_id_counter += 1
            items_by_name[name] = {
                "id": item_id_counter,
                "name": name,
                "category": "Filler",
                "classification": cls_type,
                "count": default_cnt,
            }

    print(f"Generated {len(items_by_name)} distinct Archipelago items")

    # Write worlds/silksong/data/__init__.py
    with open(out_dir / "__init__.py", "w", encoding="utf-8") as f:
        f.write('"""Static game data for Silksong."""\n')

    # Write regions_data.py
    with open(out_dir / "regions_data.py", "w", encoding="utf-8") as f:
        f.write('"""Region definitions and adjacency graph for Silksong."""\n\n')
        f.write("from __future__ import annotations\n\n")
        f.write(f"REGION_CONNECTIONS: dict[str, dict[str, str | None]] = {repr(REGION_ADJACENCY)}\n")

    # Write locations_data.py
    with open(out_dir / "locations_data.py", "w", encoding="utf-8") as f:
        f.write('"""Static location definitions for Silksong."""\n\n')
        f.write("from __future__ import annotations\n\n")
        f.write(f"ALL_LOCATIONS: list[dict] = {json.dumps(processed_locations, indent=4)}\n\n")
        f.write("LOCATION_NAME_TO_ID: dict[str, int] = {\n")
        for loc in processed_locations:
            f.write(f'    "{loc["name"]}": {loc["id"]},\n')
        f.write("}\n")

    # Write items_data.py
    with open(out_dir / "items_data.py", "w", encoding="utf-8") as f:
        f.write('"""Static item definitions for Silksong."""\n\n')
        f.write("from __future__ import annotations\n\n")
        f.write(f"ALL_ITEMS: dict[str, dict] = {json.dumps(items_by_name, indent=4)}\n\n")
        f.write("ITEM_NAME_TO_ID: dict[str, int] = {\n")
        for item in items_by_name.values():
            f.write(f'    "{item["name"]}": {item["id"]},\n')
        f.write("}\n")

    print(f"Static Python datasets successfully generated in {out_dir}")


if __name__ == "__main__":
    main()
