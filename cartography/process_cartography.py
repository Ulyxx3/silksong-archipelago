"""Silksong Cartography Processor.

Parses raw data.json from MapGenie export, cleans the bloat, categorizes all
locations, resolves regions, and exports structured datasets for Archipelago.
"""

from __future__ import annotations

import json
import math
import re
from collections import Counter, defaultdict
from pathlib import Path

CATEGORY_MAPPING: dict[int, dict[str, str]] = {
    # Points of Interest
    13535: {"name": "Bellway Station", "group": "Points of Interest"},
    13533: {"name": "Bench", "group": "Points of Interest"},
    13527: {"name": "NPC", "group": "Points of Interest"},
    13525: {"name": "Plasmium Cocoon", "group": "Points of Interest"},
    13528: {"name": "Point of Interest", "group": "Points of Interest"},
    13534: {"name": "Vendor", "group": "Points of Interest"},
    13539: {"name": "Ventrica Station", "group": "Points of Interest"},
    13529: {"name": "Void Mass", "group": "Points of Interest"},
    # Collectibles
    13593: {"name": "Arcane Egg", "group": "Collectibles"},
    13581: {"name": "Bone Scroll", "group": "Collectibles"},
    13551: {"name": "Choral Commandment", "group": "Collectibles"},
    13544: {"name": "Lost Flea", "group": "Collectibles"},
    13545: {"name": "Mask Shard", "group": "Collectibles"},
    13595: {"name": "Memento", "group": "Collectibles"},
    13552: {"name": "Memory Locket", "group": "Collectibles"},
    13594: {"name": "Psalm Cylinder", "group": "Collectibles"},
    13592: {"name": "Rune Harp", "group": "Collectibles"},
    13550: {"name": "Silk Heart", "group": "Collectibles"},
    13548: {"name": "Spool Fragment", "group": "Collectibles"},
    13580: {"name": "Weaver Effigy", "group": "Collectibles"},
    # Items
    13585: {"name": "Area Map", "group": "Items"},
    13561: {"name": "Craftmetal", "group": "Items"},
    13555: {"name": "Pale Oil", "group": "Items"},
    13554: {"name": "Rosary", "group": "Items"},
    13559: {"name": "Rosary String", "group": "Items"},
    13547: {"name": "Shard Bundle", "group": "Items"},
    13560: {"name": "Shell Shards", "group": "Items"},
    13563: {"name": "Silkeater", "group": "Items"},
    # Equipment
    13569: {"name": "Ability", "group": "Equipment"},
    13549: {"name": "Crest", "group": "Equipment"},
    13568: {"name": "Silk Skill", "group": "Equipment"},
    13567: {"name": "Tool", "group": "Equipment"},
    13570: {"name": "Upgrade", "group": "Equipment"},
    # Enemies
    13523: {"name": "Arena Battle", "group": "Enemies"},
    13587: {"name": "Boss", "group": "Enemies"},
    13579: {"name": "Journal Entry", "group": "Enemies"},
    # Quests
    13588: {"name": "Objective", "group": "Quests"},
    13590: {"name": "Quest Item", "group": "Quests"},
    13589: {"name": "Wish", "group": "Quests"},
    # Other
    13574: {"name": "Achievement", "group": "Other"},
    13584: {"name": "Breakable Surface", "group": "Other"},
    13572: {"name": "Easter Egg", "group": "Other"},
    13576: {"name": "Lever", "group": "Other"},
    13582: {"name": "Locked Door", "group": "Other"},
    13573: {"name": "Lore", "group": "Other"},
    13571: {"name": "Miscellaneous", "group": "Other"},
    13583: {"name": "Needolin Door", "group": "Other"},
    13577: {"name": "Shortcut", "group": "Other"},
    13578: {"name": "Silk Wall", "group": "Other"},
}

CORE_CATEGORIES = {
    13569,  # Ability
    13549,  # Crest
    13568,  # Silk Skill
    13567,  # Tool
    13570,  # Upgrade
    13545,  # Mask Shard
    13548,  # Spool Fragment
    13550,  # Silk Heart
    13552,  # Memory Locket
    13587,  # Boss
    13523,  # Arena Battle
    13544,  # Lost Flea
    13589,  # Wish
    13534,  # Vendor
    13590,  # Quest Item
    13561,  # Craftmetal
    13555,  # Pale Oil
    13592,  # Rune Harp
    13581,  # Bone Scroll
    13551,  # Choral Commandment
    13580,  # Weaver Effigy
    13594,  # Psalm Cylinder
    13595,  # Memento
    13593,  # Arcane Egg
}

