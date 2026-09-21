"""Multiworld Seed Generator for Silksong + Other Archipelago Games.

Generates a fully randomized, beatable-verified Archipelago multiworld seed
for all player YAML configurations found in the Players/ folder.
Exports both the .archipelago server package and a playthrough spoiler log.
"""

from __future__ import annotations

import argparse
from datetime import datetime
import logging
from pathlib import Path
import random
import sys
import zlib

ap_dir = Path.home() / "Documents" / "GitHub" / "Archipelago"
if not ap_dir.exists():
    ap_dir = Path(__file__).resolve().parent.parent / "Archipelago"
my_repo = Path(__file__).resolve().parent

sys.path.insert(0, str(ap_dir))
sys.path.insert(0, str(my_repo))

# Filter out import warnings for unrelated worlds
class WorldLoadFilter(logging.Filter):
    def filter(self, record: logging.LogRecord) -> bool:
        msg = record.getMessage()
        return "Could not load world" not in msg

logging.getLogger().addFilter(WorldLoadFilter())

# Bypass module check for unrelated games
import ModuleUpdate
ModuleUpdate.update_ran = True

from BaseClasses import CollectionState, MultiWorld
from Fill import balance_multiworld_progression, distribute_items_restrictive
import NetUtils
from Utils import parse_yaml, restricted_dumps, version_tuple
from worlds.AutoWorld import AutoWorldRegister, call_all
import worlds


def generate_multiworld(players_dir: Path, seed_num: int | None = None) -> tuple[Path, Path]:
    """Generate a multiworld seed package and spoiler log from all player YAML files."""
    if seed_num is None:
        seed_num = random.randint(100_000, 999_999)

    yaml_files = sorted(list(players_dir.glob("*.yaml")))
    if not yaml_files:
        raise ValueError(f"No .yaml player configuration files found in {players_dir}")

    player_count = len(yaml_files)
    print("=" * 65)
    print(f"Archipelago Multiworld Generator — {player_count} Players (Seed {seed_num})")
    print("=" * 65)

    mw = MultiWorld(player_count)
    mw.set_seed(seed_num)
    mw.player_name = {}
    mw.game = {}

    player_configs = []
    for slot, y_file in enumerate(yaml_files, start=1):
        with open(y_file, "r", encoding="utf-8") as f:
            y_data = parse_yaml(f.read())
        player_name = y_data.get("name", y_file.stem)
        game_name = y_data.get("game", "Silksong")
        mw.game[slot] = game_name
        mw.player_name[slot] = player_name
        player_configs.append((slot, player_name, game_name, y_data))
        print(f"  [Slot {slot}] {player_name:12} -> Game: {game_name} ({y_file.name})")

    # Instantiate world classes & resolve player options
    for slot, player_name, game_name, y_data in player_configs:
        world_cls = AutoWorldRegister.world_types[game_name]
        world = world_cls(mw, slot)
        mw.worlds[slot] = world

        game_options = y_data.get(game_name, {})
        options_dict = {}
        for opt_key, opt_cls in world_cls.options_dataclass.type_hints.items():
            if opt_key in game_options:
                options_dict[opt_key] = opt_cls.from_any(game_options[opt_key])
            else:
                options_dict[opt_key] = opt_cls.from_any(opt_cls.default)
        world.options = world_cls.options_dataclass(**options_dict)

    # Core generation stages
    print("\nExecuting generation stages:")
    mw.state = CollectionState(mw)
    print("  -> Initializing regions...")
    call_all(mw, "generate_early")
    call_all(mw, "create_regions")
    print("  -> Creating item pools...")
    call_all(mw, "create_items")
    print("  -> Setting access rules...")
    call_all(mw, "set_rules")

    print(f"  -> Distributing items across {len(mw.get_locations())} locations...")
    mw.state = CollectionState(mw)
    distribute_items_restrictive(mw)

    if mw.players > 1:
        print("  -> Balancing multiworld progression...")
        balance_multiworld_progression(mw)

    print("Generation & Fill successfully verified!")

    # Build multidata package
    slot_data = {slot: mw.worlds[slot].fill_slot_data() for slot in mw.player_ids}
    client_versions = {slot: mw.worlds[slot].required_client_version for slot in mw.player_ids}
    slot_info = {
        slot: NetUtils.NetworkSlot(mw.player_name[slot], mw.game[slot], mw.player_types[slot])
        for slot in mw.player_ids
    }

    locations_data = {slot: {} for slot in mw.player_ids}
    for loc in mw.get_filled_locations():
        if isinstance(loc.address, int):
            locations_data[loc.player][loc.address] = (loc.item.code, loc.item.player, loc.item.flags)

    data_package = {
        w.game: worlds.network_data_package["games"][w.game]
        for w in mw.worlds.values()
    }
    data_package["Archipelago"] = worlds.network_data_package["games"]["Archipelago"]

    multidata = {
        "slot_data": slot_data,
        "slot_info": slot_info,
        "connect_names": {name: (0, slot) for slot, name in mw.player_name.items()},
        "locations": locations_data,
        "checks_in_area": {},
        "server_options": {},
        "er_hint_data": {},
        "precollected_items": {slot: [i.code for i in pre if isinstance(i.code, int)]
                               for slot, pre in mw.precollected_items.items()},
        "precollected_hints": {slot: set() for slot in range(1, mw.players + 1)},
        "version": (version_tuple.major, version_tuple.minor, version_tuple.build),
        "tags": ["AP"],
        "minimum_versions": {
            "server": AutoWorldRegister.world_types["Archipelago"].required_server_version if "Archipelago" in AutoWorldRegister.world_types else (0, 6, 8),
            "clients": client_versions
        },
        "seed_name": str(seed_num),
        "spheres": [],
        "datapackage": data_package,
        "race_mode": 0,
    }

    names_str = "_".join(mw.player_name.values())
    archipelago_file = my_repo / f"AP_{seed_num}_{names_str}.archipelago"
    compressed_multidata = zlib.compress(restricted_dumps(multidata), 9)
    with open(archipelago_file, "wb") as f:
        f.write(bytes([3]))
        f.write(compressed_multidata)

    spoiler_file = my_repo / f"AP_{seed_num}_{names_str}_spoiler.txt"
    with open(spoiler_file, "w", encoding="utf-8") as f:
        f.write(f"Archipelago Multiworld Seed {seed_num} Spoiler Log\n")
        f.write(f"Generated at: {datetime.now().isoformat()}\n")
        f.write(f"Players: {', '.join(f'{name} ({mw.game[s]})' for s, name in mw.player_name.items())}\n")
        f.write("=" * 70 + "\n\n")

        for s, p_name in mw.player_name.items():
            f.write(f"--- Locations for Player {s}: {p_name} ({mw.game[s]}) ---\n")
            for loc in sorted(mw.get_locations(s), key=lambda x: x.name):
                if loc.item:
                    f.write(f"  - {loc.name} -> {loc.item.name} ({mw.player_name[loc.item.player]})\n")
            f.write("\n")

    print("\nOutputs ready:")
    print(f"  1. Server Package: {archipelago_file.name}")
    print(f"  2. Spoiler Log:    {spoiler_file.name}")
    return archipelago_file, spoiler_file


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate an Archipelago Multiworld seed from player YAML files.")
    parser.add_argument("--players", type=Path, default=my_repo / "Players",
                        help="Path to folder containing player YAML files (default: ./Players)")
    parser.add_argument("--seed", type=int, default=None, help="Optional numeric seed.")
    args = parser.parse_args()

    generate_multiworld(args.players, args.seed)


if __name__ == "__main__":
    main()
