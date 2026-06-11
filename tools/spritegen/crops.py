"""Crops: 64x64, 5 frames (stages 0-3 + dead).

Four crops with deliberately DISTINCT silhouettes:
  sky_moss   : low glowing teal-green tuft, hugs the ground
  cloud_root : pale bulbous root bulb with wispy pale top
  storm_wheat: tall golden wheat stalks that sway
  herb_plant : bushy rounded green herb clump

Stage progression: tiny sprout -> growing -> near-full -> mature (frame 3),
plus a grey-brown wilted dead frame (frame 4).

Crops are drawn anchored to the bottom-centre (pivot bottom-center per loader)
so they sit on a tile. We add a small soil mound + cast shadow at the base.
"""

from __future__ import annotations
import math
from PIL import Image
from . import core
from .core import Canvas, shade, lerp, material_ramp, dither_at

W, H = 64, 64
BASE_Y = 58  # soil line
CX = 32

SOIL = material_ramp(shade(core.TIMBER, 0.7), 4, 0.6, 1.2)


def _soil_mound(c, rng, width=12):
    c.shadow_ellipse(CX, BASE_Y + 2, width, 3, alpha=80)
    for y in range(BASE_Y - 1, BASE_Y + 4):
        ext = int(width * (1 - (y - (BASE_Y - 1)) / 6.0))
        for x in range(CX - ext, CX + ext + 1):
            i = 2 if (x < CX) else 1
            if dither_at(x, y, 0.3):
                i = 0
            c.set(x, y, SOIL[max(0, i)])


def _dead_recolor(c):
    """Recolor a built canvas to wilted grey-brown."""
    out = Canvas(W, H)
    for y in range(H):
        for x in range(W):
            p = c.px[y][x]
            if p[3] == 0:
                continue
            # luminance -> grey-brown ramp
            lum = (p[0] * 0.3 + p[1] * 0.5 + p[2] * 0.2) / 255.0
            gb = lerp(shade(core.TIMBER, 0.5), core.FOG, lum * 0.6)
            gb = shade(gb, 0.85)
            out.px[y][x] = (gb[0], gb[1], gb[2], p[3])
    return out


