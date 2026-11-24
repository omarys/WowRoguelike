#!/usr/bin/env -S uv run --quiet
# /// script
# requires-python = ">=3.10"
# dependencies = ["pillow", "numpy"]
# ///
"""Split dungeon tileset sheets in images/Tilesets/ into individual sprite PNGs.

Usage:
    python3 tools/split_tileset.py images/Tilesets/<Sheet>.jpg [--out-dir DIR]
    python3 tools/split_tileset.py --all

Outputs under images/Tilesets/sprites/<DungeonName>/ (by default):
    r1_01.png, r1_02.png, ...   (Row 1: terrain / grid tiles / dioramas)
    r2_01.png, r3_01.png, ...   (Rows 2+: additional terrain rows and/or labeled hero/denizen sprites)
    _contact-sheet.png          (labeled thumbnail grid of all extracted sprites)
    INDEX.md                    (markdown table of file, size, source box, mean RGB, note)
"""
from __future__ import annotations

import argparse
from collections import deque
from dataclasses import dataclass, field
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_OUT_ROOT = ROOT / "images/Tilesets/sprites"
SKIP_SHEETS = {"Druid_Tree_and_Moonkin.jpg", "Extended_Druid_Shaman_Hunter.jpg"}


@dataclass
class SheetSpec:
    layout: str  # "grid_top" or "diorama_top"
    split_y: int  # y separating top terrain from bottom sprite/denizen region
    title_y_end: int = 0  # top title/subtitle band to ignore in diorama_top
    denizen_header_y: tuple[int, int] | None = None  # (y0, y1) of section header to mask
    title_boxes: list[tuple[int, int, int, int]] = field(default_factory=list)


# Per-sheet layout metadata based on visual inspection of all 14 sheets.
SHEET_SPECS: dict[str, SheetSpec] = {
    "Blackfathom_Deeps": SheetSpec("grid_top", split_y=612),
    "Ragefire_Chasm": SheetSpec("grid_top", split_y=766),
    "Razorfen": SheetSpec("grid_top", split_y=598),
    "Scarlet_Monastery": SheetSpec(
        "diorama_top", split_y=785, title_y_end=0,
        title_boxes=[(1663, 0, 1677, 785)],
    ),
    "Shadowfang_Keep": SheetSpec("grid_top", split_y=948),
    "The_Deadmines": SheetSpec("grid_top", split_y=940),
    "The_Stockade": SheetSpec("grid_top", split_y=940),
    "Wailing_Caverns": SheetSpec("grid_top", split_y=895),
    "Blackrock_Depths": SheetSpec(
        "diorama_top", split_y=1020, title_y_end=175, denizen_header_y=(1020, 1095),
        title_boxes=[(760, 0, 1650, 175)],
    ),
    "Maraudon": SheetSpec(
        "diorama_top", split_y=1025, title_y_end=115, denizen_header_y=(1025, 1088),
        title_boxes=[(850, 0, 1550, 115)],
    ),
    "Scholomance": SheetSpec(
        "diorama_top", split_y=915, title_y_end=125, denizen_header_y=(915, 965),
        title_boxes=[(800, 0, 1600, 125)],
    ),
    "Stratholme": SheetSpec(
        "diorama_top", split_y=1035, title_y_end=205, denizen_header_y=(1035, 1098),
        title_boxes=[(780, 0, 1620, 205)],
    ),
    "Uldaman": SheetSpec(
        "diorama_top", split_y=912, title_y_end=175,
        title_boxes=[(850, 0, 1550, 175)],
    ),
    "ZulFarrak": SheetSpec(
        "diorama_top", split_y=990, title_y_end=225, denizen_header_y=(995, 1068),
        title_boxes=[(0, 0, 620, 165), (0, 155, 960, 220)],
    ),
}


@dataclass
class SpriteRecord:
    filename: str
    box: tuple[int, int, int, int]
    size: tuple[int, int]
    mean_rgb: tuple[int, int, int]
    note: str
    image: Image.Image


def estimate_bg(arr: np.ndarray) -> np.ndarray:
    """Estimate the dark-grey background RGB of the sheet."""
    dark = arr[arr.max(axis=2) < 90]
    if len(dark) == 0:
        return np.array([40, 40, 44], dtype=np.int16)
    return np.median(dark, axis=0).astype(np.int16)


def foreground_mask(arr: np.ndarray, bg: np.ndarray, dist_thresh: int = 22) -> np.ndarray:
    """Mask of non-background pixels, ignoring thin dark grid lines."""
    diff = arr.astype(np.int16) - bg
    dist2 = (diff * diff).sum(axis=2)
    return (dist2 > dist_thresh * dist_thresh) & (arr.max(axis=2) > 40)


