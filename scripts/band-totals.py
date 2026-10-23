#!/usr/bin/env python3
"""Sum the researched gear tables into one row per class per band.

The sim consumes only armour, the five stats, shield-ness and weapon damage/speed
(Content.fs GearPiece/Dressed), so a band collapses to a single synthetic piece.
Run: python3 scripts/band-totals.py .pi/research/levelscale/gear-ladder-rest.md
"""
import re
import sys

BAND = re.compile(r"^### Band (\d+)\D+(\d+)")
CLASS = re.compile(r"^## \d+\.\s+(\w+)")
PLACEHOLDER = {"—", "-", "–", ""}


def num(cell):
    cell = cell.replace("*", "").strip()
    return 0 if cell in PLACEHOLDER else int(cell)


def parse(path):
    klass, band, out = None, None, {}
    for line in open(path, encoding="utf-8"):
        m = CLASS.match(line)
        if m:
            klass = m.group(1)
            continue
        m = BAND.match(line)
        if m:
            band = int(m.group(2))
            if klass:
                out.setdefault(klass, {})[band] = dict(arm=0, str=0, agi=0, sta=0, int=0, spi=0,
                                                       shield=False, wmin=0, wmax=0, spd=0.0, slots=0)
            continue
        if not klass or not band or not line.startswith("|"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) < 14 or cells[0] in ("Slot",) or set(cells[0]) <= set("-: "):
            continue
        slot = cells[0].lower()
        if "no sourced item" in cells[1] or cells[2] in PLACEHOLDER:
            continue
        row = out[klass][band]
        if "weapon" in slot:
            lo, hi = re.findall(r"\d+", cells[4].replace("*", ""))[:2]
            row["wmin"], row["wmax"] = int(lo), int(hi)
            row["spd"] = float(cells[5].replace("*", ""))
            continue
        row["slots"] += 1
        row["arm"] += num(cells[3])
        row["str"] += num(cells[6])
        row["agi"] += num(cells[7])
        row["sta"] += num(cells[8])
        row["int"] += num(cells[9])
        row["spi"] += num(cells[10])
        if "shield" in cells[1].lower():
            row["shield"] = True
    return out


DPS = re.compile(r"\d")


def main(path):
    data = parse(path)
    for klass, bands in data.items():
        print(f"\n{klass}")
        print(f"  {'band':>5} {'arm':>5} {'str':>4} {'agi':>4} {'sta':>4} {'int':>4} {'spi':>4} "
              f"{'weapon':>11} {'spd':>4} {'dps':>5} {'slots':>5}  shield")
        for band in sorted(bands):
            r = bands[band]
            dps = 0.0 if r["spd"] == 0 else (r["wmin"] + r["wmax"]) / 2 / r["spd"]
            print(f"  {band:>5} {r['arm']:>5} {r['str']:>4} {r['agi']:>4} {r['sta']:>4} "
                  f"{r['int']:>4} {r['spi']:>4} {r['wmin']:>5}-{r['wmax']:<5} {r['spd']:>4} "
                  f"{dps:>5.1f} {r['slots']:>5}  {r['shield']}")


if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else ".pi/research/levelscale/gear-ladder-rest.md"
    data = parse(path)

    # Self-check: a silently mis-parsed table would ship wrong armour totals, and
    # those totals are what the whole scaling fix consumes.
    assert data, f"no class sections parsed out of {path}"

    for klass, bands in data.items():
        assert len(bands) >= 4, f"{klass}: expected at least 4 bands, parsed {sorted(bands)}"
        for band, row in bands.items():
            assert row["slots"] >= 10, f"{klass} {band}: only {row['slots']} slots parsed"
            assert row["wmax"] > row["wmin"] > 0, f"{klass} {band}: weapon damage {row['wmin']}-{row['wmax']}"
            assert row["arm"] > 0, f"{klass} {band}: no armour parsed"

    main(path)
