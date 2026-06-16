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
# Weathered warm furnace stone — the cold base STONE pushed toward rust/timber so
# the forge reads as fire-baked brick (matches the painted reference), not grey concrete.
WARMSTONE = lerp(core.STONE, core.RUST, 0.32)
WARMSTONE_R = material_ramp(WARMSTONE, 5, 0.5, 1.6)
RUST_R = material_ramp(core.RUST, 5, 0.45, 1.4)
IRON_R = material_ramp(shade(core.STONE, 0.7), 5, 0.4, 1.6)
ROPE = lerp(core.TIMBER, core.FOG, 0.35)
ROPE_R = material_ramp(ROPE, 4, 0.6, 1.2)
THATCH_R = material_ramp(lerp(core.TIMBER, core.CROP_GOLD, 0.4), 5, 0.5, 1.3)


def _shadow(c, cx, by, rx, ry=4, opaque=False):
    """Ground-contact shadow. Opaque = hard dark pixels (structures over void/cliff)."""
    if opaque:
        dark = shade(core.CHARCOAL, 0.55)
        for y in range(by - ry, by + ry + 1):
            for x in range(cx - rx, cx + rx + 1):
                if rx > 0 and ry > 0 and ((x - cx) / rx) ** 2 + ((y - by) / ry) ** 2 <= 1.0:
                    c.set(x, y, dark)
    else:
        c.shadow_ellipse(cx, by, rx, ry, alpha=85)


# ---------------------------------------------------------------------------
# Reusable iso face-painters. Each returns (top_fn, left_fn, right_fn) suitable
# for core.iso_box / core.iso_frustum. Light convention = upper-LEFT: top
# brightest, left face mid-lit, right face shadowed. Signature is (x, y, v).
# ---------------------------------------------------------------------------
def iso_stone_faces(ramp):
    def top(x, y, v):
        col = ramp[3]
        return shade(col, 0.82) if (x % 9 == 0 or y % 5 == 0) else col

    def left(x, y, v):
        i = 2 + (1 if v > 0.6 else 0)
        col = shade(ramp[min(4, i)], 1.06)
        return shade(col, 0.72) if (y % 7 == 0 or x % 9 == 0) else col

    def right(x, y, v):
        i = 1 + (1 if v > 0.6 else 0)
        col = ramp[max(0, i)]
        return shade(col, 0.72) if (y % 7 == 0 or x % 9 == 0) else col
    return top, left, right


