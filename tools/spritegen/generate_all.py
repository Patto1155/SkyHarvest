"""Regenerate ALL SkyHarvest sprites deterministically.

Run: python3 -m tools.spritegen.generate_all
"""

from __future__ import annotations
import time

from . import terrain, player, crops, structures, items, ui, fx, bg


MODULES = [
    ("terrain", terrain),
    ("player", player),
    ("crops", crops),
    ("structures", structures),
    ("items", items),
    ("ui", ui),
    ("fx", fx),
    ("bg", bg),
]


def main():
    t0 = time.time()
    for name, mod in MODULES:
        ts = time.time()
        mod.generate()
        print(f"  [{name}] generated in {time.time() - ts:.1f}s")
    print(f"All sprites regenerated in {time.time() - t0:.1f}s")


if __name__ == "__main__":
    main()
