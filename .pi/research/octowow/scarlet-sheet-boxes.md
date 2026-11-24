# Scarlet sheet tile boxes — pixel statistics, no eyes

Sheet: `images/Tilesets/Gemini_Generated_Image_Scarlet_Monastery.jpg`, true size **2400x1792**.

## The 300px-cell hypothesis is REFUTED for this sheet

The sibling wailing sheet's lattice (~300px cells, pale floors col 2, gray wall col 4)
does not transfer: on the scarlet sheet a 300px-lattice scan (8x6 cells, 25px inset)
finds **no pale-stone cell anywhere** — every cell is dark (mean RGB luminance 50-130)
and red/brown-heavy. The art is a dark painted dungeon scene, not a tile grid of
pale stone. Picks below therefore come from a 250px sliding-window scan
(stride 25), ranked by measured uniformity/structure.

## Picks (x0,y0,x1,y1), each with the numbers that chose it

| name | box | edge | lumstd | RGB | cool% | why |
|---|---|---|---|---|---|---|
| `scarlet-wall` | (1875,450,2125,700) | 11.52 | 40.7 | (70,68,62) | 58 | most structured among grayish cells — mortar-line-like edge density |
| `scarlet-wall2` | (1075,825,1325,1075) | 11.47 | 51.7 | (80,75,74) | 36 | second distinct structured grayish tone, for wall variety |
| `scarlet-floor0` | (2100,1200,2350,1450) | 0.81 | 0.8 | (49,50,55) | 0 | flattest box on the sheet — dark slate floor, seam-safe |
| `scarlet-floor1` | (975,1525,1225,1775) | 0.81 | 0.9 | (49,50,55) | 0 | second flat box, same palette (interchangeable with floor0) |

`shortcut: boxes chosen by pixel statistics without eyes; the flat cells may be
painted background rather than authored floor tiles. If the SM tour looks wrong,
re-pick from the scan (rerun the stats script; candidates recorded in
/tmp/ctx-exec-* or rederive: 250px windows, stride 25, rank by edge density).`

## Deliberately skipped

- **Ornate floor**: the only mid-edge warm cells (around (525,925), edge 8.8,
  lumstd 60.7) carry high luminance variance — more likely painted objects or
  banners than flat ornate floor. None met the "clearly ornate, flat
  background" bar, so no ornate tile ships.
- **Object decoration sprite**: no cell showed the small-bright-blob-on-flat
  signature. None ships.
