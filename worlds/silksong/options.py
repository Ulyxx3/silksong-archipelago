from dataclasses import dataclass

from Options import Choice, DefaultOnToggle, PerGameCommonOptions, Toggle


# ──────────────────────────────────────────────────────────────────────────────
# Goal & Victory Condition
# ──────────────────────────────────────────────────────────────────────────────
class Goal(Choice):
    """Victory condition required to complete the seed.

    - Act 2: Defeat either the Phantom or the Last Judge to reach Act 2.
    - Act 3: Complete Act 2 and reach Act 3.
    - True Ending: Defeat Lost Lace in Act 3.
    """

    display_name = "Goal"
    option_act_2 = 0
    option_act_3 = 1
    option_true_ending = 2
    default = 2


# ──────────────────────────────────────────────────────────────────────────────
# Core Randomizer Options (Enabled by default — Core gameplay experience)
# ──────────────────────────────────────────────────────────────────────────────
class RandomizeAbilities(DefaultOnToggle):
    """Randomize Hornet's movement abilities (Swift Step, Cling Grip, Drifter's Cloak, etc.).

    CRITICAL: This is the core Metroidvania progression experience.
    """

    display_name = "Randomize Abilities (Core)"


class RandomizeTools(DefaultOnToggle):
    """Randomize tool badges and accessories found throughout Pharloom.

    Core gameplay check category.
    """

    display_name = "Randomize Tools (Core)"


class RandomizeSkills(DefaultOnToggle):
    """Randomize silk skills (Silkspear, Thread Storm, Cross Stitch, etc.).

    Core gameplay check category.
    """

    display_name = "Randomize Silk Skills (Core)"


class RandomizeCrests(DefaultOnToggle):
    """Randomize Hornet's equipment crests (Hunter, Reaper, Wanderer, etc.).

    Core gameplay check category.
    """

    display_name = "Randomize Crests (Core)"


class RandomizeBosses(DefaultOnToggle):
    """Randomize item rewards upon defeating major and minor bosses.

    Core gameplay check category.
    """

    display_name = "Randomize Bosses (Core)"


class RandomizeWishes(DefaultOnToggle):
    """Randomize quest and wish board rewards.

    Core gameplay check category.
    """

    display_name = "Randomize Wishes (Core)"


class RandomizeFleas(DefaultOnToggle):
    """Randomize Lost Flea rescue locations.

    Core gameplay check category (analogous to Grubs).
    """

    display_name = "Randomize Lost Fleas (Core)"


class RandomizeUpgrades(DefaultOnToggle):
    """Randomize Mask Shards, Spool Fragments, Silk Hearts, and Memory Lockets.

    Core gameplay check category.
    """

    display_name = "Randomize Upgrades (Core)"


class RandomizeVendors(DefaultOnToggle):
    """Randomize vendor shop inventories (Forge Daughter, Pebb, Shakra, etc.).

    Core gameplay check category.
    """

    display_name = "Randomize Vendors (Core)"


class RandomizeCollectibles(DefaultOnToggle):
    """Randomize unique relics (Rune Harps, Bone Scrolls, Weaver Effigies, etc.).

    Core gameplay check category.
    """

    display_name = "Randomize Collectibles (Core)"


# ──────────────────────────────────────────────────────────────────────────────
# Optional Sanity Options (Disabled by default)
# ──────────────────────────────────────────────────────────────────────────────
class BenchSanity(Toggle):
    """Randomize benches across Pharloom as check locations."""

    display_name = "Benchsanity"


class FastTravelSanity(Toggle):
    """Randomize Bellway Stations and Ventrica Stations as check locations."""

    display_name = "Fast Travel Sanity"


class MapSanity(Toggle):
    """Randomize Area Maps as check locations."""

    display_name = "Mapsanity"


class LoreSanity(Toggle):
    """Randomize Lore Tablets as check locations."""

    display_name = "Loresanity"


class JournalSanity(Toggle):
    """Randomize Hunter's Journal enemy encounter entries as check locations."""

    display_name = "Hunter's Journal Sanity"


@dataclass
class SilksongOptions(PerGameCommonOptions):
    """Options dataclass for Silksong."""

    goal: Goal

    # Core checks (Enabled by default)
    randomize_abilities: RandomizeAbilities
    randomize_tools: RandomizeTools
    randomize_skills: RandomizeSkills
    randomize_crests: RandomizeCrests
    randomize_bosses: RandomizeBosses
    randomize_wishes: RandomizeWishes
    randomize_fleas: RandomizeFleas
    randomize_upgrades: RandomizeUpgrades
    randomize_vendors: RandomizeVendors
    randomize_collectibles: RandomizeCollectibles

    # Optional sanities (Disabled by default)
    bench_sanity: BenchSanity
    fast_travel_sanity: FastTravelSanity
    map_sanity: MapSanity
    lore_sanity: LoreSanity
    journal_sanity: JournalSanity

