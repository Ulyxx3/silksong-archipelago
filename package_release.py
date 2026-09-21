"""Release packaging script for Silksong Archipelago.

Generates:
1. dist/silksong.apworld - Standalone Archipelago world package for servers/generators.
2. dist/SilksongArchipelago_BepInEx.zip - Client release archive ready to extract into the game directory.
"""

from __future__ import annotations

import os
from pathlib import Path
import subprocess
import sys
import zipfile

ROOT = Path(__file__).resolve().parent
DIST_DIR = ROOT / "dist"
WORLD_DIR = ROOT / "worlds" / "silksong"
CS_DIR = ROOT / "SilksongArchipelago"
CS_RELEASE_BIN = CS_DIR / "bin" / "Release" / "netstandard2.1"


def build_csharp_release() -> bool:
    """Build the C# client in Release mode using dotnet CLI."""
    print("Building SilksongArchipelago C# client in Release mode...")
    result = subprocess.run(["dotnet", "build", "-c", "Release", str(CS_DIR)], capture_output=True, text=True)
    if result.returncode != 0:
        print(f"Error building C# client:\n{result.stderr}\n{result.stdout}")
        return False
    print("C# build succeeded.")
    return True


def package_apworld() -> Path:
    """Package the worlds/silksong directory into an Archipelago .apworld archive."""
    DIST_DIR.mkdir(parents=True, exist_ok=True)
    apworld_path = DIST_DIR / "silksong.apworld"

    print(f"Creating {apworld_path.name}...")
    with zipfile.ZipFile(apworld_path, "w", zipfile.ZIP_DEFLATED) as zf:
        for root, dirs, files in os.walk(WORLD_DIR):
            # Ignore cache directories
            dirs[:] = [d for d in dirs if d not in ("__pycache__", ".pytest_cache")]
            for file in files:
                if file.endswith((".pyc", ".pyo")):
                    continue
                file_path = Path(root) / file
                rel_path = file_path.relative_to(WORLD_DIR.parent)
                # Ensure archive paths use forward slashes
                arcname = str(rel_path).replace("\\", "/")
                zf.write(file_path, arcname)

    print(f"Successfully generated {apworld_path} ({apworld_path.stat().st_size:,} bytes).")
    return apworld_path


def package_bepinex_client() -> Path:
    """Package the compiled C# mod into a BepInEx plugin zip archive."""
    DIST_DIR.mkdir(parents=True, exist_ok=True)
    zip_path = DIST_DIR / "SilksongArchipelago_BepInEx.zip"

    # Core required client DLLs (DO NOT include Newtonsoft.Json.dll as the game provides its own)
    required_dlls = [
        "SilksongArchipelago.dll",
        "Archipelago.MultiClient.Net.dll",
    ]

    print(f"Creating {zip_path.name}...")
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
        for dll_name in required_dlls:
            dll_file = CS_RELEASE_BIN / dll_name
            if not dll_file.exists():
                # Fallback to Debug if not found in Release
                dll_file = CS_DIR / "bin" / "Debug" / "netstandard2.1" / dll_name
            if dll_file.exists():
                arcname = f"BepInEx/plugins/SilksongArchipelago/{dll_name}"
                zf.write(dll_file, arcname)
            else:
                print(f"Warning: {dll_name} was not found and skipped.")

        # Include icons and asset directories
        bin_dir = CS_RELEASE_BIN if CS_RELEASE_BIN.exists() else CS_DIR / "bin" / "Debug" / "netstandard2.1"
        icon_file = bin_dir / "ArchipelagoIcon.png"
        if icon_file.exists():
            zf.write(icon_file, "BepInEx/plugins/SilksongArchipelago/ArchipelagoIcon.png")

        check_icons_dir = bin_dir / "CheckIcons"
        if check_icons_dir.exists():
            for icon in check_icons_dir.glob("*.png"):
                zf.write(icon, f"BepInEx/plugins/SilksongArchipelago/CheckIcons/{icon.name}")

        # Include README/Setup instructions if present
        setup_doc = WORLD_DIR / "docs" / "setup_en.md"
        if setup_doc.exists():
            zf.write(setup_doc, "README_SETUP.md")

    print(f"Successfully generated {zip_path} ({zip_path.stat().st_size:,} bytes).")
    return zip_path


def main() -> None:
    print("=" * 60)
    print("Silksong Archipelago Release Packaging Tool")
    print("=" * 60)

    if not build_csharp_release():
        sys.exit(1)

    apworld = package_apworld()
    client_zip = package_bepinex_client()

    print("\nAll release artifacts created successfully in dist/:")
    print(f"  1. {apworld.name} (Archipelago World Package)")
    print(f"  2. {client_zip.name} (BepInEx Client Mod Package)")


if __name__ == "__main__":
    main()