# ---------------------------------------------------------------------------
# sky_moss : low glowing teal-green tuft
# ---------------------------------------------------------------------------
def sky_moss():
    teal = lerp(core.LEAF_L, (40, 200, 170), 0.55)
    glow = lerp(teal, core.AMBER, 0.15)
    ramp = material_ramp(teal, 5, 0.5, 1.5)
    frames = []
    sizes = [3, 6, 10, 14]
    for stage in range(4):
        c = Canvas(W, H)
        rng = core.rng_for(f"sky_moss_{stage}")
        _soil_mound(c, rng, 9)
        s = sizes[stage]
        # cluster of rounded glowing lobes hugging the ground
        n_lobes = 3 + stage * 2
        for li in range(n_lobes):
            ang = li / n_lobes * math.pi - math.pi / 2 + rng.uniform(-0.2, 0.2)
            dist = rng.uniform(0.3, 1.0) * s
            lx = CX + int(math.cos(ang) * dist)
            ly = BASE_Y - 1 - int(abs(math.sin(ang)) * s * 0.7) - rng.randint(0, 2)
            r = max(2, s // 3 + rng.randint(-1, 1))
            for yy in range(ly - r, ly + r + 1):
                for xx in range(lx - r, lx + r + 1):
                    if (xx - lx) ** 2 + (yy - ly) ** 2 <= r * r:
                        nx = (xx - lx) / r
                        ny = (yy - ly) / r
                        idx = int(((-nx - ny) * 0.5 + 0.5) * 4)
                        c.set(xx, yy, ramp[max(0, min(4, idx))])
            # bright glow centre
            c.set(lx - 1, ly - 1, ramp[-1])
        # glowing spore specks (rim light)
        for _ in range(stage * 3):
            x = rng.randint(CX - s, CX + s)
            y = rng.randint(BASE_Y - s - 2, BASE_Y - 2)
            c.set(x, y, lerp(glow, (180, 255, 230), 0.5))
        c.auto_outline(core.OUTLINE)
        _teal_rim(c, (120, 255, 220))
        frames.append(c.to_image())
    frames.append(_dead_recolor(_build_for_dead(frames)).to_image())
    return frames


def _teal_rim(c, rimc):
    for y in range(c.h):
        for x in range(c.w):
            if c.px[y][x][3] > 200 and c.px[y][x][:3] != core.OUTLINE:
                if c.get(x, y - 1)[3] < 100:
                    cur = c.px[y][x]
                    c.setf(x, y, lerp(cur[:3], rimc, 0.4) + (255,))


def _build_for_dead(frames):
    """Reconstruct stage-3 canvas for the dead recolor (cheap: reload image)."""
    img = frames[3]
    c = Canvas(W, H)
    px = list(img.getdata())
    for i, p in enumerate(px):
        c.px[i // W][i % W] = p
    return c


# ---------------------------------------------------------------------------
# cloud_root : pale bulbous root + wispy pale top
# ---------------------------------------------------------------------------
def cloud_root():
    bulb = lerp(core.FOG, (220, 210, 200), 0.5)
    bramp = material_ramp(bulb, 5, 0.55, 1.35)
    wisp = lerp(core.FOG, (200, 230, 235), 0.6)
    frames = []
    bulb_sz = [2, 5, 9, 13]
    for stage in range(4):
        c = Canvas(W, H)
        rng = core.rng_for(f"cloud_root_{stage}")
        _soil_mound(c, rng, 10)
        bs = bulb_sz[stage]
        by = BASE_Y - bs
        # bulbous root: fat ellipse partly sunk in soil
        for yy in range(by - bs, BASE_Y + 1):
            for xx in range(CX - bs - 1, CX + bs + 2):
                ddx = (xx - CX) / (bs + 1)
                ddy = (yy - (by)) / (bs + 1.5)
                if ddx * ddx + ddy * ddy <= 1.0:
                    nx = (xx - CX) / (bs + 1)
                    ny = (yy - by) / (bs + 1)
                    idx = int(((-nx - ny * 0.5) * 0.5 + 0.5) * 4)
                    c.set(xx, yy, bramp[max(0, min(4, idx))])
        # root striations / pale veins
        for _ in range(bs):
            x = rng.randint(CX - bs, CX + bs)
            c.vline(x, by - 1, BASE_Y - 1, shade(bulb, 0.85))
        # bright top sheen
        c.disc(CX - bs // 3, by - bs // 2, max(1, bs // 4), bramp[-1])
        # wispy pale top tufts (grow with stage)
        n = 2 + stage * 2
        for i in range(n):
            wx = CX + rng.randint(-bs, bs)
            wh = bs + stage * 2 + rng.randint(0, 3)
            wy = by - bs
            for t in range(wh):
                yy = wy - t
                xx = wx + int(math.sin(t * 0.4 + i) * 2)
                a = 255 if t < wh - 3 else int(255 * (wh - t) / 3)
                col = lerp(wisp, (210, 240, 245), t / max(1, wh))
                c.set(xx, yy, (col[0], col[1], col[2], a))
        c.auto_outline(core.OUTLINE)
        _teal_rim(c, (235, 245, 250))
        frames.append(c.to_image())
    frames.append(_dead_recolor(_build_for_dead(frames)).to_image())
    return frames


# ---------------------------------------------------------------------------
# storm_wheat : tall golden wheat, sways
# ---------------------------------------------------------------------------
def storm_wheat():
    gold = core.CROP_GOLD
    gramp = material_ramp(gold, 5, 0.5, 1.4)
    stalk = material_ramp(lerp(core.LEAF_D, gold, 0.4), 4, 0.5, 1.3)
    frames = []
    heights = [8, 20, 32, 42]
    for stage in range(4):
        c = Canvas(W, H)
        rng = core.rng_for(f"storm_wheat_{stage}")
        _soil_mound(c, rng, 11)
        ht = heights[stage]
        n_stalks = 3 + stage
        sway = [0, 0.04, 0.09, 0.13][stage]
        for s in range(n_stalks):
            base_x = CX + (s - n_stalks // 2) * 4 + rng.randint(-1, 1)
            top_x = base_x + int(ht * sway) + rng.randint(-1, 1)
            # stalk
            for t in range(ht):
                yy = BASE_Y - 1 - t
                xx = int(base_x + (top_x - base_x) * t / ht)
                c.set(xx, yy, stalk[1 + (t % 2)])
                c.set(xx + 1, yy, stalk[1])
            # grain head (only stage>=1)
            if stage >= 1:
                hx, hy = top_x, BASE_Y - 1 - ht
                head_len = 4 + stage * 2
                for t in range(head_len):
                    yy = hy - t
                    # kernels left/right
                    for sidedir in (-1, 1):
                        kx = hx + sidedir * (1 + (t % 2))
                        idx = 3 if sidedir < 0 else 2
                        c.set(kx, yy, gramp[idx])
                    c.set(hx, yy, gramp[2])
                # awns at top
                c.set(hx, hy - head_len, gramp[-1])
                c.set(hx - 1, hy - head_len - 1, gramp[4])
                c.set(hx + 1, hy - head_len - 1, gramp[3])
        c.auto_outline(core.OUTLINE)
        _teal_rim(c, lerp(core.AMBER, (255, 240, 180), 0.5))
        frames.append(c.to_image())
    frames.append(_dead_recolor(_build_for_dead(frames)).to_image())
    return frames


# ---------------------------------------------------------------------------
# herb_plant : bushy rounded green herbs
# ---------------------------------------------------------------------------
def herb_plant():
    g = core.LEAF_D
    gramp = material_ramp(core.LEAF_L, 5, 0.5, 1.4)
    dark = material_ramp(g, 5, 0.45, 1.3)
    frames = []
    sizes = [4, 8, 13, 17]
    for stage in range(4):
        c = Canvas(W, H)
        rng = core.rng_for(f"herb_plant_{stage}")
        _soil_mound(c, rng, 11)
        s = sizes[stage]
        # bushy clump: many small pointed leaves radiating up
        n_leaves = 5 + stage * 4
        for i in range(n_leaves):
            ang = (i / n_leaves) * math.pi * 1.2 - math.pi * 0.1 - math.pi / 2
            ang += rng.uniform(-0.15, 0.15)
            llen = s + rng.randint(-2, 2)
            bx, by = CX + rng.randint(-2, 2), BASE_Y - 2
            for t in range(llen):
                xx = bx + int(math.cos(ang) * t)
                yy = by + int(math.sin(ang) * t)
                rampc = gramp if (i % 2 == 0) else dark
                idx = 3 - int(t / max(1, llen) * 2)
                c.set(xx, yy, rampc[max(0, idx)])
                if t > 1:
                    c.set(xx + 1, yy, rampc[max(0, idx - 1)])
            # leaf tip highlight
            tx = bx + int(math.cos(ang) * llen)
            ty = by + int(math.sin(ang) * llen)
            c.set(tx, ty, gramp[-1])
        # tiny flower buds / berries on mature
        if stage >= 2:
            for _ in range(stage * 2):
                x = rng.randint(CX - s, CX + s)
                y = rng.randint(BASE_Y - s - 2, BASE_Y - 4)
                c.set(x, y, lerp(core.MAGIC, core.AMBER, 0.4))
        c.auto_outline(core.OUTLINE)
        _teal_rim(c, (170, 210, 130))
        frames.append(c.to_image())
    frames.append(_dead_recolor(_build_for_dead(frames)).to_image())
    return frames


# ---------------------------------------------------------------------------
def generate():
    core.save_strip(sky_moss(), "crops/crop_sky_moss.png", W, H)
    core.save_strip(cloud_root(), "crops/crop_cloud_root.png", W, H)
    core.save_strip(storm_wheat(), "crops/crop_storm_wheat.png", W, H)
    core.save_strip(herb_plant(), "crops/crop_herb_plant.png", W, H)


if __name__ == "__main__":
    generate()
    print("crops done")