def text_pixel_mask(arr: np.ndarray) -> np.ndarray:
    """Mask of thin-stroke bright white/grey or gold/amber label text pixels."""
    a = arr.astype(np.int16)
    r, g, b = a[:, :, 0], a[:, :, 1], a[:, :, 2]
    peak = a.max(axis=2)
    spread = peak - a.min(axis=2)
    white_text = (peak > 155) & (spread < 45)
    gold_text = (r > 155) & (g > 110) & (b < 135) & (r - b > 45)
    raw = white_text | gold_text
    # Filter out wide solid fills (>=14px horizontal runs, e.g. white robes, stone, armor)
    h, w = raw.shape
    if w >= 14:
        cs = np.zeros((h, w + 1), dtype=np.int16)
        cs[:, 1:] = np.cumsum(raw, axis=1)
        wide_starts = (cs[:, 14:] - cs[:, :-14]) == 14
        if wide_starts.any():
            wide = np.zeros_like(raw)
            for dx in range(14):
                wide[:, dx:w - 13 + dx] |= wide_starts
            raw = raw & (~wide)
    return raw


def spans_1d(profile: np.ndarray, min_gap: int = 4) -> list[tuple[int, int]]:
    """Return inclusive-exclusive (start, end) spans of True values in a 1D bool array."""
    on = np.flatnonzero(profile)
    if on.size == 0:
        return []
    out: list[tuple[int, int]] = []
    start = int(on[0])
    prev = int(on[0])
    for cur in on[1:]:
        cur_i = int(cur)
        if cur_i - prev > min_gap:
            out.append((start, prev + 1))
            start = cur_i
        prev = cur_i
    out.append((start, prev + 1))
    return out


