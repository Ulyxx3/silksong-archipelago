"""Stand-alone Seed Generator for Silksong Archipelago.

Generates a fully randomized, beaten-verified Archipelago multiworld seed
and exports both the .archipelago server package and a playthrough spoiler log.
"""

from __future__ import annotations

import argparse
from datetime import datetime
from pathlib import Path
import random
import sys
import zlib

ap_path = Path(r"c:\Users\Ulysse\Documents\GitHub\Archipelago")
my_repo = Path(__file__).resolve().parent

sys.path.insert(0, str(ap_path))
sys.path.insert(0, str(my_repo))

import worlds
if str(my_repo / "worlds") not in worlds.__path__:
    worlds.__path__.append(str(my_repo / "worlds"))

from BaseClasses import CollectionState, MultiWorld
from Fill import distribute_items_restrictive
import NetUtils
from Utils import parse_yaml, restricted_dumps, version_tuple
from worlds.silksong.options import SilksongOptions
from worlds.silksong.world import SilksongWorld


def generate_seed(yaml_path: Path, seed_num: int | None = None) -> tuple[Path, Path]:
    """Generate a Silksong seed from a player YAML file."""
    if seed_num is None:
        seed_num = random.randint(100_000, 999_999)

    print(f"--- Generating Silksong Seed {seed_num} ---")
    print(f"Using player settings from: {yaml_path}")

    with open(yaml_path, "r", encoding="utf-8") as f:
        yaml_content = parse_yaml(f.read())

    player_name = yaml_content.get("name", "Hornet")
    silksong_settings = yaml_content.get("Silksong", {})

    mw = MultiWorld(1)
    mw.game = {1: "Silksong"}
    mw.player_name = {1: player_name}
    mw.set_seed(seed_num)

    world = SilksongWorld(mw, 1)

    # Resolve options from YAML
    options_dict = {}
    for key, option_type in SilksongOptions.type_hints.items():
        if key in silksong_settings:
            options_dict[key] = option_type.from_any(silksong_settings[key])
        else:
            options_dict[key] = option_type.from_any(option_type.default)

    world.options = SilksongOptions(**options_dict)
    mw.worlds[1] = world

    print("Building regions and items...")
    world.generate_early()
    world.create_regions()
    world.create_items()
    world.set_rules()

    mw.state = CollectionState(mw)
    print("Running restrictive item distribution fill...")
    distribute_items_restrictive(mw)

    # Verification: Ensure completion condition is reachable
    test_state = CollectionState(mw)
    for loc in mw.get_locations(1):
        if loc.item:
            test_state.collect(loc.item)

    is_beatable = mw.completion_condition[1](test_state)
    assert is_beatable, "Generated seed must be beatable with all available items!"
    print("Verification passed: Victory condition is beatable!")

    # Prepare Multidata package
    locations_data = {1: {}}
    for loc in mw.get_locations(1):
        if loc.address is not None and loc.item:
            locations_data[1][loc.address] = (loc.item.code, loc.item.player, loc.item.flags)

    slot_data = {1: world.fill_slot_data()}
    slot_info = {1: NetUtils.NetworkSlot(player_name, "Silksong", NetUtils.SlotType.player)}

    multidata: NetUtils.MultiData = {
        "slot_data": slot_data,
        "slot_info": slot_info,
        "connect_names": {player_name: (0, 1)},
        "locations": locations_data,
        "checks_in_area": {},
        "server_options": {},
        "er_hint_data": {1: {}},
        "precollected_items": {1: []},
        "precollected_hints": {1: []},
        "version": (version_tuple.major, version_tuple.minor, version_tuple.build),
        "tags": ["AP"],
        "minimum_versions": {"server": (0, 5, 0), 1: (0, 5, 0)},
        "seed_name": mw.seed_name,
        "spheres": [],
        "datapackage": {
            "Silksong": {
                "item_name_to_id": world.item_name_to_id,
                "location_name_to_id": world.location_name_to_id,
                "item_name_groups": getattr(world, "item_name_groups", {}),
                "checksum": "1",
                "version": 1,
            }
        },
        "race_mode": 0,
    }

    # Save .archipelago server file
    archipelago_file = my_repo / f"AP_{seed_num}_{player_name}.archipelago"
    compressed_multidata = zlib.compress(restricted_dumps(multidata), 9)
    with open(archipelago_file, "wb") as f:
        f.write(bytes([3]))
        f.write(compressed_multidata)

    # Save human-readable spoiler log
    spoiler_file = my_repo / f"AP_{seed_num}_{player_name}_spoiler.txt"
    with open(spoiler_file, "w", encoding="utf-8") as f:
        f.write(f"Silksong Archipelago Seed {seed_num} Spoiler Log\n")
        f.write(f"Generated at: {datetime.now().isoformat()}\n")
        f.write(f"Player: {player_name}\n")
        f.write(f"Goal: {world.options.goal.current_key}\n")
        f.write("=" * 60 + "\n\n")

        # Group placements by region
        region_placements: dict[str, list[tuple[str, str, str]]] = {}
        for loc in sorted(mw.get_locations(1), key=lambda x: x.name):
            if loc.item:
                r_name = loc.parent_region.name if loc.parent_region else "Unknown"
                region_placements.setdefault(r_name, []).append(
                    (loc.name, loc.item.name, loc.item.classification.name)
                )

        for r_name, placements in sorted(region_placements.items()):
            f.write(f"[{r_name}]\n")
            for loc_name, item_name, class_name in placements:
                f.write(f"  - {loc_name}: {item_name} ({class_name})\n")
            f.write("\n")

    print(f"Generated server package: {archipelago_file.name}")
    print(f"Generated spoiler log:    {spoiler_file.name}")
    return archipelago_file, spoiler_file


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate a Silksong Archipelago seed.")
    parser.add_argument("--yaml", type=Path, default=my_repo / "worlds" / "silksong" / "Silksong.yaml",
                        help="Path to player YAML settings file.")
    parser.add_argument("--seed", type=int, default=None, help="Optional numeric seed.")
    args = parser.parse_args()

    generate_seed(args.yaml, args.seed)


if __name__ == "__main__":
    main()
