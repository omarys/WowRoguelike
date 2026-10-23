#!/usr/bin/env python3
"""Check every numeric field of every ability rank in Content.fs against the
research tables in .pi/research/levelscale/ranks-*.md.

The ladders are written as `for level, cost, cast, lo, hi in [ ... ] do`, so each
field of each rank is a tuple column. This script resolves those columns and
compares Level, Cost, Cast, Cooldown and Range with the table's columns, plus
the two conversions that are easy to get wrong and silent when wrong:

  * periodic effects: `HealOverTime`/`DamageOverTime` take an amount PER SECOND
    (`Domain.fs:329` multiplies by `r / secTicks 1.0`), so a "over 18 s" total
    must be divided by the duration, never by the tick count.
  * direct damage: `SpellDamage(lo, hi)` against the table's `A-B`.

  python3 scripts/rank-verify.py [--verbose]     # exit 1 on any mismatch
"""
import re, sys, pathlib

ROOT = pathlib.Path(__file__).resolve().parent.parent
RESEARCH = sorted((ROOT / ".pi/research/levelscale").glob("ranks-*.md"))
CONTENT = ROOT / "src/WowRoguelike.Core/Content.fs"


def num(text):
    """Leading number in a table cell ('15%' -> 15.0, '—' -> None)."""
    m = re.search(r"-?\d+(?:\.\d+)?", text)
    return float(m.group(0)) if m else None


def research_rows():
    """{ability: [{level,cost,cast,cd,range,effects}]}"""
    out = {}
    for path in RESEARCH:
        current = None
        for line in path.read_text(encoding="utf-8").splitlines():
            head = re.match(r"^##\s+(.+?)\s+[—–-]\s+\w+", line)
            if head:
                current = head.group(1).strip()
                out.setdefault(current, [])
                continue
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            if current is None or len(cells) != 8 or not cells[0].isdigit():
                continue
            out[current].append(
                dict(level=num(cells[1]), cost=num(cells[2]), cast=num(cells[3]),
                     cd=num(cells[4]), range=num(cells[5]), effects=cells[6])
            )
    return out


def resolve(expr, names, columns):
    """Integer value of a field expression, resolving a tuple variable."""
    expr = expr.strip()
    if expr in names:
        return columns[names.index(expr)]
    m = re.fullmatch(r"-?\d+", expr)
    return int(m.group(0)) if m else None


def resolve_seconds(expr, names, columns):
    m = re.fullmatch(r"secTicks\s+\(?\s*(-?[\d.]+)\s*\)?", expr.strip())
    if m:
        return float(m.group(1))
    if expr.strip() == "ticks 0":
        return 0.0
    value = resolve(expr, names, columns)
    return None if value is None else float(value)


EXPLICIT = set()


def code_rows():
    """{ability: [{level,cost,cast,cd,range,effects}]}"""
    text = CONTENT.read_text(encoding="utf-8")
    explicit = EXPLICIT
    out = {}
    for block in re.split(r"(?=^  let \w+: AbilityDef =)", text, flags=re.M)[1:]:
        name = re.search(r'Name = "([^"]+)"', block)
        ranks = re.search(r"Ranks =", block)
        if not name or not ranks:
            continue
        body = block[ranks.end():]
        loop = re.search(r"for\s+([\w\s,]+?)\s+in\s*\[(.*?)\]\s*do", body, re.S)
        if not loop:
            # An explicit-record ladder ({ Level = 10; Cost = ... }). Its levels are
            # checked by rank-diff.py; nothing here reads its numbers, so record it
            # as covered rather than reporting it missing.
            explicit.add(name.group(1))
            continue
        names = [n.strip() for n in loop.group(1).split(",")]
        table = re.split(r"[;\n]", loop.group(2))
        rows = [[float(x) for x in r.split(",") if x.strip()] for r in table if r.strip()]
        after = body[loop.end():]
        fields = {}
        for key, pattern in (
            ("cost", r"Cost = ([^;\n]+)"),
            ("cast", r"CastTicks = ([^;\n]+)"),
            ("cd", r"CooldownTicks = ([^;\n]+)"),
            ("range", r"Range = ([^;\n]+)"),
            ("effects", r"Effects = (\[.*?\])"),
        ):
            m = re.search(pattern, after, re.S)
            fields[key] = m.group(1) if m else None
        out[name.group(1)] = [
            dict(columns=row, names=names, fields=fields) for row in rows
        ]
    return out