def find_strict_grid_lines(region: np.ndarray, bg: np.ndarray, axis: int, min_cell: int = 80) -> list[int]:
    """Find true dark grid/border divider lines (narrow, >94% dark/bg with no bright art)."""
    diff = region.astype(np.int16) - bg
    dist2 = (diff * diff).sum(axis=2)
    # Only pixels very close to bg OR very dark black border lines
    is_divider = (dist2 <= 16 * 16) | (region.max(axis=2) <= 24)
    frac = is_divider.mean(axis=1 - axis)
    # Require no bright painted pixels (99th percentile < 60) so weapons/staffs are never sliced
    p99 = np.percentile(region.max(axis=2), 99, axis=1 - axis)
    hits = (frac > 0.94) & (p99 < 60)

    length = region.shape[axis]
    line_spans = spans_1d(hits, min_gap=2)
    centers = [(s + e) // 2 for s, e in line_spans if (e - s) <= 18]

    bounds = [0]
    for c in centers:
        if c - bounds[-1] >= min_cell:
            bounds.append(c)
    if length - bounds[-1] >= min_cell:
        bounds.append(length)
    else:
        bounds[-1] = length
    return bounds


def key_out_edges(img: Image.Image, bg: np.ndarray, tol: int = 20) -> Image.Image:
    """Flood-fill dark background from crop edges to transparent, preserving dark outlines."""
    rgba = img.convert("RGBA")
    arr = np.asarray(rgba, dtype=np.int16)
    h, w = arr.shape[:2]
    rgb = arr[:, :, :3]
    diff = rgb - bg
    near_bg = (diff * diff).sum(axis=2) <= tol * tol

    seen = np.zeros((h, w), dtype=bool)
    q: deque[tuple[int, int]] = deque()
    for x in range(w):
        for y in (0, h - 1):
            if near_bg[y, x] and not seen[y, x]:
                seen[y, x] = True
                q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            if near_bg[y, x] and not seen[y, x]:
                seen[y, x] = True
                q.append((x, y))

    while q:
        x, y = q.popleft()
        arr[y, x] = (0, 0, 0, 0)
        if x + 1 < w and not seen[y, x + 1] and near_bg[y, x + 1]:
            seen[y, x + 1] = True
            q.append((x + 1, y))
        if x - 1 >= 0 and not seen[y, x - 1] and near_bg[y, x - 1]:
            seen[y, x - 1] = True
            q.append((x - 1, y))
        if y + 1 < h and not seen[y + 1, x] and near_bg[y + 1, x]:
            seen[y + 1, x] = True
            q.append((x, y + 1))
        if y - 1 >= 0 and not seen[y - 1, x] and near_bg[y - 1, x]:
            seen[y - 1, x] = True
            q.append((x, y - 1))

    return Image.fromarray(arr.astype(np.uint8), "RGBA")


def is_full_bleed(crop_arr: np.ndarray, bg: np.ndarray) -> bool:
    """Return True if the crop's edges are mostly painted art rather than background."""
    border = np.concatenate([
        crop_arr[0, :, :],
        crop_arr[-1, :, :],
        crop_arr[:, 0, :],
        crop_arr[:, -1, :],
    ], axis=0)
    diff = border.astype(np.int16) - bg
    near_bg = (diff * diff).sum(axis=1) <= 22 * 22
    return float(near_bg.mean()) < 0.35


def trim_dark_border(arr: np.ndarray, bg: np.ndarray, max_trim: int = 6) -> tuple[int, int, int, int]:
    """Trim 1-6px dark grid border from a grid cell."""
    h, w = arr.shape[:2]
    diff = arr.astype(np.int16) - bg
    is_dark = ((diff * diff).sum(axis=2) <= 18 * 18) | (arr.max(axis=2) <= 26)
    y0, y1, x0, x1 = 0, h, 0, w
    while y0 < max_trim and y0 < y1 - 20 and is_dark[y0, x0:x1].mean() > 0.75:
        y0 += 1
    while h - y1 < max_trim and y1 > y0 + 20 and is_dark[y1 - 1, x0:x1].mean() > 0.75:
        y1 -= 1
    while x0 < max_trim and x0 < x1 - 20 and is_dark[y0:y1, x0].mean() > 0.75:
        x0 += 1
    while w - x1 < max_trim and x1 > x0 + 20 and is_dark[y0:y1, x1 - 1].mean() > 0.75:
        x1 -= 1
    return x0, y0, x1, y1


def connected_components_boxes(mask: np.ndarray, dilate_px: int = 6, min_area: int = 500) -> list[tuple[int, int, int, int]]:
    """Find bounding boxes of connected foreground regions with optional dilation."""
    h, w = mask.shape
    work = mask.copy()
    if dilate_px > 0:
        dil = work.copy()
        for dy in range(-dilate_px, dilate_px + 1, 2):
            if dy > 0:
                dil[dy:] |= work[:-dy]
            elif dy < 0:
                dil[:dy] |= work[-dy:]
        work = dil
        dil = work.copy()
        for dx in range(-dilate_px, dilate_px + 1, 2):
            if dx > 0:
                dil[:, dx:] |= work[:, :-dx]
            elif dx < 0:
                dil[:, :dx] |= work[:, -dx:]
        work = dil

    boxes: list[tuple[int, int, int, int]] = []
    step = 2
    sm = work[::step, ::step]
    sh, sw = sm.shape
    sseen = np.zeros((sh, sw), dtype=bool)
    for sy in range(sh):
        for sx in range(sw):
            if not sm[sy, sx] or sseen[sy, sx]:
                continue
            sseen[sy, sx] = True
            q = [(sx, sy)]
            minx = maxx = sx
            miny = maxy = sy
            count = 0
            while q:
                cx, cy = q.pop()
                count += 1
                if cx < minx:
                    minx = cx
                if cx > maxx:
                    maxx = cx
                if cy < miny:
                    miny = cy
                if cy > maxy:
                    maxy = cy
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if 0 <= nx < sw and 0 <= ny < sh and sm[ny, nx] and not sseen[ny, nx]:
                        sseen[ny, nx] = True
                        q.append((nx, ny))
            if count * step * step < min_area:
                continue
            x0 = max(0, minx * step - 2)
            y0 = max(0, miny * step - 2)
            x1 = min(w, (maxx + 1) * step + 2)
            y1 = min(h, (maxy + 1) * step + 2)
            sub = mask[y0:y1, x0:x1]
            ys, xs = np.nonzero(sub)
            if ys.size >= min_area // 2:
                boxes.append((x0 + int(xs.min()), y0 + int(ys.min()), x0 + int(xs.max()) + 1, y0 + int(ys.max()) + 1))
    boxes.sort(key=lambda b: (b[1] // 80, b[0]))
    return boxes


def strip_bottom_caption(cell_arr: np.ndarray, bg: np.ndarray) -> tuple[int, int, int, int] | None:
    """Return tight (x0, y0, x1, y1) of sprite art inside cell_arr, excluding bottom caption text."""
    fg = foreground_mask(cell_arr, bg)
    txt = text_pixel_mask(cell_arr)
    h, w = fg.shape
    row_fg = fg.mean(axis=1)

    v_spans = spans_1d(row_fg > 0.01, min_gap=3)
    if not v_spans:
        return None

    while len(v_spans) > 1:
        y0, y1 = v_spans[-1]
        span_h = y1 - y0
        fg_cnt = max(1, int(fg[y0:y1].sum()))
        txt_cnt = int(txt[y0:y1].sum())
        if span_h <= 75 and (txt_cnt / fg_cnt) > 0.42:
            v_spans.pop()
        else:
            break

    y0, y1 = v_spans[0][0], v_spans[-1][1]
    if y1 - y0 > 80:
        non_txt = fg & (~txt)
        non_txt_rows = non_txt[y0:y1].sum(axis=1)
        txt_rows = txt[y0:y1].sum(axis=1)
        cut = y1
        for ry in range(y1 - 1, max(y0 + 50, y1 - 65), -1):
            idx = ry - y0
            if txt_rows[idx] > 4 and non_txt_rows[idx] <= 8:
                cut = ry
            elif non_txt_rows[idx] > 18:
                break
        if cut < y1 - 6:
            y1 = cut

    sub_fg = fg[y0:y1, :]
    ys, xs = np.nonzero(sub_fg)
    if ys.size < 200:
        return None
    bx0 = max(0, int(xs.min()) - 2)
    by0 = max(0, y0 + int(ys.min()) - 2)
    bx1 = min(w, int(xs.max()) + 3)
    by1 = min(h, y0 + int(ys.max()) + 3)
    if (bx1 - bx0) < 24 or (by1 - by0) < 24:
        return None
    return (bx0, by0, bx1, by1)


def extract_top_grid(arr: np.ndarray, bg: np.ndarray, split_y: int, force_8col: bool = False) -> list[list[tuple[tuple[int, int, int, int], str]]]:
    """Extract terrain cells from a grid-based top region, returning rows of (box, note)."""
    top = arr[:split_y, :, :]
    y_bounds = find_strict_grid_lines(top, bg, axis=0, min_cell=100)
    if len(y_bounds) <= 2:
        # Fallback: split by horizontal foreground gaps if no strict full-width lines
        fg_y = foreground_mask(top, bg).mean(axis=1) > 0.01
        y_spans = spans_1d(fg_y, min_gap=6)
        y_bounds_pairs = [(s, e) for s, e in y_spans if e - s >= 60]
    else:
        y_bounds_pairs = [(y0, y1) for y0, y1 in zip(y_bounds, y_bounds[1:]) if y1 - y0 >= 60]

    rows_out: list[list[tuple[tuple[int, int, int, int], str]]] = []
    for ry0, ry1 in y_bounds_pairs:
        strip = top[ry0:ry1, :, :]
        x_bounds = find_strict_grid_lines(strip, bg, axis=1, min_cell=100)
        if force_8col or len(x_bounds) <= 2:
            w = top.shape[1]
            step = w / 8.0
            x_pairs = [(int(round(i * step)), int(round((i + 1) * step))) for i in range(8)]
        else:
            x_pairs = [(x0, x1) for x0, x1 in zip(x_bounds, x_bounds[1:]) if x1 - x0 >= 60]

        row_boxes: list[tuple[tuple[int, int, int, int], str]] = []
        for rx0, rx1 in x_pairs:
            cell = top[ry0:ry1, rx0:rx1, :]
            sub_y = find_strict_grid_lines(cell, bg, axis=0, min_cell=70)
            sub_ranges = [(sy0, sy1) for sy0, sy1 in zip(sub_y, sub_y[1:]) if sy1 - sy0 >= 60] if len(sub_y) > 2 else [(0, ry1 - ry0)]
            for sy0, sy1 in sub_ranges:
                sub_cell = cell[sy0:sy1, :, :]
                tx0, ty0, tx1, ty1 = trim_dark_border(sub_cell, bg)
                gx0, gy0 = rx0 + tx0, ry0 + sy0 + ty0
                gx1, gy1 = rx0 + tx1, ry0 + sy0 + ty1
                sub_fg = foreground_mask(arr[gy0:gy1, gx0:gx1], bg)
                if sub_fg.sum() < 500:
                    continue
                note = "grid tile" if is_full_bleed(arr[gy0:gy1, gx0:gx1], bg) else "grid prop cutout"
                row_boxes.append(((gx0, gy0, gx1, gy1), note))
        if row_boxes:
            rows_out.append(row_boxes)
    return rows_out


def extract_top_diorama(
    arr: np.ndarray,
    bg: np.ndarray,
    split_y: int,
    title_y_end: int,
    title_boxes: list[tuple[int, int, int, int]] | None = None,
) -> list[list[tuple[tuple[int, int, int, int], str]]]:
    """Extract isometric dioramas/scenes from top region, stripping title & label text."""
    top = arr[:split_y, :, :]
    fg = foreground_mask(top, bg)
    txt = text_pixel_mask(top)

    for tx0, ty0, tx1, ty1 in (title_boxes or []):
        y_lo, y_hi = max(0, ty0), min(split_y, ty1)
        x_lo, x_hi = max(0, tx0), min(top.shape[1], tx1)
        fg[y_lo:y_hi, x_lo:x_hi] = False
        arr[y_lo:y_hi, x_lo:x_hi] = bg

    if title_y_end > 0:
        title_fg = fg[:title_y_end, :]
        title_txt = txt[:title_y_end, :]
        for bx0, by0, bx1, by1 in connected_components_boxes(title_fg, dilate_px=4, min_area=60):
            comp_fg = max(1, int(title_fg[by0:by1, bx0:bx1].sum()))
            comp_txt = int(title_txt[by0:by1, bx0:bx1].sum())
            if (by1 - by0) < 170 and (comp_txt / comp_fg > 0.22 or (bx0 > 550 and bx1 < 1850 and by1 <= title_y_end)):
                fg[by0:by1, bx0:bx1] = False

    for bx0, by0, bx1, by1 in connected_components_boxes(fg, dilate_px=3, min_area=60):
        comp_fg = max(1, int(fg[by0:by1, bx0:bx1].sum()))
        comp_txt = int(txt[by0:by1, bx0:bx1].sum())
        if (by1 - by0) < 70 and (comp_txt / comp_fg) > 0.35:
            fg[by0:by1, bx0:bx1] = False

    boxes = connected_components_boxes(fg, dilate_px=4, min_area=2000)
    if not boxes:
        return []
    return [[(b, "terrain diorama/scene") for b in boxes]]


def find_black_box_lines(region: np.ndarray, axis: int, min_cell: int = 100) -> list[int]:
    """Find true black box-frame divider lines (2-18px thick, max RGB <= 22 across >85% of span)."""
    is_black = region.max(axis=2) <= 22
    frac = is_black.mean(axis=1 - axis)
    hits = frac > 0.85
    length = region.shape[axis]
    line_spans = spans_1d(hits, min_gap=2)
    centers = [(s + e) // 2 for s, e in line_spans if 2 <= (e - s) <= 18]
    bounds = [0]
    for c in centers:
        if c - bounds[-1] >= min_cell:
            bounds.append(c)
    if length - bounds[-1] >= min_cell:
        bounds.append(length)
    else:
        bounds[-1] = length
    return bounds


def find_caption_bands(bot: np.ndarray, fg: np.ndarray, txt: np.ndarray) -> list[tuple[int, int]]:
    """Find horizontal y-bands of printed captions in the bottom sprite region.

    A caption band has thin-stroke white/gold text distributed across multiple
    horizontal label clusters, each with a high text-to-foreground ratio.
    """
    txt_cnt = txt.sum(axis=1)
    cand = txt_cnt >= 45
    bands: list[tuple[int, int]] = []
    for y0, y1 in spans_1d(cand, min_gap=14):
        if not (16 <= (y1 - y0) <= 115):
            continue
        clusters = spans_1d(txt[y0:y1, :].any(axis=0), min_gap=22)
        text_clusters = 0
        for cx0, cx1 in clusters:
            c_txt = int(txt[y0:y1, cx0:cx1].sum())
            c_fg = max(1, int(fg[y0:y1, cx0:cx1].sum()))
            if c_txt >= 12 and (c_txt / c_fg) > 0.36:
                text_clusters += 1
        if text_clusters >= 3:
            bands.append((y0, y1))
    return bands


def strip_top_and_bottom_captions(cell_arr: np.ndarray, bg: np.ndarray) -> tuple[int, int, int, int] | None:
    """Return tight (x0, y0, x1, y1) of sprite art inside cell_arr, excluding top/bottom caption text."""
    fg = foreground_mask(cell_arr, bg)
    txt = text_pixel_mask(cell_arr)
    h, w = fg.shape
    row_fg = fg.mean(axis=1)

    v_spans = spans_1d(row_fg > 0.01, min_gap=3)
    if not v_spans:
        return None

    # Drop top spans that are leftover caption text from the row above
    while len(v_spans) > 1:
        y0, y1 = v_spans[0]
        span_h = y1 - y0
        fg_cnt = max(1, int(fg[y0:y1].sum()))
        txt_cnt = int(txt[y0:y1].sum())
        if span_h <= 75 and (txt_cnt / fg_cnt) > 0.30:
            v_spans.pop(0)
        else:
            break

    # Drop bottom spans that are caption text for this sprite
    while len(v_spans) > 1:
        y0, y1 = v_spans[-1]
        span_h = y1 - y0
        fg_cnt = max(1, int(fg[y0:y1].sum()))
        txt_cnt = int(txt[y0:y1].sum())
        if span_h <= 75 and (txt_cnt / fg_cnt) > 0.30:
            v_spans.pop()
        else:
            break

    y0, y1 = v_spans[0][0], v_spans[-1][1]
    if y1 - y0 > 80:
        non_txt = fg & (~txt)
        non_txt_rows = non_txt[y0:y1].sum(axis=1)
        txt_rows = txt[y0:y1].sum(axis=1)
        cut = y1
        for ry in range(y1 - 1, max(y0 + 50, y1 - 65), -1):
            idx = ry - y0
            if txt_rows[idx] > 4 and non_txt_rows[idx] <= 8:
                cut = ry
            elif non_txt_rows[idx] > 18:
                break
        if cut < y1 - 6:
            y1 = cut

    sub_fg = fg[y0:y1, :]
    ys, xs = np.nonzero(sub_fg)
    if ys.size < 200:
        return None
    bx0 = max(0, int(xs.min()) - 2)
    by0 = max(0, y0 + int(ys.min()) - 2)
    bx1 = min(w, int(xs.max()) + 3)
    by1 = min(h, y0 + int(ys.max()) + 3)
    if (bx1 - bx0) < 24 or (by1 - by0) < 24:
        return None
    return (bx0, by0, bx1, by1)


def split_wide_strip_by_captions(
    strip: np.ndarray,
    strip_fg: np.ndarray,
    strip_txt: np.ndarray,
    cap_band: tuple[int, int] | None = None,
    x_offset: int = 0,
) -> list[tuple[int, int]]:
    """Split a wide horizontal strip into individual sprite (x0, x1) ranges."""
    w = strip.shape[1]
    if cap_band is not None:
        cy0, cy1 = cap_band
        cap_clusters = spans_1d(strip_txt[cy0:cy1, :].any(axis=0), min_gap=24)
        if len(cap_clusters) >= 2:
            edges = [0]
            for a, b in zip(cap_clusters, cap_clusters[1:]):
                mid = (a[1] + b[0]) // 2
                lo = max(0, mid - 25)
                hi = min(w, mid + 25)
                col_sum = strip_fg[:, lo:hi].sum(axis=0)
                edges.append(lo + int(np.argmin(col_sum)))
            edges.append(w)
            return [(x_offset + edges[i], x_offset + edges[i + 1]) for i in range(len(cap_clusters))]

    col_fg = strip_fg.any(axis=0)
    return [(x_offset + x0, x_offset + x1) for x0, x1 in spans_1d(col_fg, min_gap=6) if x1 - x0 >= 30]


def extract_bottom_sprites(
    arr: np.ndarray,
    bg: np.ndarray,
    split_y: int,
    denizen_header_y: tuple[int, int] | None = None,
    has_captions: bool = True,
) -> list[list[tuple[tuple[int, int, int, int], str]]]:
    """Extract labeled hero/mob/denizen sprite rows from the bottom region."""
    bot = arr[split_y:, :, :].copy()
    fg = foreground_mask(bot, bg)
    txt = text_pixel_mask(bot)

    # Mask out any section header band (e.g. "2. BOTTOM / Blackrock Depths Denizens")
    if denizen_header_y is not None:
        hy0 = max(0, denizen_header_y[0] - split_y)
        hy1 = min(bot.shape[0], denizen_header_y[1] - split_y)
        if hy1 > hy0:
            h_fg = fg[hy0:hy1, :]
            h_txt = txt[hy0:hy1, :]
            for bx0, by0, bx1, by1 in connected_components_boxes(h_fg, dilate_px=4, min_area=30):
                comp_txt = int(h_txt[by0:by1, bx0:bx1].sum())
                if comp_txt >= 10 and (by1 - by0) <= 75:
                    fg[hy0 + by0:hy0 + by1, bx0:bx1] = False
                    txt[hy0 + by0:hy0 + by1, bx0:bx1] = False
                    bot[hy0 + by0:hy0 + by1, bx0:bx1] = bg

    # Check if bottom rows are separated by true black box borders (e.g. Wailing_Caverns, Shadowfang_Keep)
    y_bounds = find_black_box_lines(bot, axis=0, min_cell=120)
    boxed_mode = len(y_bounds) >= 3
    if boxed_mode:
        row_ranges = [(y0, y1) for y0, y1 in zip(y_bounds, y_bounds[1:]) if y1 - y0 >= 100]
        row_caps = [None] * len(row_ranges)
    else:
        cap_bands = find_caption_bands(bot, fg, txt)
        if cap_bands:
            row_ranges = []
            row_caps = []
            prev_end = 0
            for cy0, cy1 in cap_bands:
                if cy0 - prev_end >= 70:
                    row_ranges.append((prev_end, cy0))
                    row_caps.append((cy0, cy1))
                prev_end = cy1 + 2
            if bot.shape[0] - prev_end >= 120 and fg[prev_end:, :].sum() > 3000:
                row_ranges.append((prev_end, bot.shape[0]))
                row_caps.append(None)
            # If any range is > 430px tall, it spans 2+ rows whose upper caption band was bridged; split by fg gaps
            split_ranges: list[tuple[int, int]] = []
            split_caps: list[tuple[int, int] | None] = []
            for (ry0, ry1), cb in zip(row_ranges, row_caps):
                if ry1 - ry0 > 430:
                    sub_fg = fg[ry0:ry1, :].mean(axis=1) > 0.008
                    sub_spans = [(ry0 + s, ry0 + e) for s, e in spans_1d(sub_fg, min_gap=8) if e - s >= 60]
                    if len(sub_spans) >= 2:
                        for i, (sy0, sy1) in enumerate(sub_spans):
                            split_ranges.append((sy0, sy1))
                            split_caps.append(cb if i == len(sub_spans) - 1 else None)
                        continue
                split_ranges.append((ry0, ry1))
                split_caps.append(cb)
            row_ranges, row_caps = split_ranges, split_caps
        else:
            row_fg = fg.mean(axis=1) > 0.008
            row_ranges = [(y0, y1) for y0, y1 in spans_1d(row_fg, min_gap=8) if y1 - y0 >= 60]
            row_caps = [None] * len(row_ranges)

    rows_out: list[list[tuple[tuple[int, int, int, int], str]]] = []
    for (ry0, ry1), cap_band in zip(row_ranges, row_caps):
        strip = bot[ry0:ry1, :, :]
        strip_fg = fg[ry0:ry1, :]
        strip_txt = txt[ry0:ry1, :]

        total_fg = max(1, int(strip_fg.sum()))
        total_txt = int(strip_txt.sum())
        if (ry1 - ry0) < 95 and (total_txt / total_fg) > 0.38:
            continue

        if boxed_mode:
            # Check for a caption band inside the bottom 24% of this boxed row (skip for captionless sheets)
            row_h = ry1 - ry0
            y_start = int(row_h * 0.76)
            bot_txt_spans = spans_1d(strip_txt[y_start:, :].sum(axis=1) >= 25, min_gap=8) if has_captions else []
            local_cap = None
            art_y1 = row_h
            for lcy0, lcy1 in bot_txt_spans:
                if (lcy1 - lcy0) >= 12:
                    c_clusters = spans_1d(strip_txt[y_start + lcy0:y_start + lcy1, :].any(axis=0), min_gap=20)
                    txt_ok = sum(
                        1 for cx0, cx1 in c_clusters
                        if strip_txt[y_start + lcy0:y_start + lcy1, cx0:cx1].sum()
                        / max(1, strip_fg[y_start + lcy0:y_start + lcy1, cx0:cx1].sum()) > 0.38
                    )
                    if txt_ok >= 3:
                        art_y1 = y_start + lcy0 - 2
                        local_cap = (y_start + lcy0, y_start + lcy1)
                        break

            x_bounds = find_black_box_lines(strip, axis=1, min_cell=100)
            col_ranges: list[tuple[int, int]] = []
            for bx0, bx1 in zip(x_bounds, x_bounds[1:]):
                if bx1 - bx0 < 60:
                    continue
                if bx1 - bx0 > 650:
                    # Wide box (e.g. 9 heroes or multi-group row): split by local captions or art gaps
                    sub_strip = strip[:art_y1, bx0:bx1, :]
                    sub_fg = strip_fg[:art_y1, bx0:bx1]
                    sub_txt = strip_txt[:, bx0:bx1]
                    col_ranges.extend(
                        split_wide_strip_by_captions(sub_strip, sub_fg, sub_txt, local_cap, x_offset=bx0)
                    )
                else:
                    col_ranges.append((bx0, bx1))
            # Restrict strip to art_y1 so boxed captions are excluded
            strip = strip[:art_y1, :, :]
        elif cap_band is not None:
            cy0, cy1 = cap_band
            col_ranges = split_wide_strip_by_captions(strip, strip_fg, bot[:, :, :], None, 0)
            # Re-run using bot's caption band directly
            cap_clusters = spans_1d(txt[cy0:cy1, :].any(axis=0), min_gap=24)
            if len(cap_clusters) >= 2:
                edges = [0]
                for a, b in zip(cap_clusters, cap_clusters[1:]):
                    mid = (a[1] + b[0]) // 2
                    lo = max(0, mid - 25)
                    hi = min(bot.shape[1], mid + 25)
                    col_sum = strip_fg[:, lo:hi].sum(axis=0)
                    edges.append(lo + int(np.argmin(col_sum)))
                edges.append(bot.shape[1])
                col_ranges = [(edges[i], edges[i + 1]) for i in range(len(cap_clusters))]
            else:
                col_fg = strip_fg.any(axis=0)
                col_ranges = [(x0, x1) for x0, x1 in spans_1d(col_fg, min_gap=4) if x1 - x0 >= 30]
        else:
            col_fg = strip_fg.any(axis=0)
            col_ranges = [(x0, x1) for x0, x1 in spans_1d(col_fg, min_gap=4) if x1 - x0 >= 30]

        row_boxes: list[tuple[tuple[int, int, int, int], str]] = []
        for cx0, cx1 in col_ranges:
            cell = strip[:, cx0:cx1, :]
            tight_box = strip_top_and_bottom_captions(cell, bg)
            if tight_box is None:
                continue
            bx0, by0, bx1, by1 = tight_box
            if (by1 - by0) < 60:
                continue
            sub_fg = foreground_mask(cell[by0:by1, bx0:bx1], bg)
            sub_txt = text_pixel_mask(cell[by0:by1, bx0:bx1])
            if sub_txt.sum() / max(1, sub_fg.sum()) > 0.40:
                continue
            gx0, gy0 = cx0 + bx0, split_y + ry0 + by0
            gx1, gy1 = cx0 + bx1, split_y + ry0 + by1
            row_boxes.append(((gx0, gy0, gx1, gy1), "labeled sprite (caption stripped)"))

        if row_boxes:
            rows_out.append(row_boxes)
    return rows_out


def build_contact_sheet(records: list[SpriteRecord], out_path: Path) -> None:
    """Build a labeled thumbnail grid _contact-sheet.png."""
    thumb_w, thumb_h = 220, 220
    label_h = 24
    pad = 10
    cols = 7
    rows = max(1, (len(records) + cols - 1) // cols)
    canvas = Image.new("RGB", (cols * (thumb_w + pad) + pad, rows * (thumb_h + label_h + pad) + pad), (32, 34, 38))
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()

    for idx, rec in enumerate(records):
        r, c = divmod(idx, cols)
        x0 = pad + c * (thumb_w + pad)
        y0 = pad + r * (thumb_h + label_h + pad)
        draw.rectangle([x0, y0, x0 + thumb_w - 1, y0 + thumb_h - 1], fill=(44, 46, 52), outline=(64, 68, 76))
        im = rec.image.copy()
        im.thumbnail((thumb_w - 8, thumb_h - 8), Image.LANCZOS)
        px = x0 + (thumb_w - im.width) // 2
        py = y0 + (thumb_h - im.height) // 2
        if im.mode == "RGBA":
            canvas.paste(im, (px, py), im)
        else:
            canvas.paste(im, (px, py))
        draw.text((x0 + 4, y0 + thumb_h + 4), f"{rec.filename} ({rec.size[0]}x{rec.size[1]})", fill=(220, 220, 220), font=font)

    canvas.save(out_path)


def write_index_md(dungeon_name: str, src_name: str, records: list[SpriteRecord], out_path: Path) -> None:
    """Write INDEX.md matching repository convention."""
    lines = [
        f"# {dungeon_name} — sprite cuts",
        "",
        f"Source: `images/Tilesets/{src_name}`",
        "",
        f"Total sprites: {len(records)}",
        "",
        "| file | size | source box (x,y)-(x,y) | mean RGB | note |",
        "|---|---|---|---|---|",
    ]
    for rec in records:
        x0, y0, x1, y1 = rec.box
        lines.append(
            f"| {rec.filename} | {rec.size[0]}x{rec.size[1]} | ({x0},{y0})-({x1},{y1}) | {rec.mean_rgb} | {rec.note} |"
        )
    out_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def split_sheet(src_path: Path, out_dir: Path | None = None) -> tuple[Path, int]:
    """Split one tileset .jpg into individual PNGs, _contact-sheet.png, and INDEX.md."""
    stem = src_path.stem
    if out_dir is None:
        out_dir = DEFAULT_OUT_ROOT / stem
    out_dir.mkdir(parents=True, exist_ok=True)

    for old in out_dir.glob("r*_*.png"):
        old.unlink()

    sheet = Image.open(src_path).convert("RGB")
    arr = np.asarray(sheet).copy()
    bg = estimate_bg(arr)

    spec = SHEET_SPECS.get(stem)
    if spec is None:
        spec = SheetSpec("diorama_top", split_y=arr.shape[0] // 2, title_y_end=150)

    if spec.layout == "grid_top":
        top_rows = extract_top_grid(arr, bg, spec.split_y, force_8col=(stem in {"Blackfathom_Deeps", "Razorfen"}))
    else:
        top_rows = extract_top_diorama(arr, bg, spec.split_y, spec.title_y_end, spec.title_boxes)

    bot_rows = extract_bottom_sprites(
        arr, bg, spec.split_y, spec.denizen_header_y, has_captions=(stem != "Wailing_Caverns")
    )
    all_rows = top_rows + bot_rows

    records: list[SpriteRecord] = []
    for r_idx, row_items in enumerate(all_rows, start=1):
        for c_idx, (box, note) in enumerate(row_items, start=1):
            x0, y0, x1, y1 = box
            crop_arr = arr[y0:y1, x0:x1]
            crop = Image.fromarray(crop_arr)
            if is_full_bleed(crop_arr, bg):
                out_img = crop
            else:
                out_img = key_out_edges(crop, bg)
                bb = out_img.getbbox()
                if bb is None:
                    continue
                out_img = out_img.crop(bb)
                x0, y0, x1, y1 = x0 + bb[0], y0 + bb[1], x0 + bb[2], y0 + bb[3]

            w, h = out_img.size
            if w < 18 or h < 18:
                continue

            fname = f"r{r_idx}_{c_idx:02d}.png"
            out_img.save(out_dir / fname)

            rgb_pixels = np.asarray(out_img.convert("RGB"))
            if out_img.mode == "RGBA":
                alpha = np.asarray(out_img)[:, :, 3] > 0
                pts = rgb_pixels[alpha] if alpha.any() else rgb_pixels.reshape(-1, 3)
            else:
                pts = rgb_pixels.reshape(-1, 3)
            mean_rgb = tuple(int(v) for v in pts.mean(axis=0))

            records.append(SpriteRecord(
                filename=fname,
                box=(x0, y0, x1, y1),
                size=(w, h),
                mean_rgb=mean_rgb,
                note=note,
                image=out_img,
            ))

    build_contact_sheet(records, out_dir / "_contact-sheet.png")
    write_index_md(stem, src_path.name, records, out_dir / "INDEX.md")
    return out_dir, len(records)


def main() -> None:
    parser = argparse.ArgumentParser(description="Split dungeon tileset .jpg sheets into sprite PNGs.")
    parser.add_argument("sheet", nargs="?", help="Path to a .jpg tileset sheet")
    parser.add_argument("--out-dir", type=Path, default=None, help="Output directory (default: images/Tilesets/sprites/<DungeonName>)")
    parser.add_argument("--all", action="store_true", help="Split all 14 dungeon sheets in images/Tilesets/")
    args = parser.parse_args()

    if args.all:
        tileset_dir = ROOT / "images/Tilesets"
        for jpg in sorted(tileset_dir.glob("*.jpg")):
            if jpg.name in SKIP_SHEETS:
                continue
            out_dir, count = split_sheet(jpg)
            print(f"{jpg.name}: {count} sprites -> {out_dir.relative_to(ROOT)}")
    elif args.sheet:
        src = Path(args.sheet)
        if not src.is_absolute():
            src = (Path.cwd() / src).resolve()
        out_dir, count = split_sheet(src, args.out_dir)
        print(f"{src.name}: {count} sprites -> {out_dir}")
    else:
        parser.error("Provide a sheet path or --all")


if __name__ == "__main__":
    main()
