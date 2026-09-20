# Silksong

## Where is the options page?

The [player options page for this game](../player-options) contains
all the options you need to configure and export a YAML file.

## What does randomization do to this game?

Silksong Archipelago randomizes key abilities, tools, crests, skills, collectibles, and upgrades across the
expansive kingdom of Pharloom. Items normally collected in specific areas or dropped by bosses are shuffled
into the multiworld item pool, meaning you can find items for other players and receive your own upgrades
from other games.

## What items and locations get shuffled?

The randomizer splits checks into **Core Checks** (enabled by default) and **Sanity Checks** (optional):

### Core Pool (Default)
- **Abilities & Movement**: Swift Step, Cling Grip, Drifter's Cloak, Faydown Cloak, Needolin, Silk Soar.
- **Silk Skills**: Thread Storm, Cross Stitch, Sharp Dart, Pale Nails, Weaver's Needle.
- **Crests**: Hunter Crest, Reaper Crest, Weaver Crest, Architect Crest, Witch Crest.
- **Rosary & Tool Slots**: Tool Pouches, Pouch Expansions, Crest Attunement Slots.
- **Combat Tools**: Sting Shards, Pimpillo Bombs, Tri-Pins, Thread Traps, Barbed Needle.
- **Boss Checks**: Major bosses throughout Pharloom (Moss Mother, Bell Beast, Lace, Last Judge, etc.).
- **Collectibles**: Fleas, Spool Shards, Mask Shards, Rosary Strings, Crafting Shards.

### Optional Sanities
- **Benchsanity**: Benches act as checks and must be unlocked.
- **Fast Travel / Bell Stations**: Bell Towers, Bell Beast stations, and Tube transit checks.
- **Mapsanity**: Area map pickups from Shakra.
- **Loresanity**: Lore tablets, historical carvings, and ancient inscriptions.
- **Hunter's Journal**: Defeating distinct enemies adds checks to your world.

## What are the Victory Conditions?

You can choose your objective via the `goal` option in your YAML configuration:
1. **Act 2 Arrival (`act2_arrival`)**: Defeat either the Phantom in Greymoor or the Last Judge at Blistering Peak.
2. **Act 3 Arrival (`act3_arrival`)**: Reach the upper citadel and breach the higher sanctum.
3. **True Ending / Lost Lace (`lost_lace_true_ending`)** *(Default)*: Conquer the depths and defeat Lost Lace.