def iso_plank_faces(ramp):
    """Vertical-plank wood faces — seams run along x (plank edges)."""
    def top(x, y, v):
        col = ramp[3]
        return shade(col, 0.8) if x % 4 == 0 else col

    def left(x, y, v):
        i = 2 + ((x // 4) % 2)
        col = shade(ramp[min(4, i)], 1.05)
        return shade(col, 0.68) if x % 4 == 0 else col

    def right(x, y, v):
        i = 1 + ((x // 4) % 2)
        col = ramp[max(0, i)]
        return shade(col, 0.68) if x % 4 == 0 else col
    return top, left, right


def iso_metal_faces(ramp):
    def top(x, y, v):
        return ramp[4]

    def left(x, y, v):
        return ramp[3]

    def right(x, y, v):
        return ramp[2]
    return top, left, right


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
    """Iso salvage hut: timber-plank iso box body + a hipped rusted-metal roof
    with an eaves overhang, lit door + window on the camera faces, ember chimney."""
    W, Hh = 128, 128
    c = Canvas(W, Hh)
    rng = core.rng_for("shelter")
    cx = 64
    base_y = 108
    hw, hh = 27, 13
    body_h = 28
    cy_top = base_y - hh - body_h          # top diamond centre
    _shadow(c, cx, base_y - hh + 4, hw + 4, hh - 2)

    # --- timber box body ---
    t_top, t_left, t_right = iso_plank_faces(TIMBER_R)
    core.iso_box(c, cx, base_y, hw, hh, body_h, t_top, t_left, t_right)

    # door on the left-front face, lit window on the right-front face
    c.rect(cx - 18, base_y - 22, cx - 8, base_y - 3, shade(core.TIMBER, 0.42))
    _plank_wall(c, cx - 17, base_y - 21, cx - 9, base_y - 4, DARKWOOD_R, rng)
    c.set(cx - 10, base_y - 12, core.RUST)          # handle
    _lit_window(c, cx + 13, base_y - 16, 4, 5)

    # --- hipped rusted-metal roof on the top diamond, with overhang ---
    apex = (cx, cy_top - 22)
    el = (cx - hw - 4, cy_top)
    er = (cx + hw + 4, cy_top)
    ef = (cx, cy_top + hh + 3)
    eb = (cx, cy_top - hh - 1)

    def roof_face(lit):
        def f(x, y):
            corr = (x // 3) % 2                     # corrugation
            i = 2 + corr
            col = RUST_R[max(0, min(4, i))]
            col = shade(col, 1.12 if lit else 0.82)
            if dither_at(x, y, 0.18):
                col = shade(col, 0.85)
            return col
        return f
    core.fill_poly(c, [apex, eb, el, ef], roof_face(True))    # left roof slope (lit)
    core.fill_poly(c, [apex, ef, er, eb], roof_face(False))   # right roof slope (shadow)
    c.line(apex[0], apex[1], ef[0], ef[1], shade(core.RUST, 0.55))  # hip ridge to front

    # --- chimney with ember smoke on the back-right of the roof ---
    chx = cx + 12
    chy = cy_top - 10
    c.rect(chx - 2, chy - 12, chx + 2, chy, shade(core.STONE, 0.7))
    c.rect(chx - 2, chy - 12, chx + 2, chy - 10, core.CHARCOAL)
    c.set(chx, chy - 14, lerp(core.AMBER, core.FORGE, 0.5))
    c.set(chx + 1, chy - 16, core.FOG)
    return [_finish(c, warm=True)]


# ===========================================================================
# rain_catcher 64x96, 2 frames (empty/full)
# ===========================================================================
def rain_catcher():
    """Iso rain catcher: an open funnel basin (frustum, wide rim narrowing down) on
    four splayed timber legs. Top opening fills with water in the 'full' frame."""
    W, Hh = 64, 96
    frames = []
    cx = 32
    for full in (0, 1):
        c = Canvas(W, Hh)
        rng = core.rng_for(f"rain_catcher_{full}")
        base_y = 86
        _shadow(c, cx, base_y - 2, 22, 6)
        # four splayed legs from the ground up to the basin underside
        leg_top = 50
        for lx in (cx - 16, cx + 16):
            c.line(lx, base_y, cx + (4 if lx > cx else -4), leg_top, DARKWOOD_R[1])
            c.line(lx + 1, base_y, cx + (5 if lx > cx else -3), leg_top, DARKWOOD_R[2])
        c.line(cx - 14, base_y - 18, cx + 14, base_y - 18, DARKWOOD_R[1])   # cross-brace

        # basin = inverted frustum: narrow bottom widening up to a broad rim
        basin_base = leg_top + 8
        t_top, t_left, t_right = iso_plank_faces(TIMBER_R)
        core.iso_frustum(c, cx, basin_base, 7, 4, 22, 11, 16,
                         t_top, t_left, t_right, draw_top=False)
        rim_cy = (basin_base - 4) - 16          # top (rim) diamond centre
        # rim hoop
        for x in range(cx - 22, cx + 23):
            d = abs(x - cx) / 22
            c.set(x, rim_cy + int(11 * (1 - d * d)), shade(core.RUST, 0.8))
        # open interior: water (full) or dark dry bottom
        if full:
            water = material_ramp(lerp(core.STORM, core.STONE, 0.4), 4, 0.6, 1.6)
            core.iso_top(c, cx, basin_base - 16, 20, 10, 0,
                         lambda x, y, v, w=water: w[2] if (x + y) % 5 else w[3])
            c.ellipse(cx, rim_cy, 17, 8, lerp(core.FOG, core.STORM, 0.35))
        else:
            core.iso_top(c, cx, basin_base - 16, 19, 9, 0,
                         lambda x, y, v: shade(core.TIMBER, 0.45))
        frames.append(_finish(c, warm=True))
    return frames


# ===========================================================================
# windbreak 64x80
# ===========================================================================
def windbreak():
    """Iso fence: posts marching along an iso axis (front-left -> back-right) with
    horizontal slats given a 1px top edge for depth, and a tattered wind flag."""
    W, Hh = 64, 80
    c = Canvas(W, Hh)
    rng = core.rng_for("windbreak")
    cx = 32
    base_y = 70
    _shadow(c, cx, base_y - 2, 24, 6)
    p0 = (cx - 20, base_y)            # front-left
    p1 = (cx + 20, base_y - 20)       # back-right (iso line)
    n = 4
    posts = []
    for i in range(n):
        t = i / (n - 1)
        px = int(p0[0] + (p1[0] - p0[0]) * t)
        py = int(p0[1] + (p1[1] - p0[1]) * t)
        posts.append((px, py))
        c.rect(px - 1, py - 32, px + 1, py, DARKWOOD_R[1])
        c.set(px, py - 32, DARKWOOD_R[3])
    # horizontal slats connecting consecutive posts, 3 levels, with thickness
    for lvl in (8, 18, 28):
        for i in range(n - 1):
            a, b = posts[i], posts[i + 1]
            c.line(a[0], a[1] - lvl, b[0], b[1] - lvl, TIMBER_R[2])
            c.line(a[0], a[1] - lvl - 1, b[0], b[1] - lvl - 1, TIMBER_R[3])  # lit top edge
    # rope lashings at the joints
    for (px, py) in posts:
        _rope_lash(c, px - 1, py - 19, 5, vertical=False)
    # tattered cloth flag on the back post showing the wind
    fx, fy = posts[-1][0], posts[-1][1] - 30
    for i in range(9):
        c.set(fx + 2 + i, fy + int(math.sin(i * 0.6) * 2), shade(core.RUST, 1.1))
        c.set(fx + 2 + i, fy + 1 + int(math.sin(i * 0.6) * 2), core.RUST)
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
    """Iso scaffold: four corner poles, ringed cross-beams, a plank platform on top
    (iso diamond) and diagonal braces on the camera faces."""
    W, Hh = 64, 80
    c = Canvas(W, Hh)
    cx = 32
    base_y = 66
    hw, hh = 20, 10
    post_h = 40
    front = (cx, base_y)
    left = (cx - hw, base_y - hh)
    right = (cx + hw, base_y - hh)
    back = (cx, base_y - 2 * hh)
    _shadow(c, cx, base_y - hh + 3, hw, hh - 2)
    # corner poles
    for (px, py) in (front, left, right, back):
        c.rect(px - 1, py - post_h, px + 1, py, TIMBER_R[1])
        c.set(px, py - post_h, TIMBER_R[3])
    # cross-beam rings at two levels
    for lvl in (14, 30):
        f2 = (front[0], front[1] - lvl)
        l2 = (left[0], left[1] - lvl)
        r2 = (right[0], right[1] - lvl)
        b2 = (back[0], back[1] - lvl)
        for a, b in ((f2, r2), (r2, b2), (b2, l2), (l2, f2)):
            c.line(a[0], a[1], b[0], b[1], TIMBER_R[2])
    # plank platform (iso diamond at the pole tops)
    p_top, _, _ = iso_plank_faces(TIMBER_R)
    core.iso_top(c, cx, base_y - post_h, hw, hh, 0, p_top)
    # diagonal braces on the two camera faces
    c.line(left[0], left[1] - 2, front[0], front[1] - post_h + 6, DARKWOOD_R[1])
    c.line(right[0], right[1] - 2, front[0], front[1] - post_h + 6, DARKWOOD_R[1])
    return [_finish(c, warm=True)]


# ===========================================================================
# skynet 96x96, 2 frames (empty / has-catch)
# ===========================================================================
def skynet():
    """Iso debris catcher: an iso anchor block, two timber arms angling up and out
    over the cliff, and a rope net slung between them — salvage glints in it when caught."""
    W, Hh = 96, 96
    frames = []
    cx = 48
    for caught in (0, 1):
        c = Canvas(W, Hh)
        rng = core.rng_for(f"skynet_{caught}")
        base_y = 82
        _shadow(c, cx, base_y - 4, 26, 7)
        # iso anchor block at the back
        a_top, a_left, a_right = iso_plank_faces(DARKWOOD_R)
        core.iso_box(c, cx - 4, base_y - 2, 13, 6, 9, a_top, a_left, a_right)
        # two arms angling up and toward the camera
        foot_l, foot_r = (cx - 10, base_y - 8), (cx + 6, base_y - 8)
        top_l, top_r = (cx - 24, 18), (cx + 22, 26)
        for (fx, fy), (tx, ty) in ((foot_l, top_l), (foot_r, top_r)):
            c.line(fx, fy, tx, ty, DARKWOOD_R[1])
            c.line(fx + 1, fy, tx + 1, ty, DARKWOOD_R[2])
        c.line(top_l[0], top_l[1], top_r[0], top_r[1], DARKWOOD_R[2])   # cross bar
        # net: rope mesh from the cross bar down to a low front gather point
        gather = (cx + 1, base_y - 6)
        for i in range(0, 9):
            t = i / 8
            sx = int(top_l[0] + (top_r[0] - top_l[0]) * t)
            sy = int(top_l[1] + (top_r[1] - top_l[1]) * t)
            c.line(sx, sy, gather[0], gather[1], ROPE_R[1])
        for j in range(1, 5):
            t = j / 5
            mx0 = int(top_l[0] + (gather[0] - top_l[0]) * t)
            my0 = int(top_l[1] + (gather[1] - top_l[1]) * t)
            mx1 = int(top_r[0] + (gather[0] - top_r[0]) * t)
            my1 = int(top_r[1] + (gather[1] - top_r[1]) * t)
            c.line(mx0, my0, mx1, my1, ROPE_R[2])
        if caught:
            for _ in range(5):
                x = rng.randint(cx - 12, cx + 14)
                y = rng.randint(40, 64)
                col = rng.choice([core.RUST, core.STONE, core.CROP_GOLD, core.AMBER])
                c.disc(x, y, rng.randint(2, 3), col)
                c.set(x - 1, y - 1, shade(col, 1.4))
        _rope_lash(c, cx - 1, 20, 7)
        frames.append(_finish(c, warm=True))
    return frames


# ===========================================================================
# drying_rack 96x80, 2 frames (empty / drying)
# ===========================================================================
def drying_rack():
    """Iso drying rack: a 3D timber frame (four posts + top rim) with drying rails
    spanning front-to-back; herb bundles hang from the rails in the 'drying' frame."""
    W, Hh = 96, 80
    frames = []
    cx = 48
    hw, hh = 26, 13
    base_y = 64
    post_h = 38
    # footprint vertices: front, left, right, back
    front = (cx, base_y)
    left = (cx - hw, base_y - hh)
    right = (cx + hw, base_y - hh)
    back = (cx, base_y - 2 * hh)
    # corresponding top-rim vertices
    tf = (front[0], front[1] - post_h)
    tl = (left[0], left[1] - post_h)
    tr = (right[0], right[1] - post_h)
    tb = (back[0], back[1] - post_h)

    for drying in (0, 1):
        c = Canvas(W, Hh)
        rng = core.rng_for(f"drying_rack_{drying}")
        _shadow(c, cx, base_y - hh + 3, hw, hh - 2)
        # four corner posts
        for (px, py) in (front, left, right, back):
            c.rect(px - 1, py - post_h, px + 1, py, DARKWOOD_R[1])
            c.set(px, py - post_h, DARKWOOD_R[3])
        # top rim beams (the diamond at the top)
        for a, b in ((tf, tr), (tr, tb), (tb, tl), (tl, tf)):
            c.line(a[0], a[1], b[0], b[1], TIMBER_R[2])
        # drying rails spanning back->front at a few depths
        for t in (0.32, 0.5, 0.68):
            ax = int(tl[0] + (tf[0] - tl[0]) * t)
            ay = int(tl[1] + (tf[1] - tl[1]) * t)
            bx = int(tb[0] + (tr[0] - tb[0]) * t)
            by = int(tb[1] + (tr[1] - tb[1]) * t)
            c.line(ax, ay, bx, by, TIMBER_R[3])
            if drying:
                steps = 6
                for s in range(1, steps):
                    hx = int(ax + (bx - ax) * s / steps)
                    hy = int(ay + (by - ay) * s / steps)
                    blen = rng.randint(6, 11)
                    for k in range(blen):
                        col = shade(lerp(core.LEAF_D, core.TIMBER, k / blen), 0.9)
                        c.set(hx, hy + 1 + k, col)
                        c.set(hx + 1, hy + 1 + k, shade(col, 0.82))
                    c.set(hx, hy + 1, core.RUST)   # tie
            else:
                c.set((ax + bx) // 2, (ay + by) // 2 + 2, ROPE_R[1])  # bare rope end
        frames.append(_finish(c, warm=True))
    return frames


# ===========================================================================
# stone_mill 128x160, 4 frames (sails turning)
# ===========================================================================
def stone_mill():
    """Iso tapered stone tower + conical timber roof + rotating sails (4 frames)."""
    W, Hh = 128, 160
    frames = []
    cx = 64
    base_y = 140
    hw_b, hh_b = 28, 14
    hw_t, hh_t = 19, 9
    body_h = 70
    cyt = base_y - hh_b - body_h           # top diamond centre
    s_top, s_left, s_right = iso_stone_faces(STONE_R)

    for f in range(4):
        c = Canvas(W, Hh)
        rng = core.rng_for(f"stone_mill_{f}")
        _shadow(c, cx, base_y - hh_b + 4, hw_b + 4, hh_b - 2)

        # --- tapered stone tower ---
        core.iso_frustum(c, cx, base_y, hw_b, hh_b, hw_t, hh_t, body_h,
                         s_top, s_left, s_right)
        # door (front-left face) + a lit window (front-right face)
        c.rect(cx - 14, base_y - 24, cx - 4, base_y - 2, shade(core.TIMBER, 0.5))
        _plank_wall(c, cx - 13, base_y - 23, cx - 5, base_y - 3, DARKWOOD_R, rng)
        _lit_window(c, cx + 12, base_y - 26, 4, 5)

        # --- conical timber roof sitting on the tower top ---
        roof_base = cyt + hh_t + 1
        roof_h = 30
        apex = (cx, cyt - roof_h)
        left_b = (cx - hw_t - 3, cyt)
        right_b = (cx + hw_t + 3, cyt)
        front_b = (cx, roof_base + 2)

        def roof_fn(x, y, _ax=apex):
            t = (y - _ax[1]) / max(1, (front_b[1] - _ax[1]))   # 0 apex -> 1 eaves
            i = 1 + int(t * 2) + (1 if x < cx else -1)
            col = THATCH_R[max(0, min(4, i))]
            return shade(col, 0.82) if (y % 4 == 0) else col   # thatch banding
        core.fill_poly(c, [apex, left_b, front_b, right_b], roof_fn)
        c.set(cx, apex[1] - 1, core.RUST)                      # finial

        # --- sail hub + 4 rotating sails on the front of the tower ---
        hub_x, hub_y = cx, cyt + hh_t + 14
        c.disc(hub_x, hub_y, 3, IRON_R[2])
        c.disc(hub_x, hub_y, 1, IRON_R[4])
        base_ang = f / 4.0 * (math.pi / 2)
        for s in range(4):
            ang = base_ang + s * (math.pi / 2)
            length = 34
            perp = ang + math.pi / 2
            c.line(hub_x, hub_y, int(hub_x + math.cos(ang) * length),
                   int(hub_y + math.sin(ang) * length), DARKWOOD_R[1])
            for t in range(5, length, 2):
                bx = hub_x + math.cos(ang) * t
                by = hub_y + math.sin(ang) * t
                for w in range(0, 7):
                    col = shade(lerp(core.FOG, core.TIMBER, 0.3), 1.0 - w * 0.04)
                    c.set(int(bx + math.cos(perp) * w), int(by + math.sin(perp) * w), col)
                c.set(int(bx + math.cos(perp) * 7), int(by + math.sin(perp) * 7), DARKWOOD_R[1])
        frames.append(_finish(c, warm=True))
    return frames


# ===========================================================================
# forge 128x128, 4 frames (ember glow pulse)
# ===========================================================================
def forge():
    """Isometric stone furnace: a chunky dimetric box (top + two side faces),
    a chimney stack rising from the back, an arched mouth glowing on the
    camera-facing right face, and an anvil block out front. 4 ember frames."""
    W, Hh = 128, 128
    frames = []
    glow_levels = [0.45, 0.75, 1.0, 0.65]  # ember pulse over 4 frames
    cx = 64
    # Anchor footprint bottom vertex near the frame bottom (pivot = bottom-centre in Unity).
    base_y = 120
    hw_b, hh_b = 26, 13        # ~0.85 tile footprint (was 30/15 — read too large in-game)
    hw_t, hh_t = 18, 9         # tapered top
    body_h = 30
    cyt = base_y - hh_b - body_h   # top diamond centre

    def crack(x, y):
        return (int(x * 131 + y * 71) % 23) == 0

    def stone_top(x, y, v):
        col = WARMSTONE_R[3]
        if x % 9 == 0 or y % 5 == 0:
            col = shade(col, 0.82)
        return col

    def stone_left(x, y, v):    # lit face (light from upper-left)
        i = 2 + (1 if v > 0.6 else 0)
        col = shade(WARMSTONE_R[min(4, i)], 1.06)
        if y % 7 == 0 or x % 9 == 0:
            col = shade(col, 0.72)                        # mortar course
        elif crack(x, y):
            col = shade(col, 0.6)                         # crack
        return col

    def stone_right(x, y, v):   # shadowed face
        i = 1 + (1 if v > 0.6 else 0)
        col = WARMSTONE_R[max(0, i)]
        if y % 7 == 0 or x % 9 == 0:
            col = shade(col, 0.72)
        elif crack(x + 5, y):
            col = shade(col, 0.62)
        return col

    for f in range(4):
        c = Canvas(W, Hh)
        rng = core.rng_for(f"forge_{f}")
        g = glow_levels[f]

        # contact shadow hugging the footprint (opaque — semi-alpha reads as ghost over void)
        _shadow(c, cx, base_y - hh_b + 3, hw_b + 3, hh_b - 2, opaque=True)

        # --- main furnace mass (tapered frustum: beehive-ish body) ---
        core.iso_frustum(c, cx, base_y, hw_b, hh_b, hw_t, hh_t, body_h,
                         stone_top, stone_left, stone_right)

        # --- chimney: square stone stack rising from the (smaller) top CENTRE, flared hood ---
        ch_cx = cx + 2
        ch_hw, ch_hh, ch_h = 7, 3, 24
        ch_base = cyt + ch_hh                    # seat the stack on the tapered top-face centre
        core.iso_box(c, ch_cx, ch_base, ch_hw, ch_hh, ch_h,
                     lambda x, y, v: WARMSTONE_R[3],
                     lambda x, y, v: WARMSTONE_R[2] if y % 8 >= 1 else shade(WARMSTONE_R[2], 0.7),
                     lambda x, y, v: WARMSTONE_R[1] if y % 8 >= 1 else shade(WARMSTONE_R[1], 0.7))
        # flared hood cap (wider, slightly darker) — the reference chimney pot
        cap_base = ch_base - ch_h
        cap_h = 5
        core.iso_box(c, ch_cx, cap_base, ch_hw + 2, ch_hh + 1, cap_h,
                     shade(WARMSTONE_R[2], 0.92), shade(WARMSTONE_R[1], 0.95), shade(WARMSTONE_R[0], 1.05))
        # dark flue opening on top of the hood (smoke source)
        flue_cy = (cap_base - cap_h) - (ch_hh + 1)
        c.ellipse(ch_cx, flue_cy, ch_hw - 2, ch_hh - 1, core.CHARCOAL)
        c.ellipse(ch_cx, flue_cy, ch_hw - 3, ch_hh - 2,
                  lerp(core.CHARCOAL, core.FORGE, 0.12 + g * 0.18))  # faint inner ember glow

        # --- the forge ARCH: voussoir stone ring + glowing hearth, on the camera face ---
        ember = lerp(core.FORGE, core.AMBER, g * 0.4)
        core_c = lerp((255, 240, 180), ember, 1 - g)
        acx = cx + 11
        hearth_y = base_y - 6          # bottom of the opening
        ow, sh, rw = 8, 6, 3           # opening half-width, straight jamb height, ring thickness
        springline = hearth_y - sh     # where the semicircular top begins

        def _in_open(x, y):
            dx = x - acx
            if abs(dx) > ow or y > hearth_y:
                return False
            if y >= springline:
                return True
            return dx * dx + (y - springline) ** 2 <= ow * ow

        def _in_outer(x, y):
            dx = x - acx
            o = ow + rw
            if abs(dx) > o or y > hearth_y:
                return False
            if y >= springline:
                return True
            return dx * dx + (y - springline) ** 2 <= o * o

        # voussoir arch ring (banded wedge-stones following the arch)
        for y in range(springline - ow - rw, hearth_y + 1):
            for x in range(acx - ow - rw, acx + ow + rw + 1):
                if _in_outer(x, y) and not _in_open(x, y):
                    dy = y - springline
                    if dy < 0:
                        ang = math.atan2(-dy, x - acx)
                        seam = int(ang / math.pi * 7) % 2 == 0
                    else:
                        seam = (y // 3) % 2 == 0
                    base = WARMSTONE_R[3] if x <= acx else WARMSTONE_R[2]
                    c.set(x, y, shade(base, 0.78) if seam else base)
        # glowing hearth interior (bright coals at the bottom, dark up top)
        for y in range(springline - ow, hearth_y + 1):
            for x in range(acx - ow, acx + ow + 1):
                if _in_open(x, y):
                    t = (hearth_y - y) / max(1, (hearth_y - (springline - ow)))
                    col = lerp(core_c, lerp(ember, core.CHARCOAL, 0.6), t)
                    c.set(x, y, shade(col, 0.5 + g * 0.5))
        # bright coal bed at the bottom
        for x in range(acx - ow + 1, acx + ow):
            for y in range(hearth_y - 3, hearth_y + 1):
                if _in_open(x, y) and rng.random() < 0.6 + g * 0.3:
                    c.set(x, y, lerp(ember, (255, 250, 200), g))
        # stone hearth shelf / apron under the opening
        c.rect(acx - ow - 2, hearth_y + 1, acx + ow + 2, hearth_y + 2, WARMSTONE_R[1])
        c.hline(acx - ow - 2, acx + ow + 2, hearth_y + 1, WARMSTONE_R[3])  # lit front lip

        # --- anvil out front-left, as a little iso block ---
        ax, ay = cx - 15, base_y + 1
        core.iso_box(c, ax, ay, 7, 3, 6, IRON_R[3], IRON_R[2], IRON_R[1])
        c.rect(ax + 3, ay - 11, ax + 10, ay - 9, IRON_R[3])  # horn overhang

        # --- smoke/embers from the flue (no warm ground spill — semi-lerp looked ghostly) ---
        n_sparks = int(2 + g * 6)
        for _ in range(n_sparks):
            x = ch_cx + rng.randint(-3, 3)
            y = flue_cy - rng.randint(1, 16)
            col = rng.choice([core.FORGE, core.AMBER, (255, 220, 150)])
            c.set(x, y, col)
        frames.append(_finish(c, warm=True))
    return frames


# ===========================================================================
# crate 48x48, barrel 48x64
# ===========================================================================
def crate():
    """Iso timber crate: box with a plank lid top face, corner battens, cross-brace."""
    W, Hh = 48, 48
    c = Canvas(W, Hh)
    cx = 24
    base_y = 42
    hw, hh, h = 14, 7, 18
    _shadow(c, cx, base_y - hh + 3, hw + 2, hh - 1)
    t_top, t_left, t_right = iso_plank_faces(TIMBER_R)
    core.iso_box(c, cx, base_y, hw, hh, h, t_top, t_left, t_right)
    # corner battens down the 3 visible vertical edges
    for k in range(h + 1):
        c.set(cx, base_y - k, DARKWOOD_R[1])                       # front edge
        c.set(cx - hw, base_y - hh - k, DARKWOOD_R[1])            # left edge
        c.set(cx + hw, base_y - hh - k, DARKWOOD_R[1])            # right edge
    # diagonal cross-brace on each front face
    c.line(cx - hw + 1, base_y - hh, cx, base_y - h + 2, DARKWOOD_R[0])
    c.line(cx + hw - 1, base_y - hh, cx, base_y - h + 2, DARKWOOD_R[0])
    # iron nails at the top corners
    for nx in (cx - hw + 1, cx + hw - 1, cx):
        c.set(nx, base_y - hh - h + 2 if nx != cx else base_y - h + 2, IRON_R[4])
    return [_finish(c, warm=True)]


def barrel():
    """Iso barrel: a bulged cylinder with cross-barrel shading, metal hoops and an
    elliptical top lid so it reads as a round 3D volume on the tile."""
    W, Hh = 48, 64
    c = Canvas(W, Hh)
    cx = 24
    base_y = 58
    top = 10
    _shadow(c, cx, base_y - 1, 15, 4)

    def half_at(y):
        t = (y - top) / (base_y - top)
        return int(11 + math.sin(t * math.pi) * 5)

    # body: bulged staves, lit on the left, shadowed on the right (round shading)
    for y in range(top, base_y + 1):
        half = half_at(y)
        for x in range(cx - half, cx + half + 1):
            nx = (x - cx) / max(1, half)
            i = 2 + (1 if nx < -0.15 else 0) - (1 if nx > 0.35 else 0)
            col = TIMBER_R[max(0, min(4, i))]
            if (x - cx) % 4 == 0:
                col = shade(col, 0.78)                 # stave gap
            c.set(x, y, col)
    # metal hoops following the bulge
    for frac in (0.1, 0.5, 0.9):
        hy = int(top + (base_y - top) * frac)
        half = half_at(hy)
        for x in range(cx - half, cx + half + 1):
            nx = (x - cx) / max(1, half)
            c.set(x, hy, RUST_R[1] if nx > -0.15 else RUST_R[2])
            c.set(x, hy + 1, RUST_R[0] if nx > 0.2 else RUST_R[2])
    # elliptical top lid (the round opening read)
    c.ellipse(cx, top + 1, half_at(top) + 1, 4, shade(core.TIMBER, 0.6))
    c.ellipse(cx, top, half_at(top), 3, TIMBER_R[3])
    c.ellipse(cx, top, half_at(top) - 4, 2, TIMBER_R[2])
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
