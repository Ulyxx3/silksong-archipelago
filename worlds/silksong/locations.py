from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Location

from .data.locations_data import ALL_LOCATIONS, LOCATION_NAME_TO_ID

if TYPE_CHECKING:
    from .world import SilksongWorld


# ──────────────────────────────────────────────────────────────────────────────
# Location subclass
# ──────────────────────────────────────────────────────────────────────────────
class SilksongLocation(Location):
    game = "Silksong"


# ──────────────────────────────────────────────────────────────────────────────
# Filtering and Creation
# ──────────────────────────────────────────────────────────────────────────────
def is_location_enabled(world: SilksongWorld, loc_data: dict) -> bool:
    """Check whether a location is enabled based on active world options."""
    cat = loc_data["category"]
    opt = world.options

    # Core check options
    if cat == "Ability" and not opt.randomize_abilities:
        return False
    if cat == "Tool" and not opt.randomize_tools:
        return False
    if cat == "Silk Skill" and not opt.randomize_skills:
        return False
    if cat == "Crest" and not opt.randomize_crests:
        return False
    if cat == "Boss" and not opt.randomize_bosses:
        return False
    if cat == "Wish" and not opt.randomize_wishes:
        return False
    if cat == "Lost Flea" and not opt.randomize_fleas:
        return False
    if (
        cat in ("Mask Shard", "Spool Fragment", "Silk Heart", "Memory Locket", "Upgrade")
        and not opt.randomize_upgrades
    ):
        return False
    if cat == "Vendor" and not opt.randomize_vendors:
        return False
    if (
        cat in (
            "Quest Item",
            "Craftmetal",
            "Pale Oil",
            "Rune Harp",
            "Bone Scroll",
            "Choral Commandment",
            "Weaver Effigy",
            "Psalm Cylinder",
            "Memento",
            "Arcane Egg",
        )
        and not opt.randomize_collectibles
    ):
        return False

    # Optional sanity options
    if cat == "Bench" and not opt.bench_sanity:
        return False
    if cat in ("Bellway Station", "Ventrica Station") and not opt.fast_travel_sanity:
        return False
    if cat == "Area Map" and not opt.map_sanity:
        return False
    if cat == "Lore" and not opt.lore_sanity:
        return False
    if cat == "Journal Entry" and not opt.journal_sanity:
        return False

    return True


def create_all_locations(world: SilksongWorld) -> None:
    """Create all active locations and add them to their respective regions."""
    locations_by_region: dict[str, dict[str, int]] = {}

    for loc in ALL_LOCATIONS:
        if not is_location_enabled(world, loc):
            continue

        region_name = loc["region"]
        if region_name not in locations_by_region:
            locations_by_region[region_name] = {}

        locations_by_region[region_name][loc["name"]] = loc["id"]

    for region_name, loc_dict in locations_by_region.items():
        try:
            region = world.get_region(region_name)
            region.add_locations(loc_dict, SilksongLocation)
        except KeyError:
            # Fallback to starting region if region not found
            fallback = world.get_region("Mosslands")
            fallback.add_locations(loc_dict, SilksongLocation)

