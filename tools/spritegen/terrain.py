"""Terrain tiles: 2:1 dimetric diamonds (64x80) + flat overlays (64x32).

Diamond top face occupies top 32 rows of the 64x80 frame. The diamond spans
the full 64px width and 32px height: top vertex at (32,0), left (0,16),
right (63,16), bottom (32,31). The cliff skirt hangs below (rows 32..79),
stratified rock with hanging roots, ragged-fading into transparency.

SEAMLESS TILING: the top face is the load-bearing seam. Detail on the top
face is generated from WORLD-position-independent symmetric noise so that the
left edge of one diamond meets the right edge of its neighbour cleanly. We
keep the outer 1px diamond border consistent across all variants/tiles.
"""

from __future__ import annotations
import math
from PIL import Image
from . import core
from .core import Canvas, shade, lerp, material_ramp, dither_at

FRAME_W, FRAME_H = 64, 80
TOP_H = 32  # diamond face height
CX = 32     # diamond centre x (vertex)
# Diamond geometry: top vertex (32,0), spans to y=31. half_w=32, half_h=16,
# but the face is the TOP 32 rows so it is a full diamond of height 32.


def _diamond_rows():
    """Yield (y, x0, x1) scanlines of the top-face diamond (64 wide, 32 tall)."""
    for y in range(TOP_H):
        # diamond: at y=0 width 1 (vertex), at y=15/16 full width, y=31 width 1
        d = abs(y - 15.5)
        frac = 1.0 - d / 15.5
        xext = int(round(31.5 * frac))
        x0 = CX - xext
        x1 = CX - 1 + xext + 1  # symmetric around 31.5
        # clamp
        x0 = max(0, x0)
        x1 = min(63, x1)
        yield y, x0, x1


def _on_diamond(x, y):
    if y >= TOP_H:
        return False
    d = abs(y - 15.5)
    frac = 1.0 - d / 15.5
    xext = 31.5 * frac
    return abs(x - 31.5) <= xext + 0.001


def _skirt_extent(y):
    """Horizontal extent of the cliff skirt at screen row y (y>=16).

    Below the diamond mid (y>=16) the rock face hangs. Width tapers as it goes
    down to give the floating-island chunk a ragged bottom.
    """
    # full width near top of skirt, narrowing toward bottom
    top = 16
    bottom = FRAME_H - 1
    if y < top:
        return None
    t = (y - top) / (bottom - top)
    # follow the diamond's lower edges down to y=31, then taper inwards
    if y <= 31:
        d = abs(y - 15.5)
        frac = 1.0 - d / 15.5
        xext = 31.5 * frac
        return (int(round(31.5 - xext)), int(round(31.5 + xext)))
    # below diamond: taper from full (at 31) to ragged narrow at bottom
    tt = (y - 31) / (bottom - 31)
    base_ext = 31.5 * (1.0 - 0.55 * tt)
    return (int(round(31.5 - base_ext)), int(round(31.5 + base_ext)))


