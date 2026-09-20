from __future__ import annotations

import random
from typing import TYPE_CHECKING

from BaseClasses import Item, ItemClassification

from .data.items_data import ALL_ITEMS, ITEM_NAME_TO_ID

if TYPE_CHECKING:
    from .world import SilksongWorld


# ──────────────────────────────────────────────────────────────────────────────
# Base ID
# ──────────────────────────────────────────────────────────────────────────────
SILKSONG_BASE_ID: int = 777_000


# ──────────────────────────────────────────────────────────────────────────────
# Default item classifications
# ──────────────────────────────────────────────────────────────────────────────
def _build_classifications() -> dict[str, ItemClassification]:
    res: dict[str, ItemClassification] = {}
    for name, item in ALL_ITEMS.items():
        cls_str = item.get("classification", "Filler")
        if cls_str == "Progression":
            res[name] = ItemClassification.progression
        elif cls_str == "Useful":
            res[name] = ItemClassification.useful
        else:
            res[name] = ItemClassification.filler
    return res


DEFAULT_ITEM_CLASSIFICATIONS: dict[str, ItemClassification] = _build_classifications()

FILLER_ITEMS: list[str] = [
    "Rosary String",
    "Shard Bundle",
    "Flea Brew",
    "Silkeater",
]


# ──────────────────────────────────────────────────────────────────────────────
# Item subclass
# ──────────────────────────────────────────────────────────────────────────────
class SilksongItem(Item):
    game = "Silksong"


# ──────────────────────────────────────────────────────────────────────────────
# Factory helpers
# ──────────────────────────────────────────────────────────────────────────────
def create_item(world: SilksongWorld, name: str) -> SilksongItem:
    """Create an item with the correct classification for this world."""
    classification = DEFAULT_ITEM_CLASSIFICATIONS.get(name, ItemClassification.filler)
    item_id = ITEM_NAME_TO_ID.get(name)
    return SilksongItem(name, classification, item_id, world.player)


def get_filler_item_name(world: SilksongWorld) -> str:
    """Return the name of a non-farmable filler item."""
    weights = [50, 40, 20, 10]
    return random.choices(FILLER_ITEMS, weights=weights, k=1)[0]


def create_all_items(world: SilksongWorld) -> None:
    """Build the complete item pool and submit it to the multiworld."""
    itempool: list[Item] = []

    # 1. Add unique progression and useful items from ALL_ITEMS
    for name, item_meta in ALL_ITEMS.items():
        cat = item_meta.get("category", "")
        # Skip generic filler entries during base creation (they are used for padding)
        if cat == "Filler":
            continue

        # Respect options for items
        opt = world.options
        if cat == "Ability" and not opt.randomize_abilities:
            continue
        if cat == "Tool" and not opt.randomize_tools:
            continue
        if cat == "Silk Skill" and not opt.randomize_skills:
            continue
        if cat == "Crest" and not opt.randomize_crests:
            continue
        if (
            cat in ("Mask Shard", "Spool Fragment", "Silk Heart", "Memory Locket", "Upgrade")
            and not opt.randomize_upgrades
        ):
            continue

        # Add item according to its count
        count = max(1, item_meta.get("count", 1))
        for _ in range(count):
            itempool.append(world.create_item(name))

    # 2. Balance item pool with unfilled locations: item_count == location_count
    unfilled = len(world.multiworld.get_unfilled_locations(world.player))

    if len(itempool) < unfilled:
        needed_filler = unfilled - len(itempool)
        for _ in range(needed_filler):
            filler_name = get_filler_item_name(world)
            itempool.append(world.create_item(filler_name))
    elif len(itempool) > unfilled:
        # If pool exceeds (e.g. some locations were toggled off), trim useful/filler items first
        excess = len(itempool) - unfilled
        trimmed = 0
        i = len(itempool) - 1
        while i >= 0 and trimmed < excess:
            if itempool[i].classification != ItemClassification.progression:
                itempool.pop(i)
                trimmed += 1
            i -= 1

    # IMPORTANT: always use += , never = .
    world.multiworld.itempool += itempool

