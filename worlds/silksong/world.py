from collections.abc import Mapping
from typing import Any

from worlds.AutoWorld import World

from . import items, locations, regions, rules, web_world
from . import options as silksong_options


class SilksongWorld(World):
    """Silksong is a Metroidvania action-adventure game by Team Cherry,
    sequel to Hollow Knight. Explore a vast new kingdom as Hornet,
    princess-protector of Hallownest, in a bug-filled land of silk and song.
    """

    # ── Core identifiers ────────────────────────────────────────────────
    game = "Silksong"
    web = web_world.SilksongWebWorld()

    # ── Options ─────────────────────────────────────────────────────────
    options_dataclass = silksong_options.SilksongOptions
    options: silksong_options.SilksongOptions

    # ── Name-to-ID mappings (required by AutoWorldRegister) ─────────────
    item_name_to_id = items.ITEM_NAME_TO_ID
    location_name_to_id = locations.LOCATION_NAME_TO_ID

    # ── Origin region ───────────────────────────────────────────────────
    # "Menu" is the default; override if needed later.
    # origin_region_name = "Menu"

    # ══════════════════════════════════════════════════════════════════════
    # Generation steps (called in order by Main.py)
    # ══════════════════════════════════════════════════════════════════════
    def create_regions(self) -> None:
        regions.create_and_connect_regions(self)
        locations.create_all_locations(self)

    def set_rules(self) -> None:
        rules.set_all_rules(self)

    def create_items(self) -> None:
        items.create_all_items(self)

    # ══════════════════════════════════════════════════════════════════════
    # Item creation (must be callable at any time, even with self.world = None)
    # ══════════════════════════════════════════════════════════════════════
    def create_item(self, name: str) -> items.SilksongItem:
        return items.create_item(self, name)

    def get_filler_item_name(self) -> str:
        return items.get_filler_item_name(self)

    # ══════════════════════════════════════════════════════════════════════
    # Slot data — sent to the client on connection
    # ══════════════════════════════════════════════════════════════════════
    def fill_slot_data(self) -> Mapping[str, Any]:
        """Expose options and goal configuration to the game client upon connection."""
        return {
            "goal": self.options.goal.value,
            "randomize_abilities": bool(self.options.randomize_abilities.value),
            "randomize_tools": bool(self.options.randomize_tools.value),
            "randomize_skills": bool(self.options.randomize_skills.value),
            "randomize_crests": bool(self.options.randomize_crests.value),
            "randomize_bosses": bool(self.options.randomize_bosses.value),
            "randomize_wishes": bool(self.options.randomize_wishes.value),
            "randomize_fleas": bool(self.options.randomize_fleas.value),
            "randomize_upgrades": bool(self.options.randomize_upgrades.value),
            "randomize_vendors": bool(self.options.randomize_vendors.value),
            "randomize_collectibles": bool(self.options.randomize_collectibles.value),
            "bench_sanity": bool(self.options.bench_sanity.value),
            "fast_travel_sanity": bool(self.options.fast_travel_sanity.value),
            "map_sanity": bool(self.options.map_sanity.value),
            "lore_sanity": bool(self.options.lore_sanity.value),
            "journal_sanity": bool(self.options.journal_sanity.value),
        }