# ---------------------------------------------------------------------------
# Cliff skirt — shared across all diamond tiles for consistent island look
# ---------------------------------------------------------------------------
def draw_cliff(c: Canvas, name: str, rock_base=core.STONE, rooty=True):
    rng = core.rng_for("cliff_" + name)
    rock = material_ramp(rock_base, 5, 0.4, 1.4)
    dirt = material_ramp(core.TIMBER, 5, 0.5, 1.3)
    for y in range(16, FRAME_H):
        ext = _skirt_extent(y)
        if ext is None:
            continue
        x0, x1 = ext
        # ragged bottom: random erosion on the lowest rows
        erode = 0
        if y > FRAME_H - 14:
            tt = (y - (FRAME_H - 14)) / 13.0
            erode = int(tt * 6)
        for x in range(x0, x1 + 1):
            # only the part BELOW the diamond face (skirt), but rows 16..31 are
            # the diamond's own lower triangle — those belong to the top face,
            # skip them here (drawn by surface fn). Draw skirt only y>=32 plus
            # a 1px lip echo handled by surface.
            if y < 32:
                continue
            # ragged edges near bottom
            edge_d = min(x - x0, x1 - x)
            if y > FRAME_H - 14 and edge_d < erode and rng.random() < 0.6:
                continue
            # bottom fade to transparency
            alpha = 255
            if y > FRAME_H - 16:
                ft = (y - (FRAME_H - 16)) / 15.0
                alpha = int(255 * (1.0 - ft * ft))
                if rng.random() < ft * 0.5:
                    continue
            # stratified horizontal rock bands
            band = (y // 4) % 2
            depth_t = (y - 32) / (FRAME_H - 32)
            # vertical lighting: left side lighter (key light top-left)
            lt = (x - x0) / max(1, (x1 - x0))
            idx = 1 + int((1 - lt) * 2) - band
            idx = max(0, min(4, idx))
            col = rock[idx]
            # darker toward the bottom (in shadow / fading into cloud)
            col = shade(col, 1.0 - 0.35 * depth_t)
            # dirt seam near the top of the skirt
            if y < 38 and dither_at(x, y, 0.5):
                col = dirt[idx]
            c.set(x, y, (col[0], col[1], col[2], alpha))

        # vertical crack striations
    # cracks
    for _ in range(rng.randint(3, 5)):
        sx = rng.randint(10, 53)
        sy = 33
        x = sx
        for y in range(sy, FRAME_H - 8):
            ext = _skirt_extent(y)
            if ext and ext[0] < x < ext[1]:
                c.set(x, y, shade(rock_base, 0.35))
                if rng.random() < 0.4:
                    x += rng.choice([-1, 0, 1])

    if rooty:
        _hang_roots(c, name)


def _hang_roots(c: Canvas, name: str):
    rng = core.rng_for("roots_" + name)
    root_c = material_ramp(core.TIMBER, 4, 0.4, 1.2)
    moss = material_ramp(core.LEAF_D, 3, 0.6, 1.3)
    n = rng.randint(4, 7)
    for _ in range(n):
        x = rng.randint(8, 55)
        ext = _skirt_extent(33)
        if not (ext[0] <= x <= ext[1]):
            continue
        length = rng.randint(6, 22)
        y = 33
        cur = x
        for i in range(length):
            yy = y + i
            if yy >= FRAME_H - 2:
                break
            ext = _skirt_extent(yy)
            if not ext or not (ext[0] <= cur <= ext[1] + 2):
                pass
            col = root_c[1 + (i % 2)]
            # alpha fade toward tip
            a = 255 if i < length - 4 else int(255 * (length - i) / 4)
            c.set(cur, yy, (col[0], col[1], col[2], a))
            if i < 4 and rng.random() < 0.7:
                m = moss[rng.randint(0, 2)]
                c.set(cur + rng.choice([-1, 1]), yy, m)
            if rng.random() < 0.35:
                cur += rng.choice([-1, 0, 1])


# ---------------------------------------------------------------------------
# Top-face surface painters
# ---------------------------------------------------------------------------
def _shade_face(c, x, y, tones, light_bias=0.0):
    """Pick a tone for a top-face pixel based on a stable iso lighting model."""
    # key light top-left: pixels toward top-left vertex are brighter.
    # Map within diamond using normalized iso coords.
    # u along left edge, v along right edge.
    nx = (x - 31.5) / 31.5  # -1..1
    ny = (y - 15.5) / 15.5
    # top-left brightest -> use -(nx)+-(ny)
    lite = (-nx - ny) * 0.5 + 0.5 + light_bias  # 0..1
    idx = int(lite * (len(tones) - 1) + 0.5)
    idx = max(0, min(len(tones) - 1, idx))
    return tones[idx]


def _rim_lip(c, tones):
    """Draw the bright top edges + dark lower edges of the diamond (the 'lip')."""
    light = tones[-1]
    dark = tones[0]
    # walk the four edges
    for y in range(TOP_H):
        d = abs(y - 15.5)
        frac = 1.0 - d / 15.5
        xext = int(round(31.5 * frac))
        xl = 31 - xext + (0 if xext else 0)
        xr = 32 + xext - (1 if xext else 0)
        xl = max(0, xl)
        xr = min(63, xr)
        if y <= 15:  # upper edges -> rim light
            c.set(xl, y, light)
            c.set(xl + 1, y, lerp(light, tones[len(tones)//2], 0.5))
            c.set(xr, y, lerp(light, tones[len(tones)//2], 0.4))
        else:  # lower edges -> shadow lip
            c.set(xl, y, dark)
            c.set(xr, y, dark)


def _fill_top(c, base, rng, soil=False, mossy=False, rocky=False, wet=False,
              gold=False, light_bias=0.0):
    tones = material_ramp(base, 5, 0.5, 1.45)
    for y, x0, x1 in _diamond_rows():
        for x in range(x0, x1 + 1):
            if not _on_diamond(x, y):
                continue
            col = _shade_face(c, x, y, tones, light_bias)
            # texture
            if soil:
                # furrows along the iso grain
                if (x + 2 * y) % 6 == 0:
                    col = shade(col, 0.82)
                elif (x + 2 * y) % 6 == 3 and dither_at(x, y, 0.5):
                    col = shade(col, 1.12)
                if rng.random() < 0.05:
                    col = shade(col, 0.7)
            if rocky:
                if dither_at(x, y, 0.35):
                    col = shade(col, 0.85)
                if rng.random() < 0.04:
                    col = tones[0]
            if mossy:
                if dither_at(x, y, 0.4):
                    col = lerp(col, core.LEAF_D, 0.45)
                if rng.random() < 0.06:
                    col = lerp(col, core.LEAF_L, 0.5)
            if gold:
                if dither_at(x, y, 0.5):
                    col = lerp(col, core.CROP_GOLD, 0.4)
            if wet:
                if dither_at(x, y, 0.3):
                    col = shade(col, 0.78)
            c.set(x, y, col)
    _rim_lip(c, tones)


# ---------------------------------------------------------------------------
# Tile generators (3 variants each unless noted)
# ---------------------------------------------------------------------------
def _tile(name, variant, painter, rock_base=core.STONE):
    c = Canvas(FRAME_W, FRAME_H)
    draw_cliff(c, f"{name}_{variant}", rock_base=rock_base)
    rng = core.rng_for(f"{name}_top_{variant}")
    painter(c, rng, variant)
    # unify outline on the diamond's upper silhouette only is unnecessary; the
    # rim handles top, cliff handles bottom. Add a subtle dark contact line at
    # the diamond bottom vertex region for grounding:
    return c.to_image()


def fertile_valley():
    def paint(c, rng, v):
        base = lerp(core.TIMBER, core.LEAF_D, 0.35)
        base = shade(base, [1.0, 1.08, 0.95][v])
        _fill_top(c, base, rng, soil=True, mossy=True)
        # scattered grass tufts + golden specks
        for _ in range(rng.randint(8, 14)):
            x = rng.randint(6, 57)
            y = rng.randint(3, 28)
            if _on_diamond(x, y):
                g = core.LEAF_L if rng.random() < 0.6 else core.CROP_GOLD
                c.set(x, y, g)
                c.set(x, y - 1, shade(g, 1.2))
    return [_tile("fertile_valley", v, paint) for v in range(3)]


def rocky_plateau():
    def paint(c, rng, v):
        base = shade(core.STONE, [1.0, 1.1, 0.92][v])
        _fill_top(c, base, rng, rocky=True)
        # embedded boulders
        for _ in range(rng.randint(3, 6)):
            x = rng.randint(10, 53)
            y = rng.randint(6, 25)
            if _on_diamond(x, y):
                r = rng.randint(2, 4)
                c.disc(x, y, r, shade(core.STONE, 0.8))
                c.disc(x, y - 1, r - 1, shade(core.STONE, 1.15))
                c.set(x - r + 1, y - r + 1, shade(core.STONE, 1.4))
    return [_tile("rocky_plateau", v, paint) for v in range(3)]


def cliff_edge():
    """Like fertile/rocky blend but with a visibly broken edge facing camera."""
    def paint(c, rng, v):
        base = lerp(core.STONE, core.TIMBER, 0.3)
        base = shade(base, [1.0, 1.05, 0.9][v])
        _fill_top(c, base, rng, rocky=True, mossy=True)
        # crumble pebbles near the lower-right edge
        for _ in range(rng.randint(5, 9)):
            x = rng.randint(34, 60)
            y = rng.randint(16, 30)
            if _on_diamond(x, y):
                c.set(x, y, shade(core.STONE, 0.7))
    return [_tile("cliff_edge", v, paint, rock_base=shade(core.STONE, 0.92)) for v in range(3)]


def natural_spring():
    """Water-bearing tile with a shimmering pool; variants = shimmer phase."""
    def paint(c, rng, v):
        base = lerp(core.TIMBER, core.LEAF_D, 0.4)
        _fill_top(c, base, rng, mossy=True, wet=True)
        # central pool: small diamond of water
        water = material_ramp(lerp(core.STORM, core.STONE, 0.3), 5, 0.5, 1.7)
        pcx, pcy = 32, 17
        for y in range(pcy - 7, pcy + 7):
            d = abs(y - pcy)
            frac = 1.0 - d / 7.0
            if frac <= 0:
                continue
            xext = int(round(13 * frac))
            for x in range(pcx - xext, pcx + xext + 1):
                # shimmer: phase-shifted highlight bands
                phase = v * 2
                band = math.sin((x * 0.6 + y * 1.1 + phase))
                idx = 2 + (1 if band > 0.4 else -1 if band < -0.4 else 0)
                col = water[max(0, min(4, idx))]
                c.set(x, y, lerp(col, core.AMBER, 0.06 if band > 0.8 else 0))
        # bright shimmer specks
        for _ in range(6):
            x = rng.randint(pcx - 9, pcx + 9)
            y = rng.randint(pcy - 4, pcy + 4)
            if (abs(y-pcy)/7.0) + (abs(x-pcx)/14.0) < 0.9:
                c.set(x, y, lerp(core.FOG, core.AMBER, 0.3))
        # wet stone rim
        for y in range(pcy - 8, pcy + 8):
            d = abs(y - pcy)
            frac = 1.0 - d / 8.0
            if frac <= 0:
                continue
            xext = int(round(15 * frac))
            for xx in (pcx - xext, pcx + xext):
                c.set(xx, y, shade(core.STONE, 0.7))
    return [_tile("natural_spring", v, paint) for v in range(3)]


def wind_corridor():
    """Wind-scoured ground; pale streaks; variants shift the streaks."""
    def paint(c, rng, v):
        base = shade(lerp(core.STONE, core.TIMBER, 0.45), 1.05)
        _fill_top(c, base, rng, rocky=True)
        # wind streaks across the iso surface (left-down direction)
        for s in range(5):
            x = 8 + s * 11 + v * 3
            y = 4
            for i in range(26):
                xx = x - i // 2
                yy = y + i // 2
                if _on_diamond(xx, yy):
                    c.set(xx, yy, lerp(base, core.FOG, 0.4))
                if _on_diamond(xx + 1, yy):
                    c.set(xx + 1, yy, lerp(base, core.FOG, 0.2))
    return [_tile("wind_corridor", v, paint) for v in range(3)]


def scaffold():
    """Expansion platform: timber planks laid across an iso diamond, 1 frame."""
    c = Canvas(FRAME_W, FRAME_H)
    draw_cliff(c, "scaffold", rock_base=shade(core.STONE, 0.85), rooty=False)
    rng = core.rng_for("scaffold_top")
    tones = material_ramp(core.TIMBER, 5, 0.45, 1.4)
    # planks run along one iso axis: draw as parallel iso strips
    plank_w = 5
    for y, x0, x1 in _diamond_rows():
        for x in range(x0, x1 + 1):
            if not _on_diamond(x, y):
                continue
            # iso plank index along (x - 2y)
            k = (x - 2 * y)
            pidx = (k // plank_w) % 2
            grain = math.sin(x * 1.3 + y * 0.5)
            base_i = 2 + pidx + (1 if grain > 0.6 else -1 if grain < -0.6 else 0)
            base_i = max(0, min(4, base_i))
            col = tones[base_i]
            # plank gap shadow lines
            if (k % plank_w) == 0:
                col = tones[0]
            c.set(x, y, col)
    # iron bolts at plank crossings
    for _ in range(8):
        x = rng.randint(10, 53)
        y = rng.randint(4, 27)
        if _on_diamond(x, y):
            c.set(x, y, shade(core.RUST, 1.2))
            c.set(x, y, shade(core.STONE, 1.4))
    _rim_lip(c, tones)
    return [c.to_image()]


# ---------------------------------------------------------------------------
# Flat overlays (64x32) — diamond-only, transparent elsewhere
# ---------------------------------------------------------------------------
def _overlay(painter, name):
    c = Canvas(64, 32)
    rng = core.rng_for("overlay_" + name)
    painter(c, rng)
    return c.to_image()


def _overlay_diamond_rows():
    for y in range(32):
        d = abs(y - 15.5)
        frac = 1.0 - d / 15.5
        xext = int(round(31.5 * frac))
        yield y, 32 - xext, 31 + xext


def overlay_tilled():
    def paint(c, rng):
        base = material_ramp(shade(core.TIMBER, 0.8), 5, 0.55, 1.25)
        for y, x0, x1 in _overlay_diamond_rows():
            for x in range(x0, x1 + 1):
                # tilled furrows along iso axis
                furrow = (x + 2 * y) % 5
                i = 2
                if furrow == 0:
                    i = 0
                elif furrow == 1:
                    i = 4
                col = base[i]
                if rng.random() < 0.06:
                    col = base[0]
                c.set(x, y, col)
        _overlay_rim(c, base)
    return _overlay(paint, "tilled")


def overlay_wet():
    def paint(c, rng):
        base = material_ramp(shade(core.TIMBER, 0.5), 5, 0.5, 1.2)
        for y, x0, x1 in _overlay_diamond_rows():
            for x in range(x0, x1 + 1):
                furrow = (x + 2 * y) % 5
                i = 1 if furrow else 0
                col = base[i]
                # wet sheen specks
                if dither_at(x, y, 0.18):
                    col = lerp(col, core.STORM, 0.5)
                c.set(x, y, col)
        _overlay_rim(c, base)
    return _overlay(paint, "wet")


def overlay_dry():
    def paint(c, rng):
        base = material_ramp(lerp(core.TIMBER, core.FOG, 0.25), 5, 0.6, 1.25)
        for y, x0, x1 in _overlay_diamond_rows():
            for x in range(x0, x1 + 1):
                col = base[3]
                if dither_at(x, y, 0.3):
                    col = base[2]
                c.set(x, y, col)
        # cracks
        for _ in range(6):
            x = rng.randint(14, 49)
            y = rng.randint(6, 25)
            for i in range(rng.randint(3, 7)):
                if 0 <= y < 32:
                    d = abs(y - 15.5); frac = 1 - d/15.5; xext = 31.5*frac
                    if abs(x - 31.5) <= xext:
                        c.set(x, y, base[0])
                x += rng.choice([-1, 0, 1])
                y += rng.choice([0, 1])
        _overlay_rim(c, base)
    return _overlay(paint, "dry")


def _overlay_rim(c, base):
    for y in range(32):
        d = abs(y - 15.5)
        frac = 1.0 - d / 15.5
        xext = int(round(31.5 * frac))
        xl = 32 - xext
        xr = 31 + xext
        if y <= 15:
            c.set(xl, y, base[-1]); c.set(xr, y, base[3])
        else:
            c.set(xl, y, base[0]); c.set(xr, y, base[0])


# ---------------------------------------------------------------------------
def generate():
    core.save_strip(fertile_valley(), "terrain/tile_fertile_valley.png", 64, 80)
    core.save_strip(rocky_plateau(), "terrain/tile_rocky_plateau.png", 64, 80)
    core.save_strip(cliff_edge(), "terrain/tile_cliff_edge.png", 64, 80)
    core.save_strip(natural_spring(), "terrain/tile_natural_spring.png", 64, 80)
    core.save_strip(wind_corridor(), "terrain/tile_wind_corridor.png", 64, 80)
    core.save_strip(scaffold(), "terrain/tile_scaffold.png", 64, 80)
    core.save_single(overlay_tilled(), "terrain/overlay_tilled.png")
    core.save_single(overlay_wet(), "terrain/overlay_wet.png")
    core.save_single(overlay_dry(), "terrain/overlay_dry.png")


if __name__ == "__main__":
    generate()
    print("terrain done")