OPTIONAL_SANITY_CATEGORIES = {
    13533,  # Bench
    13535,  # Bellway Station
    13539,  # Ventrica Station
    13585,  # Area Map
    13573,  # Lore
    13579,  # Journal Entry
}

FILLER_CATEGORIES = {
    13554,  # Rosary
    13559,  # Rosary String
    13560,  # Shell Shards
    13547,  # Shard Bundle
    13563,  # Silkeater
}

LOGIC_OBSTACLE_CATEGORIES = {
    13582,  # Locked Door
    13576,  # Lever
    13584,  # Breakable Surface
    13577,  # Shortcut
    13583,  # Needolin Door
    13578,  # Silk Wall
    13588,  # Objective
    13574,  # Achievement
    13571,  # Miscellaneous
    13529,  # Void Mass
    13572,  # Easter Egg
    13525,  # Plasmium Cocoon
    13527,  # NPC
    13528,  # Point of Interest
}


def clean_markdown_links(text: str | None) -> str:
    """Replace [Text](http://...) with just Text."""
    if not text:
        return ""
    return re.sub(r"\[([^\]]+)\]\([^\)]+\)", r"\1", text).strip()


def main() -> None:
    base_dir = Path(__file__).resolve().parent
    raw_path = base_dir / "data.json"

    with open(raw_path, "r", encoding="utf-8") as f:
        raw_data = json.load(f)

    raw_locations = raw_data.get("locations", [])
    print(f"Loaded {len(raw_locations)} locations from {raw_path.name}")

    # 1. Build Region Anchors from Benches, Bellways, Stations, and Maps
    region_anchors: dict[str, list[tuple[float, float]]] = defaultdict(list)

    for loc in raw_locations:
        title = loc.get("title", "")
        cid = loc.get("category_id")
        lat = float(loc.get("latitude", 0))
        lon = float(loc.get("longitude", 0))

        area_found: str | None = None
        if cid == 13533 and " - " in title:  # Bench
            area_found = title.split(" - ", 1)[1].strip()
        elif cid in (13535, 13539) and " - " in title:  # Bellway / Ventrica
            area_found = title.split(" - ", 1)[1].strip()
        elif cid == 13585 and title.endswith(" Map"):  # Area Map
            area_found = title[:-4].strip()

        if area_found:
            # Clean up modifiers like "Temporary Bench"
            if area_found.startswith("Temporary Bench"):
                continue
            region_anchors[area_found].append((lat, lon))

    # Calculate centroid for each region
    region_centroids: dict[str, dict[str, float]] = {}
    for region, coords in region_anchors.items():
        avg_lat = sum(c[0] for c in coords) / len(coords)
        avg_lon = sum(c[1] for c in coords) / len(coords)
        region_centroids[region] = {"latitude": avg_lat, "longitude": avg_lon}

    print(f"Identified {len(region_centroids)} distinct regions with anchor centroids")

    # 2. Process and enrich every location
    processed_locations: list[dict] = []
    region_counts: Counter[str] = Counter()

    for loc in raw_locations:
        cid = loc.get("category_id")
        cat_meta = CATEGORY_MAPPING.get(cid, {"name": f"Category {cid}", "group": "Unknown"})
        title = loc.get("title", "")
        desc = clean_markdown_links(loc.get("description", ""))
        lat = float(loc.get("latitude", 0))
        lon = float(loc.get("longitude", 0))

        # Determine region
        assigned_region: str | None = None

        # A: Check explicit prefix/suffix in title
        if " - " in title:
            candidate = title.split(" - ", 1)[1].strip()
            for r in region_centroids:
                if candidate.lower() == r.lower():
                    assigned_region = r
                    break

        # B: Check full word match of known regions in title
        if not assigned_region:
            for r in sorted(region_centroids.keys(), key=len, reverse=True):
                if re.search(rf"\b{re.escape(r)}\b", title, re.IGNORECASE):
                    assigned_region = r
                    break

        # C: Check full word match of known regions in description
        if not assigned_region and desc:
            for r in sorted(region_centroids.keys(), key=len, reverse=True):
                if re.search(rf"\b{re.escape(r)}\b", desc, re.IGNORECASE):
                    assigned_region = r
                    break

        # D: Nearest geographic anchor centroid
        if not assigned_region:
            closest_dist = float("inf")
            for r, centroid in region_centroids.items():
                dlat = lat - centroid["latitude"]
                dlon = lon - centroid["longitude"]
                dist = math.hypot(dlat, dlon)
                if dist < closest_dist:
                    closest_dist = dist
                    assigned_region = r

        if not assigned_region:
            assigned_region = "Pharloom"

        region_counts[assigned_region] += 1

        # Determine tier
        if cid in CORE_CATEGORIES:
            tier = "core"
        elif cid in OPTIONAL_SANITY_CATEGORIES:
            tier = "optional_sanity"
        elif cid in FILLER_CATEGORIES:
            tier = "filler"
        else:
            tier = "logic_obstacle"

        clean_item = {
            "id": loc.get("id"),
            "title": title,
            "category_id": cid,
            "category": cat_meta["name"],
            "group": cat_meta["group"],
            "region": assigned_region,
            "tier": tier,
            "description": desc,
            "latitude": lat,
            "longitude": lon,
        }
        processed_locations.append(clean_item)

    # 3. Export structured files
    # A. Categories
    cat_summary = []
    for cid, meta in sorted(CATEGORY_MAPPING.items(), key=lambda x: (x[1]["group"], x[1]["name"])):
        count = sum(1 for l in processed_locations if l["category_id"] == cid)
        cat_summary.append({
            "category_id": cid,
            "name": meta["name"],
            "group": meta["group"],
            "count": count,
        })
    with open(base_dir / "categories.json", "w", encoding="utf-8") as f:
        json.dump(cat_summary, f, indent=2, ensure_ascii=False)

    # B. Regions
    regions_list = []
    for reg, centroid in sorted(region_centroids.items()):
        total_locs = region_counts[reg]
        core_locs = sum(1 for l in processed_locations if l["region"] == reg and l["tier"] == "core")
        benches = [l["title"] for l in processed_locations if l["region"] == reg and l["category_id"] == 13533]
        bellways = [l["title"] for l in processed_locations if l["region"] == reg and l["category_id"] in (13535, 13539)]
        regions_list.append({
            "name": reg,
            "center_latitude": centroid["latitude"],
            "center_longitude": centroid["longitude"],
            "total_locations": total_locs,
            "core_checks": core_locs,
            "benches": benches,
            "fast_travel": bellways,
        })
    with open(base_dir / "regions.json", "w", encoding="utf-8") as f:
        json.dump(regions_list, f, indent=2, ensure_ascii=False)

    # C. Split by Tier
    core_checks = [l for l in processed_locations if l["tier"] == "core"]
    optional_checks = [l for l in processed_locations if l["tier"] == "optional_sanity"]
    filler_checks = [l for l in processed_locations if l["tier"] == "filler"]
    obstacle_checks = [l for l in processed_locations if l["tier"] == "logic_obstacle"]

    with open(base_dir / "checks_core.json", "w", encoding="utf-8") as f:
        json.dump(core_checks, f, indent=2, ensure_ascii=False)

    with open(base_dir / "checks_optional.json", "w", encoding="utf-8") as f:
        json.dump(optional_checks, f, indent=2, ensure_ascii=False)

    with open(base_dir / "checks_filler.json", "w", encoding="utf-8") as f:
        json.dump(filler_checks, f, indent=2, ensure_ascii=False)

    with open(base_dir / "logic_obstacles.json", "w", encoding="utf-8") as f:
        json.dump(obstacle_checks, f, indent=2, ensure_ascii=False)

    # D. Items Catalog
    items_catalog: dict[str, dict] = {}
    for l in core_checks:
        t = l["title"]
        cat = l["category"]
        if t not in items_catalog:
            # Determine classification
            if cat in ("Ability", "Crest", "Silk Skill", "Quest Item"):
                classification = "Progression"
            elif cat in ("Tool", "Upgrade", "Mask Shard", "Spool Fragment", "Silk Heart", "Memory Locket"):
                classification = "Useful"
            else:
                classification = "Filler"

            items_catalog[t] = {
                "name": t,
                "category": cat,
                "group": l["group"],
                "classification": classification,
                "count": 0,
            }
        items_catalog[t]["count"] += 1

    catalog_list = sorted(items_catalog.values(), key=lambda x: (x["category"], x["name"]))
    with open(base_dir / "items_catalog.json", "w", encoding="utf-8") as f:
        json.dump(catalog_list, f, indent=2, ensure_ascii=False)

    print(f"Summary:")
    print(f"  - Core Checks: {len(core_checks)}")
    print(f"  - Optional Sanity Checks: {len(optional_checks)}")
    print(f"  - Filler Pickups: {len(filler_checks)}")
    print(f"  - Logic Obstacles/Gates: {len(obstacle_checks)}")
    print(f"  - Unique Items Cataloged: {len(catalog_list)}")
    print("Files successfully generated in cartography/")


if __name__ == "__main__":
    main()
