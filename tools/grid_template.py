#!/usr/bin/env python3
"""
grid_template.py - Render the SkyHarvest starter-island grid as a control template.

This is the structural reference for AI art generation (ControlNet / img2img base)
AND for in-game alignment. It uses the EXACT game projection so a painting made on
top of it lands on the real logical grid.

Projection (from Assets/Scripts/Core/GridMath.cs, Constants.cs):
    wx = (gx - gy) * 0.5
    wy = (gx + gy) * -0.25 + elevation * 0.25      (ElevationWorldStep = 0.25)
    PixelsPerUnit = 64 ; tile diamond = 1.0w x 0.5h world = 64x32 px

Starter island layout (the designed hero piece):
    3 wide (gx 0..2) x 4 deep (gy 0..3)
    BACK / raised tier  (FORGE) : gy in {0,1}, elevation = +1
    FRONT / lower tier  (FARM)  : gy in {2,3}, elevation =  0
    Stone wall along the gy1|gy2 boundary (3 cells wide).
    STAIRS carved (in tutorial) at the MIDDLE boundary cell: gx = 1.
    Back tier is unwalkable until the stairs are mined.
"""
import os
from PIL import Image, ImageDraw, ImageFont

# ---- exact game constants -------------------------------------------------
PPU = 64
ELEV_STEP = 0.5      # Constants.ElevationWorldStep (chunkier wall)
HALF_W = 0.5      # TileWorldWidth  / 2
HALF_H = 0.25     # TileWorldHeight / 2

# render at higher res than the game so the template is crisp for ControlNet
SCALE = 4                      # render px per world unit = PPU * SCALE = 256
RPPU = PPU * SCALE
MARGIN = 90

# ---- island definition ----------------------------------------------------
W, D = 3, 4                    # width (gx), depth (gy)
BACK_ROWS = {0, 1}            # raised tier
FRONT_ROWS = {2, 3}          # lower farm tier
STAIR_GX = 1                  # middle of the 3-wide boundary

def elev(gy):
    return 1 if gy in BACK_ROWS else 0

def grid_to_world(gx, gy, e):
    wx = (gx - gy) * 0.5
    wy = (gx + gy) * -0.25 + e * ELEV_STEP
    return wx, wy

def diamond_corners(gx, gy, e):
    """4 corners of the cell's diamond in world space (centered on GridToWorld)."""
    wx, wy = grid_to_world(gx, gy, e)
    return {
        "top":    (wx, wy + HALF_H),
        "right":  (wx + HALF_W, wy),
        "bottom": (wx, wy - HALF_H),
        "left":   (wx - HALF_W, wy),
    }

# ---- compute bounds (include wall extrusion downward) ---------------------
all_pts = []
for gx in range(W):
    for gy in range(D):
        c = diamond_corners(gx, gy, elev(gy))
        all_pts.extend(c.values())
# wall drops from boundary raised cells down by one elevation step
for gx in range(W):
    c = diamond_corners(gx, 1, 1)
    all_pts.append((c["bottom"][0], c["bottom"][1] - ELEV_STEP))
    all_pts.append((c["left"][0],   c["left"][1]   - ELEV_STEP))

min_wx = min(p[0] for p in all_pts)
max_wx = max(p[0] for p in all_pts)
min_wy = min(p[1] for p in all_pts)
max_wy = max(p[1] for p in all_pts)

IMG_W = int((max_wx - min_wx) * RPPU) + 2 * MARGIN
IMG_H = int((max_wy - min_wy) * RPPU) + 2 * MARGIN

def w2px(wx, wy):
    px = (wx - min_wx) * RPPU + MARGIN
    py = (max_wy - wy) * RPPU + MARGIN     # flip Y for image space
    return (px, py)

# ---- colours --------------------------------------------------------------
BG       = (24, 22, 28)
FARM_FILL = (74, 56, 38)
FARM_EDGE = (150, 116, 78)
FORGE_FILL = (52, 70, 44)
FORGE_EDGE = (120, 168, 96)
WALL_FILL = (78, 74, 70)
WALL_EDGE = (150, 146, 140)
STAIR_FILL = (170, 120, 40)
STAIR_EDGE = (255, 196, 90)
TEXT     = (235, 232, 226)
DIM      = (140, 136, 130)

