#!/usr/bin/env python3
"""Compare the trained-at levels of every ability rank in Content.fs against the
research tables in .pi/research/levelscale/ranks-*.md.

Only levels are compared, not the effect magnitudes: a wrong level means a rank
the player never trains (or trains early), while a wrong magnitude shows up in
the harness numbers. Levels are where transcription actually goes wrong.

  python3 scripts/rank-diff.py            # exit 1 on any mismatch
"""
import re, sys, pathlib

ROOT = pathlib.Path(__file__).resolve().parent.parent
RESEARCH = sorted((ROOT / ".pi/research/levelscale").glob("ranks-*.md"))
CONTENT = ROOT / "src/WowRoguelike.Core/Content.fs"
# Rogue ladders live in no ranks-*.md file; their levels are checked by hand.
NO_TABLE = {"Sinister Strike", "Backstab", "Eviscerate", "Gouge", "Kick"}

# Deliberate deviations, with the reason. Anything not listed here is a
# transcription error. {ability: {table level: level used in the code}}
DEVIATIONS = {
    # Rejuvenation rank 5 is level 28 in the table but 24 here: the level-24 party
    # is this file's calibration point, so the Druid trains it four levels early.
    # See the comment above `rejuvenationRanks`.
    "Rejuvenation": {28: 24},
}


def research_levels():
    """{ability name: [trained-at levels]} from every rank table."""
    out = {}
    for path in RESEARCH:
        current = None
        for line in path.read_text(encoding="utf-8").splitlines():
            head = re.match(r"^##\s+(.+?)\s+[—–-]\s+\w+", line)
            if head:
                current = head.group(1).strip()
                out.setdefault(current, [])
                continue
            row = re.match(r"^\|\s*(\d+)\s*\|\s*(\d+)\s*\|", line)
            if row and current:
                out[current].append(int(row.group(2)))
    return out


def code_levels():
    """{ability name: [trained-at levels]} from the AbilityDef ladders."""
    text = CONTENT.read_text(encoding="utf-8")
    out = {}
    for block in re.split(r"(?=^  let )", text, flags=re.M)[1:]:
        name = re.search(r'Name = "([^"]+)"', block)
        ranks = re.search(r"Ranks =", block)
        if not name or not ranks:
            continue
        body = block[ranks.end():]
        explicit = [int(x) for x in re.findall(r"Level = (\d+)", body)]
        if explicit:
            out[name.group(1)] = explicit
            continue
        table = re.search(r"in\s*\[(.*?)\]\s*do", body, re.S)
        if table:
            # rows are separated by ';' or by a newline, whichever the ladder used
            rows = re.split(r"[;\n]", table.group(1))
            out[name.group(1)] = [int(r.split(",")[0]) for r in rows if r.strip()]
    return out


def main():
    want, have = research_levels(), code_levels()
    bad = 0
    for name, levels in want.items():
        deviation = DEVIATIONS.get(name, {})
        levels = [deviation.get(level, level) for level in levels]
        got = have.get(name)
        if got is None:
            print(f"MISSING  {name}: no AbilityDef ladder in Content.fs")
            bad += 1
        elif got != levels:
            print(f"LEVELS   {name}: research {levels} != code {got}")
            bad += 1
    for name in have:
        if name not in want and name not in NO_TABLE:
            print(f"EXTRA    {name}: ladder in Content.fs, no research table")
            bad += 1
    total = sum(len(v) for v in want.values())
    print(f"{len(want)} researched abilities / {total} ranks checked, {bad} mismatch(es)")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
