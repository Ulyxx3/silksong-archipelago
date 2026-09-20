from __future__ import annotations

from typing import TYPE_CHECKING

from worlds.generic.Rules import set_rule

from .data.locations_data import ALL_LOCATIONS
from .data.regions_data import REGION_CONNECTIONS
from .options import Goal

if TYPE_CHECKING:
    from .world import SilksongWorld


# ──────────────────────────────────────────────────────────────────────────────
# Access rules & Victory conditions
# ──────────────────────────────────────────────────────────────────────────────
def set_all_rules(world: SilksongWorld) -> None:
    """Apply access rules to regions, entrances, and locations."""
    _set_entrance_rules(world)
    _set_location_rules(world)
    _set_completion_condition(world)


def _set_entrance_rules(world: SilksongWorld) -> None:
    """Apply requirement rules to region transitions."""
    player = world.player

    for source_name, targets in REGION_CONNECTIONS.items():
        for target_name, req in targets.items():
            if not req:
                continue

            entrance_name = f"{source_name} -> {target_name}"
            try:
                entrance = world.get_entrance(entrance_name)
            except KeyError:
                continue

            if req == "Rescued 20 Fleas":
                set_rule(entrance, lambda state, p=player: state.has("Lost Flea", p, 20))
            elif req == "Defeat Phantom":
                set_rule(entrance, lambda state, p=player: state.has("Defeat Phantom", p))
            elif req == "Defeat Last Judge":
                set_rule(entrance, lambda state, p=player: state.has("Defeat Last Judge", p))
            else:
                set_rule(entrance, lambda state, p=player, item=req: state.has(item, p))


def _set_location_rules(world: SilksongWorld) -> None:
    """Apply ability and key requirements to individual check locations."""
    player = world.player

    for loc in ALL_LOCATIONS:
        reqs = loc.get("requirements", [])
        if not reqs:
            continue

        try:
            location_obj = world.get_location(loc["name"])
        except KeyError:
            # Location is disabled by user options
            continue

        set_rule(location_obj, lambda state, p=player, r=reqs: state.has_all(r, p))


def _set_completion_condition(world: SilksongWorld) -> None:
    """Set the multiworld completion condition according to the chosen Goal option."""
    goal = world.options.goal

    if goal == Goal.option_act_2:
        # Goal: Arrive in Act 2 (Defeat Phantom or Last Judge)
        world.multiworld.completion_condition[world.player] = (
            lambda state: state.has("Defeat Phantom", world.player)
            or state.has("Defeat Last Judge", world.player)
            or state.has("Victory", world.player)
        )
    elif goal == Goal.option_act_3:
        # Goal: Arrive in Act 3
        world.multiworld.completion_condition[world.player] = (
            lambda state: state.has("Arrive in Act 3", world.player)
            or state.has("Victory", world.player)
        )
    else:
        # Goal: True Ending (Defeat Lost Lace)
        world.multiworld.completion_condition[world.player] = (
            lambda state: state.has("Defeat Lost Lace", world.player)
            or state.has("Victory", world.player)
        )


