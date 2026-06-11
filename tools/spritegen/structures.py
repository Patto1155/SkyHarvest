"""Structures / workshops: hand-built salvage look.

Timber frames, rusted metal, rope lashings, lit amber windows. Sizes per
manifest. Forge pulses warm orange from within (4 frames); mill sails rotate
(4 frames); lit windows amber.

All structures pivot bottom-center and sit on a tile, so they're anchored to
a baseline near the frame bottom with a small ground-contact shadow.
"""

from __future__ import annotations
import math
from PIL import Image
from . import core
from .core import Canvas, shade, lerp, material_ramp, dither_at

# material ramps
TIMBER_R = material_ramp(core.TIMBER, 5, 0.45, 1.45)
DARKWOOD_R = material_ramp(shade(core.TIMBER, 0.7), 5, 0.45, 1.4)
STONE_R = material_ramp(core.STONE, 5, 0.45, 1.5)
RUST_R = material_ramp(core.RUST, 5, 0.45, 1.4)
IRON_R = material_ramp(shade(core.STONE, 0.7), 5, 0.4, 1.6)
ROPE = lerp(core.TIMBER, core.FOG, 0.35)
ROPE_R = material_ramp(ROPE, 4, 0.6, 1.2)
THATCH_R = material_ramp(lerp(core.TIMBER, core.CROP_GOLD, 0.4), 5, 0.5, 1.3)


def _shadow(c, cx, by, rx, ry=4):
    c.shadow_ellipse(cx, by, rx, ry, alpha=85)


def _plank_wall(c, x0, y0, x1, y1, ramp, rng, horizontal=False):
    """Timber plank wall with grain + gaps + a few nails."""
    if horizontal:
        ph = 4
        for y in range(y0, y1 + 1):
            row = (y - y0) // ph
            for x in range(x0, x1 + 1):
                grain = math.sin(x * 1.1 + row * 2)
                i = 2 + row % 2 + (1 if grain > 0.7 else -1 if grain < -0.7 else 0)
                col = ramp[max(0, min(len(ramp) - 1, i))]
                if (y - y0) % ph == 0:
                    col = ramp[0]
                c.set(x, y, col)
    else:
        pw = 5
        for x in range(x0, x1 + 1):
            col_i = (x - x0) // pw
            for y in range(y0, y1 + 1):
                grain = math.sin(y * 0.9 + col_i * 2)
                i = 2 + col_i % 2 + (1 if grain > 0.7 else -1 if grain < -0.7 else 0)
                col = ramp[max(0, min(len(ramp) - 1, i))]
                if (x - x0) % pw == 0:
                    col = ramp[0]
                if rng.random() < 0.01:
                    col = ramp[0]
                c.set(x, y, col)
        # nails
        for cx in range(x0 + 2, x1, pw):
            c.set(cx, y0 + 1, IRON_R[3])
            c.set(cx, y1 - 1, IRON_R[3])