def font(size):
    for name in ("seguibl.ttf", "segoeui.ttf", "arial.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()

img = Image.new("RGB", (IMG_W, IMG_H), BG)
dr = ImageDraw.Draw(img, "RGBA")

def poly(corner_dict, order, fill, edge, width=3):
    pts = [w2px(*corner_dict[k]) for k in order]
    dr.polygon(pts, fill=fill, outline=edge)
    # redraw outline thicker
    pts2 = pts + [pts[0]]
    dr.line(pts2, fill=edge, width=width)

def centered(text, cx, cy, fnt, fill):
    bb = dr.textbbox((0, 0), text, font=fnt)
    dr.text((cx - (bb[2]-bb[0]) / 2, cy - (bb[3]-bb[1]) / 2), text, font=fnt, fill=fill)

f_small = font(20)
f_cell  = font(22)
f_tag   = font(30)
f_title = font(34)

# ---- draw order: farther (smaller gx+gy) first so nearer overlaps ---------
cells = sorted(
    [(gx, gy) for gx in range(W) for gy in range(D)],
    key=lambda c: (c[0] + c[1])
)

# 1) draw the wall faces first (they sit behind the lower tier visually)
for gx in range(W):
    c = diamond_corners(gx, 1, 1)
    bottom = c["bottom"]; left = c["left"]
    b_lo = (bottom[0], bottom[1] - ELEV_STEP)
    l_lo = (left[0],   left[1]   - ELEV_STEP)
    face = [w2px(*bottom), w2px(*left), w2px(*l_lo), w2px(*b_lo)]
    is_stair = (gx == STAIR_GX)
    dr.polygon(face, fill=(STAIR_FILL if is_stair else WALL_FILL) + (255,),
               outline=(STAIR_EDGE if is_stair else WALL_EDGE))
    dr.line(face + [face[0]], fill=(STAIR_EDGE if is_stair else WALL_EDGE), width=3)
    if is_stair:
        mx = sum(p[0] for p in face) / 4
        my = sum(p[1] for p in face) / 4
        centered("STAIRS", mx, my, f_small, (20, 16, 10))

# 2) draw the tile diamonds
for gx, gy in cells:
    e = elev(gy)
    c = diamond_corners(gx, gy, e)
    if gy in BACK_ROWS:
        poly(c, ["top", "right", "bottom", "left"], FORGE_FILL + (255,), FORGE_EDGE)
    else:
        poly(c, ["top", "right", "bottom", "left"], FARM_FILL + (255,), FARM_EDGE)
    cx, cy = w2px(*grid_to_world(gx, gy, e))
    centered(f"{gx},{gy}", cx, cy - 4, f_cell, TEXT)

# 3) tier tags
def tier_centroid(rows):
    xs, ys = [], []
    for gx in range(W):
        for gy in rows:
            px, py = w2px(*grid_to_world(gx, gy, elev(gy)))
            xs.append(px); ys.append(py)
    return sum(xs)/len(xs), sum(ys)/len(ys)

fx, fy = tier_centroid(FRONT_ROWS)
centered("FARM  (lower tier, elev 0)", fx, fy + 70, f_tag, FARM_EDGE)
bx, by = tier_centroid(BACK_ROWS)
centered("FORGE  (raised tier, elev +1)", bx, by - 78, f_tag, FORGE_EDGE)

# ---- title / legend -------------------------------------------------------
dr.text((MARGIN - 30, 18), "SkyHarvest starter island - grid control template",
        font=f_title, fill=TEXT)
notes = [
    "2:1 dimetric  |  wx=(x-y)*0.5  wy=(x+y)*-0.25 + elev*0.5  |  64 px/unit",
    f"render scale {SCALE}x ({RPPU} px/unit)  |  3 wide x 4 deep  |  back tier raised +1 step",
    "STAIRS carved in tutorial at boundary cell gx=1; back tier locked until mined",
]
for i, n in enumerate(notes):
    dr.text((MARGIN - 30, IMG_H - 84 + i * 24), n, font=f_small, fill=DIM)

out = os.path.join("artifacts", "grid_template.png")
os.makedirs("artifacts", exist_ok=True)
img.save(out)
print(f"wrote {out}  ({IMG_W}x{IMG_H})")
