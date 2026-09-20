from BaseClasses import Tutorial
from worlds.AutoWorld import WebWorld


class SilksongWebWorld(WebWorld):
    """WebWorld subclass governing how Silksong appears on the Archipelago webhost."""

    theme = "jungle"

    bug_report_page = "https://github.com/Ulyxx3/silksong-archipelago/issues"

    setup_en = Tutorial(
        "Multiworld Setup Guide",
        "A guide to setting up Silksong for Archipelago Multiworld.",
        "English",
        "setup_en.md",
        "setup/en",
        ["Ulyxx3"],
    )

    tutorials = [setup_en]