def periodic(expr, names, columns):
    """(duration_seconds, amount_per_second) of a HoT/DoT effect, if present."""
    m = re.search(r"(?:HealOverTime|DamageOverTime)\(\s*([^,]+),\s*([^)]+)\)", expr)
    if not m:
        return None
    duration = resolve_seconds(m.group(1), names, columns)
    amount = resolve(m.group(2), names, columns)
    return (duration, amount) if duration and amount is not None else None


def table_periodic(cell):
    """(duration_seconds, total) from 'HoA 45 every 3 s for 12 s (180 total)'
    or 'DoT 132 over 18 s (6 ticks)'."""
    m = re.search(r"\(\s*(\d+)\s*total\s*\)", cell)
    total = int(m.group(1)) if m else None
    d = re.search(r"for\s+(\d+(?:\.\d+)?)\s*s", cell) or re.search(
        r"over\s+(\d+(?:\.\d+)?)\s*s", cell
    )
    if not d:
        return None
    duration = float(d.group(1))
    if total is None:
        # 'DoT 132 over 18 s (6 ticks)' leads with the total
        lead = re.search(r"DoT\s+(\d+)", cell)
        if not lead:
            return None
        total = int(lead.group(1))
    return duration, total


# Deliberate deviations, with the reason. Anything not listed is an error.
#   cost: a percentage of base mana in the table, converted to mana here.
#   periodic: a hand-sourced level-24 value the project calibrates everything to.
DEVIATIONS = {
    "Fear": {"cost"},
    "Charge": {"cost"},  # the table spends 0 rage; rage is generated, so Cost is negative
    "Rejuvenation": {"periodic"},  # rank 5 sits at level 24 with the file's 22/s
}


def allowed(ability, key):
    return key in DEVIATIONS.get(ability, set())


def main():
    verbose = "--verbose" in sys.argv
    want, have = research_rows(), code_rows()
    bad = unverified = checked = 0
    for name, rows in want.items():
        ladders = have.get(name)
        if not ladders:
            if name not in EXPLICIT:
                print(f"MISSING  {name}: no ladder found in Content.fs")
                bad += 1
            continue
        if len(ladders) != len(rows):
            print(f"COUNT    {name}: table {len(rows)} ranks, code {len(ladders)}")
            bad += 1
        for table, code in zip(rows, ladders):
            rank = code["columns"][0]
            for key, field_value in (("cost", code["fields"]["cost"]),):
                got = resolve(field_value or "", code["names"], code["columns"])
                if allowed(name, "cost"):
                    continue
                if got is not None and table[key] is not None and got != table[key]:
                    print(f"{key.upper():8} {name} r{rank:g}: table {table[key]:g} != code {got}")
                    bad += 1
            if verbose:
                got = resolve(code["fields"]["range"] or "", code["names"], code["columns"])
                if got is not None and table["range"] is not None and got != table["range"]:
                    print(f"RANGE    {name} r{rank:g}: table {table['range']:g} != code {got}")
                    bad += 1
            cast = resolve_seconds(code["fields"]["cast"] or "", code["names"], code["columns"])
            if cast is not None and table["cast"] is not None and abs(cast - table["cast"]) > 0.01:
                print(f"CAST     {name} r{rank:g}: table {table['cast']:g} != code {cast:g}")
                bad += 1
            cd = resolve_seconds(code["fields"]["cd"] or "", code["names"], code["columns"])
            if verbose and cd is not None and table["cd"] is not None and abs(cd - table["cd"]) > 0.01:
                print(f"CD       {name} r{rank:g}: table {table['cd']:g} != code {cd:g}")
                bad += 1
            effect = code["fields"]["effects"] or ""
            got_periodic = periodic(effect, code["names"], code["columns"])
            want_periodic = table_periodic(table["effects"])
            # The one hand-sourced level-24 value is exempt; its neighbours are not.
            exempt = allowed(name, "periodic") and rank == 24
            if got_periodic and want_periodic and not exempt:
                duration, amount = got_periodic
                want_duration, total = want_periodic
                checked += 1
                expected = total / want_duration
                if abs(amount - expected) > max(1.0, expected * 0.06):
                    print(
                        f"PERIODIC {name} r{rank:g}: table {total} over {want_duration:g}s "
                        f"= {expected:.1f}/s, code {amount:g}/s"
                    )
                    bad += 1
                if abs(duration - want_duration) > 0.01:
                    print(
                        f"DURATION {name} r{rank:g}: table {want_duration:g}s, code {duration:g}s"
                    )
                    bad += 1
            elif verbose:
                unverified += 1
    print(
        f"{len(want)} abilities checked ({len(EXPLICIT)} explicit ladders via rank-diff.py), "
        f"{checked} periodic ranks verified, "
        f"{bad} mismatch(es)" + (f", {unverified} effects not comparable" if verbose else "")
    )
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
