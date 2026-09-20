"""
Launcher for Archipelago Text Client.
Can be used to connect as any player (e.g. Madeline) to test multiworld without running the actual game.
Usage:
    py -3.13 run_client.py --name Madeline localhost:38281
"""
import sys
from pathlib import Path

archipelago_path = Path.home() / "Documents" / "GitHub" / "Archipelago"
if not archipelago_path.exists():
    archipelago_path = Path(__file__).resolve().parent.parent / "Archipelago"
if str(archipelago_path) not in sys.path:
    sys.path.insert(0, str(archipelago_path))

import logging

class WorldLoadFilter(logging.Filter):
    def filter(self, record: logging.LogRecord) -> bool:
        return "Could not load world" not in record.getMessage()

logging.getLogger().addFilter(WorldLoadFilter())

import ModuleUpdate
ModuleUpdate.update_ran = True

import CommonClient


if __name__ == "__main__":
    args = list(sys.argv[1:])
    for i, arg in enumerate(args):
        if not arg.startswith("-") and ":" in arg and "://" not in arg:
            args[i] = f"archipelago://{arg}"
    CommonClient.run_as_textclient(*args)