def _lit_window(c, cx, cy, w, h, glow=core.AMBER):
    """An amber-lit window with timber frame."""
    c.rect(cx - w - 1, cy - h - 1, cx + w + 1, cy + h + 1, shade(core.TIMBER, 0.5))
    inner = material_ramp(glow, 4, 0.6, 1.3)
    for y in range(cy - h, cy + h + 1):
        for x in range(cx - w, cx + w + 1):
            d = abs(x - cx) + abs(y - cy)
            i = 3 - min(3, d // 2)
            c.set(x, y, inner[max(0, i)])
    # cross bar
    c.vline(cx, cy - h, cy + h, shade(core.TIMBER, 0.6))
    c.hline(cx - w, cx + w, cy, shade(core.TIMBER, 0.6))


def _rope_lash(c, x, y, length, vertical=True):
    for i in range(length):
        col = ROPE_R[1 + (i % 2)]
        if vertical:
            c.set(x, y + i, col)
            c.set(x + 1, y + i, ROPE_R[2 if i % 2 else 0])
        else:
            c.set(x + i, y, col)
            c.set(x + i, y + 1, ROPE_R[2 if i % 2 else 0])


def _finish(c, warm=False):
    c.auto_outline(core.OUTLINE_WARM if warm else core.OUTLINE)
    return c.to_image()


# ===========================================================================
# shelter 128x128
# ===========================================================================
def shelter():
    W, Hh = 128, 128
    c = Canvas(W, Hh)
    rng = core.rng_for("shelter")
    cx = 64
    base_y = 116
    _shadow(c, cx, base_y + 4, 50, 8)
    # body: a leaning salvage hut, timber walls, sloped scrap-metal roof
    wall_top = 60
    _plank_wall(c, cx - 38, wall_top, cx + 38, base_y, TIMBER_R, rng)
    # corner posts
    for px in (cx - 38, cx + 38, cx - 14, cx + 14):
        c.rect(px - 1, wall_top - 2, px + 1, base_y, DARKWOOD_R[1])
    # door
    c.rect(cx - 9, base_y - 34, cx + 9, base_y, shade(core.TIMBER, 0.45))
    _plank_wall(c, cx - 8, base_y - 33, cx + 8, base_y - 1, DARKWOOD_R, rng)
    c.set(cx + 6, base_y - 17, core.RUST)  # handle
    # lit windows flanking door
    _lit_window(c, cx - 24, base_y - 24, 5, 6)
    _lit_window(c, cx + 24, base_y - 24, 5, 6)
    # roof: corrugated rusted metal, sloped, overhanging
    roof_peak = 30
    for y in range(roof_peak, wall_top + 4):
        t = (y - roof_peak) / (wall_top + 4 - roof_peak)
        half = int(20 + t * 30)
        for x in range(cx - half, cx + half + 1):
            corr = (x // 3) % 2
            i = 2 + corr - (1 if y < roof_peak + 6 else 0)
            col = RUST_R[max(0, min(4, i))]
            # streaky rust + key light on left
            if x < cx:
                col = shade(col, 1.12)
            if dither_at(x, y, 0.2):
                col = shade(col, 0.85)
            c.set(x, y, col)
    # roof ridge + chimney with amber ember smoke
    c.hline(cx - 20, cx + 20, roof_peak, shade(core.RUST, 0.6))
    c.rect(cx + 12, roof_peak - 14, cx + 18, roof_peak + 2, shade(core.STONE, 0.7))
    c.rect(cx + 12, roof_peak - 14, cx + 18, roof_peak - 12, core.CHARCOAL)
    c.set(cx + 15, roof_peak - 16, lerp(core.AMBER, core.FORGE, 0.5))
    # rope lashings on corners
    _rope_lash(c, cx - 39, wall_top + 6, 14)
    _rope_lash(c, cx + 38, wall_top + 6, 14)
    # patched scrap plate
    c.rect(cx + 16, base_y - 20, cx + 30, base_y - 6, shade(core.STONE, 0.85))
    for nx in (cx + 17, cx + 29):
        for ny in (base_y - 19, base_y - 7):
            c.set(nx, ny, IRON_R[4])
    return [_finish(c, warm=True)]


# ===========================================================================
# rain_catcher 64x96, 2 frames (empty/full)
# ===========================================================================
def rain_catcher():
    W, Hh = 64, 96
    frames = []
    for full in (0, 1):
        c = Canvas(W, Hh)
        rng = core.rng_for(f"rain_catcher_{full}")
        cx = 32
        base_y = 88
        _shadow(c, cx, base_y + 3, 24, 5)
        # four timber legs splayed
        for lx, dir_ in ((cx - 20, -1), (cx + 20, 1)):
            c.line(lx, base_y, cx + dir_ * 8, 40, DARKWOOD_R[1])
            c.line(lx + dir_, base_y, cx + dir_ * 8 + dir_, 40, DARKWOOD_R[2])
        # funnel basin (tilted barrel/tarp catch)
        rim_y = 36
        for y in range(rim_y, rim_y + 22):
            t = (y - rim_y) / 22
            half = int(26 * (1 - t * 0.55))
            for x in range(cx - half, cx + half + 1):
                i = 2 + (1 if x < cx else -1)
                c.set(x, y, TIMBER_R[max(0, min(4, i))])
        # metal hoops
        c.hline(cx - 26, cx + 26, rim_y, shade(core.RUST, 0.7))
        c.hline(cx - 18, cx + 18, rim_y + 18, shade(core.RUST, 0.7))
        # water inside
        if full:
            water = material_ramp(lerp(core.STORM, core.STONE, 0.4), 4, 0.6, 1.6)
            for y in range(rim_y + 2, rim_y + 12):
                t = (y - rim_y) / 22
                half = int(24 * (1 - t * 0.55))
                for x in range(cx - half, cx + half + 1):
                    band = math.sin(x * 0.7 + y)
                    i = 2 + (1 if band > 0.3 else 0)
                    c.set(x, y, water[max(0, min(3, i))])
            c.hline(cx - 22, cx + 22, rim_y + 3, lerp(core.FOG, core.STORM, 0.3))
            # overflow drip
            c.vline(cx + 24, rim_y + 6, rim_y + 14, water[2])
        else:
            # dry interior shadow
            for y in range(rim_y + 2, rim_y + 14):
                t = (y - rim_y) / 22
                half = int(24 * (1 - t * 0.55))
                c.hline(cx - half, cx + half, y, shade(core.TIMBER, 0.5))
        # rope lashings holding the funnel to legs
        _rope_lash(c, cx - 14, rim_y + 16, 10)
        _rope_lash(c, cx + 12, rim_y + 16, 10)
        frames.append(_finish(c, warm=True))
    return frames


# ===========================================================================
# windbreak 64x80
# ===========================================================================
def windbreak():
    W, Hh = 64, 80
    c = Canvas(W, Hh)
    rng = core.rng_for("windbreak")
    cx = 32
    base_y = 72
    _shadow(c, cx, base_y + 3, 26, 5)
    # angled fence of lashed planks
    posts = [cx - 22, cx - 7, cx + 8, cx + 23]
    for p in posts:
        c.rect(p - 2, 22, p + 1, base_y, DARKWOOD_R[1])
        c.set(p - 1, 21, DARKWOOD_R[3])
    # horizontal slats with gaps (wind passes)
    for y in range(28, base_y - 4, 7):
        _plank_wall(c, cx - 24, y, cx + 24, y + 4, TIMBER_R, rng, horizontal=True)
    # diagonal brace
    c.line(cx - 22, base_y, cx + 23, 26, RUST_R[1])
    c.line(cx - 21, base_y, cx + 24, 26, RUST_R[2])
    # rope lashings at post crossings
    for p in posts:
        _rope_lash(c, p - 1, 30, 6, vertical=False)
        _rope_lash(c, p - 1, 50, 6, vertical=False)
    # tattered cloth flag showing wind direction
    for i in range(10):
        x = cx + 18 + i
        y = 24 + int(math.sin(i * 0.6) * 2)
        c.set(x, y, shade(core.RUST, 1.1))
        c.set(x, y + 1, core.RUST)
    return [_finish(c, warm=True)]


# ===========================================================================
# path 64x32 flat overlay
# ===========================================================================
def path():
    c = Canvas(64, 32)
    rng = core.rng_for("path")
    base = material_ramp(core.STONE, 5, 0.55, 1.3)
    for y in range(32):
        d = abs(y - 15.5)
        frac = 1.0 - d / 15.5
        xext = int(round(28 * frac))
        for x in range(32 - xext, 32 + xext):
            # cobbles via cellular pattern
            cell = ((x + 1) // 4 + (y + 1) // 3 * 7)
            i = 2 + (cell % 3 - 1)
            col = base[max(0, min(4, i))]
            # mortar gaps
            if (x % 4 == 0) or (y % 3 == 0):
                col = base[0]
            c.set(x, y, col)
    # rim
    for y in range(32):
        d = abs(y - 15.5); frac = 1 - d / 15.5; xext = int(round(28 * frac))
        c.set(32 - xext, y, base[-1] if y <= 15 else base[0])
        c.set(31 + xext, y, base[3] if y <= 15 else base[0])
    return [_finish(c)]


# ===========================================================================
# scaffolding 64x80
# ===========================================================================
def scaffolding():
    W, Hh = 64, 80
    c = Canvas(W, Hh)
    rng = core.rng_for("scaffolding")
    cx = 32
    base_y = 72
    _shadow(c, cx, base_y + 3, 24, 5)
    # vertical poles
    poles = [cx - 20, cx - 6, cx + 8, cx + 22]
    for p in poles:
        c.rect(p - 1, 18, p + 1, base_y, TIMBER_R[1])
        c.set(p - 1, 17, TIMBER_R[3])
    # horizontal cross-beams at 3 levels
    for y in (28, 46, 64):
        c.rect(cx - 22, y, cx + 22, y + 2, TIMBER_R[2])
    # diagonal braces
    c.line(cx - 20, base_y, cx + 8, 28, DARKWOOD_R[1])
    c.line(cx + 22, base_y, cx - 6, 28, DARKWOOD_R[1])
    # planks on top platform
    _plank_wall(c, cx - 24, 14, cx + 24, 20, TIMBER_R, rng, horizontal=True)
    # rope lashings at every joint
    for p in poles:
        for y in (28, 46, 64):
            _rope_lash(c, p - 1, y - 1, 4, vertical=False)
    return [_finish(c, warm=True)]


# ===========================================================================
# skynet 96x96, 2 frames (empty / has-catch)
# ===========================================================================
def skynet():
    W, Hh = 96, 96
    frames = []
    for caught in (0, 1):
        c = Canvas(W, Hh)
        rng = core.rng_for(f"skynet_{caught}")
        cx = 48
        base_y = 84
        _shadow(c, cx, base_y + 3, 30, 6)
        # two angled timber arms reaching out over the cliff edge
        c.line(cx - 18, base_y, cx - 30, 24, DARKWOOD_R[1])
        c.line(cx - 17, base_y, cx - 29, 24, DARKWOOD_R[2])
        c.line(cx + 18, base_y, cx + 30, 24, DARKWOOD_R[1])
        c.line(cx + 17, base_y, cx + 29, 24, DARKWOOD_R[2])
        # cross bar at top
        c.rect(cx - 30, 22, cx + 30, 25, DARKWOOD_R[2])
        # base anchor
        c.rect(cx - 22, base_y - 4, cx + 22, base_y + 2, shade(core.STONE, 0.8))
        # the net: rope mesh slung between arms
        for i in range(-28, 29, 4):
            c.line(cx + i, 26, cx + i // 2, base_y - 6, ROPE_R[1])
        for j in range(28, 78, 6):
            half = int(28 * (1 - (j - 26) / 60))
            c.hline(cx - half, cx + half, j, ROPE_R[2])
        if caught:
            # captured salvage / fish-like sky-creature glints in the net
            for _ in range(5):
                x = rng.randint(cx - 16, cx + 16)
                y = rng.randint(46, 70)
                col = rng.choice([core.RUST, core.STONE, core.CROP_GOLD, core.AMBER])
                c.disc(x, y, rng.randint(2, 4), col)
                c.set(x - 1, y - 1, shade(col, 1.4))
            # net bulges (drawn as a darker sag)
            c.ellipse(cx, 62, 18, 12, (0, 0, 0, 0))
        # rope lashing at the apex
        _rope_lash(c, cx - 1, 24, 8)
        frames.append(_finish(c, warm=True))
    return frames


# ===========================================================================
# drying_rack 96x80, 2 frames (empty / drying)
# ===========================================================================
def drying_rack():
    W, Hh = 96, 80
    frames = []
    for drying in (0, 1):
        c = Canvas(W, Hh)
        rng = core.rng_for(f"drying_rack_{drying}")
        cx = 48
        base_y = 72
        _shadow(c, cx, base_y + 3, 34, 6)
        # A-frame posts
        for side in (-1, 1):
            c.line(cx + side * 34, base_y, cx + side * 8, 18, DARKWOOD_R[1])
            c.line(cx + side * 33, base_y, cx + side * 7, 18, DARKWOOD_R[2])
        # top ridge beam
        c.rect(cx - 10, 16, cx + 10, 19, DARKWOOD_R[2])
        # horizontal drying bars
        bars_y = [30, 42, 54]
        for by in bars_y:
            half = int(10 + (by - 18) / (base_y - 18) * 26)
            c.rect(cx - half, by, cx + half, by + 1, TIMBER_R[2])
        if drying:
            # bundles of herbs hanging
            for by in bars_y:
                half = int(10 + (by - 18) / (base_y - 18) * 26)
                for hx in range(cx - half + 4, cx + half - 2, 8):
                    blen = rng.randint(6, 10)
                    for t in range(blen):
                        col = lerp(core.LEAF_D, core.TIMBER, t / blen)
                        col = shade(col, 0.9)
                        c.set(hx, by + 1 + t, col)
                        c.set(hx + 1, by + 1 + t, shade(col, 0.85))
                    c.set(hx, by + 1, core.RUST)  # tie
        else:
            # a couple of empty hooks / rope ends
            for by in bars_y:
                c.vline(cx, by + 1, by + 4, ROPE_R[1])
                c.vline(cx + 14, by + 1, by + 3, ROPE_R[1])
        frames.append(_finish(c, warm=True))
    return frames


# ===========================================================================
# stone_mill 128x160, 4 frames (sails turning)
# ===========================================================================
def stone_mill():
    W, Hh = 128, 160
    frames = []
    for f in range(4):
        c = Canvas(W, Hh)
        rng = core.rng_for(f"stone_mill_{f}")
        cx = 64
        base_y = 150
        _shadow(c, cx, base_y + 4, 44, 8)
        # tapered stone tower
        tower_top = 52
        for y in range(tower_top, base_y):
            t = (y - tower_top) / (base_y - tower_top)
            half = int(20 + t * 18)
            for x in range(cx - half, cx + half + 1):
                # stone blocks
                bx = (x // 7); byb = (y // 6)
                i = 2 + ((bx + byb) % 3 - 1)
                col = STONE_R[max(0, min(4, i))]
                if x < cx:
                    col = shade(col, 1.1)
                if (y % 6 == 0) or ((x + (byb % 2) * 3) % 7 == 0):
                    col = STONE_R[0]
                c.set(x, y, col)
        # door + lit window
        c.rect(cx - 8, base_y - 28, cx + 8, base_y, shade(core.TIMBER, 0.5))
        _plank_wall(c, cx - 7, base_y - 27, cx + 7, base_y - 1, DARKWOOD_R, rng)
        _lit_window(c, cx, base_y - 50, 6, 7)
        _lit_window(c, cx - 22, base_y - 30, 4, 5)
        # conical timber roof cap
        for y in range(tower_top - 22, tower_top + 2):
            t = (y - (tower_top - 22)) / 24
            half = int(t * 22)
            for x in range(cx - half, cx + half + 1):
                col = THATCH_R[2 + (1 if x < cx else -1)]
                if dither_at(x, y, 0.25):
                    col = THATCH_R[1]
                c.set(x, y, col)
        c.set(cx, tower_top - 24, core.RUST)  # finial
        # sail hub
        hub_y = tower_top + 6
        c.disc(cx, hub_y, 4, IRON_R[2])
        c.disc(cx, hub_y, 2, IRON_R[4])
        # FOUR sails rotating: angle advances per frame
        base_ang = f / 4.0 * (math.pi / 2)  # 90 deg total over 4 frames
        for s in range(4):
            ang = base_ang + s * (math.pi / 2)
            length = 40
            ex = cx + math.cos(ang) * length
            ey = hub_y + math.sin(ang) * length
            # spar
            c.line(cx, hub_y, int(ex), int(ey), DARKWOOD_R[1])
            # sail cloth (offset perpendicular)
            perp = ang + math.pi / 2
            for t in range(6, length, 2):
                bx = cx + math.cos(ang) * t
                by = hub_y + math.sin(ang) * t
                for w in range(0, 8):
                    px = int(bx + math.cos(perp) * w)
                    py = int(by + math.sin(perp) * w)
                    col = lerp(core.FOG, core.TIMBER, 0.3)
                    col = shade(col, 1.0 - w * 0.04)
                    c.set(px, py, col)
                # sail frame edge
                c.set(int(bx + math.cos(perp) * 8), int(by + math.sin(perp) * 8),
                      DARKWOOD_R[1])
        frames.append(_finish(c, warm=True))
    return frames


# ===========================================================================
# forge 128x128, 4 frames (ember glow pulse)
# ===========================================================================
def forge():
    W, Hh = 128, 128
    frames = []
    glow_levels = [0.45, 0.75, 1.0, 0.65]  # ember pulse over 4 frames
    for f in range(4):
        c = Canvas(W, Hh)
        rng = core.rng_for(f"forge_{f}")
        cx = 64
        base_y = 116
        _shadow(c, cx, base_y + 4, 46, 8)
        # stone forge base / chimney stack
        stack_x = cx + 18
        for y in range(28, base_y):
            half = 16 if y > 60 else 10
            for x in range(stack_x - half, stack_x + half):
                bx = x // 6; byb = y // 6
                i = 2 + ((bx + byb) % 3 - 1)
                col = STONE_R[max(0, min(4, i))]
                if (y % 6 == 0):
                    col = STONE_R[0]
                c.set(x, y, col)
        # main forge body (stone + timber frame)
        _plank_wall(c, cx - 40, 70, cx - 2, base_y, DARKWOOD_R, rng)
        c.rect(cx - 42, 68, cx + 36, 72, DARKWOOD_R[1])  # lintel
        # the forge MOUTH: arched opening glowing orange from within
        g = glow_levels[f]
        ember = lerp(core.FORGE, core.AMBER, g * 0.4)
        core_c = lerp((255, 240, 180), ember, 1 - g)
        mcx, mcy = cx - 16, base_y - 26
        for y in range(mcy - 18, mcy + 18):
            for x in range(mcx - 18, mcx + 18):
                d = math.hypot(x - mcx, (y - mcy) * 1.1)
                if d <= 17:
                    # radial glow gradient
                    t = d / 17
                    col = lerp(core_c, lerp(ember, core.CHARCOAL, 0.5), t)
                    col = shade(col, 0.6 + g * 0.5)
                    c.set(x, y, col)
                    # bright coals at the centre
                    if d < 6 and rng.random() < 0.5 + g * 0.3:
                        c.set(x, y, lerp(ember, (255, 250, 200), g))
        # stone arch around the mouth
        for ang_deg in range(180, 361, 6):
            a = math.radians(ang_deg)
            x = mcx + int(math.cos(a) * 19)
            y = mcy + int(math.sin(a) * 19)
            c.set(x, y, STONE_R[1])
            c.set(x, y - 1, STONE_R[2])
        # anvil out front
        ay = base_y - 6
        c.rect(cx + 2, ay - 8, cx + 22, ay - 4, IRON_R[2])
        c.rect(cx + 6, ay - 4, cx + 12, ay, IRON_R[1])
        c.rect(cx + 18, ay - 6, cx + 24, ay - 4, IRON_R[3])  # horn
        c.set(cx + 4, ay - 8, IRON_R[4])
        # ember sparks rising from chimney (more when hotter)
        n_sparks = int(2 + g * 6)
        for _ in range(n_sparks):
            x = stack_x + rng.randint(-6, 6)
            y = 28 - rng.randint(0, 18)
            col = rng.choice([core.FORGE, core.AMBER, (255, 220, 150)])
            c.set(x, y, col)
        # warm light spill on the ground
        spill = lerp(core.FORGE, core.AMBER, 0.5)
        for x in range(mcx - 20, mcx + 22):
            for y in range(base_y - 3, base_y + 3):
                t = abs(x - mcx) / 22
                if rng.random() < (1 - t) * g * 0.4:
                    cur = c.get(x, y)
                    c.set(x, y, lerp((cur[0], cur[1], cur[2]), spill, 0.3 * g))
        frames.append(_finish(c, warm=True))
    return frames


# ===========================================================================
# crate 48x48, barrel 48x64
# ===========================================================================
def crate():
    W, Hh = 48, 48
    c = Canvas(W, Hh)
    rng = core.rng_for("crate")
    cx = 24
    base_y = 42
    _shadow(c, cx, base_y + 2, 18, 4)
    top = 14
    _plank_wall(c, cx - 16, top, cx + 16, base_y, TIMBER_R, rng)
    # frame edges (darker corner battens)
    c.rect(cx - 16, top, cx - 13, base_y, DARKWOOD_R[1])
    c.rect(cx + 13, top, cx + 16, base_y, DARKWOOD_R[1])
    c.rect(cx - 16, top, cx + 16, top + 3, DARKWOOD_R[1])
    c.rect(cx - 16, base_y - 3, cx + 16, base_y, DARKWOOD_R[1])
    # diagonal cross brace
    c.line(cx - 14, base_y - 4, cx + 14, top + 4, DARKWOOD_R[0])
    c.line(cx + 14, base_y - 4, cx - 14, top + 4, DARKWOOD_R[0])
    # iron corner brackets + nails
    for cxx in (cx - 14, cx + 14):
        for cyy in (top + 2, base_y - 2):
            c.set(cxx, cyy, IRON_R[4])
    return [_finish(c, warm=True)]


def barrel():
    W, Hh = 48, 64
    c = Canvas(W, Hh)
    rng = core.rng_for("barrel")
    cx = 24
    base_y = 58
    top = 8
    _shadow(c, cx, base_y + 3, 16, 4)
    # barrel bulge silhouette
    for y in range(top, base_y + 1):
        t = (y - top) / (base_y - top)
        half = int(12 + math.sin(t * math.pi) * 6)
        for x in range(cx - half, cx + half + 1):
            stave = ((x - (cx - half)) // 3) % 2
            grain = math.sin(y * 0.5 + x)
            i = 2 + stave + (1 if x < cx else -1) + (1 if grain > 0.7 else 0)
            col = TIMBER_R[max(0, min(4, i))]
            # stave gaps
            if (x - cx) % 4 == 0:
                col = shade(col, 0.8)
            c.set(x, y, col)
    # metal hoops
    for hy in (top + 4, (top + base_y) // 2, base_y - 5):
        t = (hy - top) / (base_y - top)
        half = int(12 + math.sin(t * math.pi) * 6)
        for x in range(cx - half, cx + half + 1):
            c.set(x, hy, RUST_R[1])
            c.set(x, hy + 1, RUST_R[2] if x < cx else RUST_R[0])
    # top lid
    c.ellipse(cx, top + 1, 11, 3, shade(core.TIMBER, 0.7))
    c.ellipse(cx, top, 11, 3, TIMBER_R[3])
    return [_finish(c, warm=True)]


# ===========================================================================
# debris piles 48x48 each
# ===========================================================================
def debris(n):
    W, Hh = 48, 48
    c = Canvas(W, Hh)
    rng = core.rng_for(f"debris_{n}")
    cx = 24
    base_y = 42
    _shadow(c, cx, base_y + 2, 18, 4)
    # a heaped pile of salvage: broken planks, rusted metal, rope coil, stones
    items = []
    palette = [TIMBER_R, RUST_R, STONE_R, DARKWOOD_R]
    for _ in range(8 + n * 2):
        x = cx + rng.randint(-15, 15)
        y = base_y - rng.randint(0, 18)
        kind = rng.randint(0, 3)
        items.append((y, x, kind))
    items.sort()  # back-to-front by y
    for y, x, kind in items:
        ramp = palette[kind]
        if kind == 0 or kind == 3:  # plank shard
            ln = rng.randint(6, 14)
            ang = rng.uniform(-0.6, 0.6)
            for t in range(ln):
                xx = x + int(math.cos(ang) * t)
                yy = y + int(math.sin(ang) * t)
                c.set(xx, yy, ramp[2])
                c.set(xx, yy - 1, ramp[3])
                c.set(xx, yy + 1, ramp[1])
            c.set(x, y, IRON_R[4])  # nail
        elif kind == 1:  # rusted metal sheet
            w = rng.randint(4, 8); h = rng.randint(3, 6)
            for yy in range(y - h, y):
                for xx in range(x - w, x + w):
                    col = ramp[2 + (1 if xx < x else -1)]
                    if dither_at(xx, yy, 0.3):
                        col = ramp[1]
                    c.set(xx, yy, col)
        else:  # stone chunk
            r = rng.randint(2, 5)
            c.disc(x, y, r, ramp[2])
            c.disc(x - 1, y - 1, r - 1, ramp[3])
            c.set(x - r + 1, y - r + 1, ramp[4])
    # a coil of rope on top
    c.disc(cx + rng.randint(-6, 6), base_y - rng.randint(8, 16), 3, ROPE_R[1])
    c.disc(cx + 2, base_y - 12, 2, ROPE_R[2])
    return [_finish(c, warm=True)]


# ===========================================================================
def generate():
    core.save_strip(shelter(), "structures/shelter.png", 128, 128)
    core.save_strip(rain_catcher(), "structures/rain_catcher.png", 64, 96)
    core.save_strip(windbreak(), "structures/windbreak.png", 64, 80)
    core.save_strip(path(), "structures/path.png", 64, 32)
    core.save_strip(scaffolding(), "structures/scaffolding.png", 64, 80)
    core.save_strip(skynet(), "structures/skynet.png", 96, 96)
    core.save_strip(drying_rack(), "structures/drying_rack.png", 96, 80)
    core.save_strip(stone_mill(), "structures/stone_mill.png", 128, 160)
    core.save_strip(forge(), "structures/forge.png", 128, 128)
    core.save_strip(crate(), "structures/crate.png", 48, 48)
    core.save_strip(barrel(), "structures/barrel.png", 48, 64)
    for i in (1, 2, 3):
        core.save_strip(debris(i), f"debris/debris_{i}.png", 48, 48)


if __name__ == "__main__":
    generate()
    print("structures done")
