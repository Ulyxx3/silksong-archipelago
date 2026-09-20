from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Region

from . import items
from .data.regions_data import REGION_CONNECTIONS
from .locations import SilksongLocation

if TYPE_CHECKING:
    from .world import SilksongWorld


# ──────────────────────────────────────────────────────────────────────────────
# Region creation & wiring
# ──────────────────────────────────────────────────────────────────────────────
def create_and_connect_regions(world: SilksongWorld) -> None:
    """Create all regions, connect them, then add the victory event."""
    create_all_regions(world)
    connect_regions(world)
    create_victory_event(world)


def create_all_regions(world: SilksongWorld) -> None:
    """Instantiate every region and submit them to the multiworld."""
    # Origin region (Menu)
    menu = Region("Menu", world.player, world.multiworld)
    regions: list[Region] = [menu]

    # All Pharloom regions
    for region_name in REGION_CONNECTIONS:
        regions.append(Region(region_name, world.player, world.multiworld))

    # IMPORTANT: always use += , never = .
    world.multiworld.regions += regions


def connect_regions(world: SilksongWorld) -> None:
    """Wire up entrances between regions."""
    # Connect Menu to the user's selected starting region: Mosslands
    menu = world.get_region("Menu")
    mosslands = world.get_region("Mosslands")
    menu.connect(mosslands, "Menu -> Mosslands")

    # Connect all adjacent regions
    for source_name, targets in REGION_CONNECTIONS.items():
        source_reg = world.get_region(source_name)
        for target_name in targets:
            target_reg = world.get_region(target_name)
            entrance_name = f"{source_name} -> {target_name}"
            # Region.connect creates and attaches the entrance
            source_reg.connect(target_reg, entrance_name)


def create_victory_event(world: SilksongWorld) -> None:
    """Place victory events and goal completion triggers."""
    # Grand Gate: Act 2 milestones
    grand_gate = world.get_region("Grand Gate")
    grand_gate.add_event(
        "Defeat Phantom",
        "Defeat Phantom",
        location_type=SilksongLocation,
        item_type=items.SilksongItem,
    )
    grand_gate.add_event(
        "Defeat Last Judge",
        "Defeat Last Judge",
        location_type=SilksongLocation,
        item_type=items.SilksongItem,
    )

    # High Halls: Act 3 milestone
    high_halls = world.get_region("High Halls")
    high_halls.add_event(
        "Arrive in Act 3",
        "Arrive in Act 3",
        location_type=SilksongLocation,
        item_type=items.SilksongItem,
    )

    # The Cradle: True Ending milestone
    cradle = world.get_region("The Cradle")
    cradle.add_event(
        "Defeat Lost Lace",
        "Defeat Lost Lace",
        location_type=SilksongLocation,
        item_type=items.SilksongItem,
    )
    cradle.add_event(
        "Victory",
        "Victory",
        location_type=SilksongLocation,
        item_type=items.SilksongItem,
    )

