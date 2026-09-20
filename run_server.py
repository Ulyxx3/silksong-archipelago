"""Launcher for Archipelago Server hosting Silksong games.

Automatically finds the latest generated .archipelago seed package
and runs Archipelago's MultiServer on port 38281 without requirement errors.
"""

from __future__ import annotations

import argparse
import asyncio
from pathlib import Path
import sys

ap_dir = Path(r"c:\Users\Ulysse\Documents\GitHub\Archipelago")
my_repo = Path(__file__).resolve().parent

sys.path.insert(0, str(ap_dir))
sys.path.insert(0, str(my_repo))

import logging
import socket

# Filter out import warnings for unrelated worlds
class WorldLoadFilter(logging.Filter):
    def filter(self, record: logging.LogRecord) -> bool:
        msg = record.getMessage()
        if "Could not load world" in msg:
            return False
        return True

logging.getLogger().addFilter(WorldLoadFilter())

# Bypass module check for unrelated games
import ModuleUpdate
ModuleUpdate.update_ran = True

import MultiServer


def is_port_in_use(port: int) -> bool:
    """Check if the given port is already occupied."""
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        return s.connect_ex(("127.0.0.1", port)) == 0


def find_latest_seed() -> Path | None:
    """Find the most recently created .archipelago seed file."""
    seeds = list(my_repo.glob("*.archipelago"))
    if not seeds:
        return None
    return max(seeds, key=lambda p: p.stat().st_mtime)


def main() -> None:
    parser = argparse.ArgumentParser(description="Run Archipelago server for Silksong.")
    parser.add_argument("seed", nargs="?", type=Path, default=None,
                        help="Path to .archipelago seed file (defaults to latest generated).")
    parser.add_argument("--port", type=int, default=38281, help="Port to listen on (default 38281).")
    args = parser.parse_args()

    if is_port_in_use(args.port):
        print(f"Error: Port {args.port} is already in use by another server or process!")
        print("To terminate any existing background Python process, run in PowerShell:")
        print("  Stop-Process -Name python3.13 -Force")
        sys.exit(1)

    seed_file = args.seed or find_latest_seed()
    if not seed_file or not seed_file.exists():
        print("Error: No .archipelago seed file found. Run 'py -3.13 generate_seed.py' first!")
        sys.exit(1)

    print(f"=== Starting Archipelago MultiServer on port {args.port} ===")
    print(f"Hosting seed: {seed_file.name}")
    print("Connect your Silksong game using:")
    print("  Host: localhost")
    print(f"  Port: {args.port}")
    print("  Slot: Hornet")
    print("=" * 60)

    sys.argv = ["MultiServer.py", str(seed_file), "--port", str(args.port)]
    server_args = MultiServer.parse_args()
    try:
        asyncio.run(MultiServer.main(server_args))
    except (KeyboardInterrupt, asyncio.exceptions.CancelledError):
        print("\nServer stopped.")


if __name__ == "__main__":
    main()
