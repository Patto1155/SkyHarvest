"""Item icons (32x32) + tool icons. Chunky readable silhouettes on transparency.

Item IDs (from CONVENTIONS):
 seeds: sky_moss_seed, cloud_root_seed, wheat_seed, herb_seed
 crops: sky_moss, cloud_root, wheat, herbs
 materials: scrap, wood, stone, iron_ore, coal, rope, nails, skynet_frame
 processed: flour, dried_herbs
Tools (ui/): hoe, wateringcan, sickle, hammer

Icon centres are kept readable; the UI draws card_frame.png OVER them, so we
keep important detail within the central ~24px and only soft edges outside.
"""

from __future__ import annotations
import math
from PIL import Image
from . import core
from .core import Canvas, shade, lerp, material_ramp, dither_at

S = 32
CX = CY = 16


def _icon(name, painter):
    c = Canvas(S, S)
    rng = core.rng_for("item_" + name)
    painter(c, rng)
    c.auto_outline(core.OUTLINE)
    # subtle warm rim top-left for readability
    for y in range(S):
        for x in range(S):
            if c.px[y][x][3] > 200 and c.px[y][x][:3] != core.OUTLINE:
                if c.get(x, y - 1)[3] < 100 and c.get(x - 1, y)[3] < 100:
                    cur = c.px[y][x]
                    c.setf(x, y, lerp(cur[:3], lerp(core.AMBER, core.FOG, 0.4), 0.4) + (255,))
    return c.to_image()


# ---- seed pouch (shared shape, tinted per seed) ---------------------------
def _seed_pouch(c, rng, tint):
    sack = material_ramp(shade(core.TIMBER, 0.85), 5, 0.5, 1.3)
    # burlap sack body
    for y in range(12, 27):
        t = (y - 12) / 15
        half = int(4 + math.sin(t * math.pi) * 6 + t * 2)
        for x in range(CX - half, CX + half + 1):
            i = 2 + (1 if x < CX else -1)
            col = sack[max(0, min(4, i))]
            if dither_at(x, y, 0.25):
                col = sack[1]
            c.set(x, y, col)
    # cinched neck + tie
    c.rect(CX - 4, 9, CX + 4, 12, sack[1])
    c.hline(CX - 5, CX + 5, 11, core.RUST)
    # gathered top folds
    for fx in (CX - 3, CX, CX + 3):
        c.vline(fx, 6, 9, sack[3])
    # spilled seeds at the mouth, tinted
    for _ in range(5):
        x = CX + rng.randint(-3, 3)
        y = rng.randint(6, 9)
        c.set(x, y, tint)
        c.set(x, y - 1, shade(tint, 1.3))
    # a couple seeds fallen at base
    c.set(CX - 7, 26, tint); c.set(CX + 7, 25, shade(tint, 1.2))


def icon_sky_moss_seed():
    return _icon("sky_moss_seed", lambda c, r: _seed_pouch(c, r, lerp(core.LEAF_L, (60, 210, 175), 0.5)))


def icon_cloud_root_seed():
    return _icon("cloud_root_seed", lambda c, r: _seed_pouch(c, r, lerp(core.FOG, (220, 220, 210), 0.5)))


def icon_wheat_seed():
    return _icon("wheat_seed", lambda c, r: _seed_pouch(c, r, core.CROP_GOLD))


def icon_herb_seed():
    return _icon("herb_seed", lambda c, r: _seed_pouch(c, r, core.LEAF_L))


# ---- crops (harvested goods) ----------------------------------------------
def icon_sky_moss():
    def paint(c, rng):
        teal = lerp(core.LEAF_L, (50, 205, 175), 0.55)
        ramp = material_ramp(teal, 5, 0.5, 1.5)
        # glowing moss clump
        for _ in range(7):
            x = CX + rng.randint(-7, 7)
            y = CY + rng.randint(-5, 7)
            r = rng.randint(2, 4)
            for yy in range(y - r, y + r + 1):
                for xx in range(x - r, x + r + 1):
                    if (xx - x) ** 2 + (yy - y) ** 2 <= r * r:
                        i = 2 + (1 if (xx < x and yy < y) else -1)
                        c.set(xx, yy, ramp[max(0, min(4, i))])
            c.set(x - 1, y - 1, ramp[-1])
        for _ in range(4):
            c.set(CX + rng.randint(-7, 7), CY + rng.randint(-6, 4), (160, 255, 220))
    return _icon("sky_moss", paint)


def icon_cloud_root():
    def paint(c, rng):
        bulb = lerp(core.FOG, (225, 215, 205), 0.5)
        ramp = material_ramp(bulb, 5, 0.55, 1.35)
        # fat tapering root
        for y in range(8, 26):
            t = (y - 8) / 18
            half = int(7 * math.sin((1 - t) * math.pi * 0.5) + 1)
            for x in range(CX - half, CX + half + 1):
                i = 2 + (1 if x < CX else -1)
                c.set(x, y, ramp[max(0, min(4, i))])
        # tapering root tail
        for t in range(6):
            c.set(CX + t - 2, 25 + t // 2, ramp[1])
        # veins
        for vx in (CX - 3, CX + 2):
            c.vline(vx, 10, 23, shade(bulb, 0.85))
        c.disc(CX - 2, 12, 2, ramp[-1])
        # wispy top
        for i in range(4):
            c.set(CX + rng.randint(-4, 4), 6 + i, lerp(bulb, (210, 240, 245), 0.6))
    return _icon("cloud_root", paint)


def icon_wheat():
    def paint(c, rng):
        g = material_ramp(core.CROP_GOLD, 5, 0.5, 1.4)
        # a tied bundle of wheat ears
        for s in range(3):
            bx = CX + (s - 1) * 4
            for t in range(20):
                yy = 26 - t
                xx = bx + (s - 1)
                c.set(xx, yy, lerp(core.LEAF_D, core.CROP_GOLD, 0.4))
            # ear
            hy = 6
            for t in range(8):
                for sd in (-1, 1):
                    c.set(bx + sd * (1 + t % 2), hy + t, g[3 if sd < 0 else 2])
                c.set(bx, hy + t, g[2])
            c.set(bx, hy - 1, g[-1])
        # twine tie
        c.hline(CX - 4, CX + 4, 22, core.RUST)
        c.hline(CX - 4, CX + 4, 23, shade(core.RUST, 0.8))
    return _icon("wheat", paint)


def icon_herbs():
    def paint(c, rng):
        g = material_ramp(core.LEAF_L, 5, 0.5, 1.4)
        d = material_ramp(core.LEAF_D, 5, 0.45, 1.3)
        # bushy bundle of leaves
        for i in range(11):
            ang = (i / 11) * math.pi - math.pi / 2 + rng.uniform(-0.1, 0.1)
            ln = rng.randint(8, 12)
            for t in range(ln):
                xx = CX + int(math.cos(ang) * t)
                yy = 24 + int(math.sin(ang) * t * 1.2) - t // 2
                ramp = g if i % 2 else d
                c.set(xx, yy, ramp[max(0, 3 - t // 4)])
                c.set(xx + 1, yy, ramp[max(0, 2 - t // 4)])
        c.hline(CX - 3, CX + 3, 24, core.RUST)  # tie
    return _icon("herbs", paint)


# ---- materials ------------------------------------------------------------
def icon_scrap():
    def paint(c, rng):
        ramp = material_ramp(core.STONE, 5, 0.4, 1.5)
        rust = material_ramp(core.RUST, 4, 0.5, 1.3)
        # twisted metal scrap pieces
        for _ in range(4):
            x = CX + rng.randint(-7, 7); y = CY + rng.randint(-7, 7)
            w = rng.randint(3, 6); h = rng.randint(2, 5)
            r = rng.random() < 0.4
            pr = rust if r else ramp
            for yy in range(y - h, y + h):
                for xx in range(x - w, x + w):
                    col = pr[2 + (1 if xx < x else -1)]
                    if dither_at(xx, yy, 0.3):
                        col = pr[1]
                    c.set(xx, yy, col)
            c.set(x - w, y - h, pr[-1])
        # bolt holes
        c.set(CX - 3, CY, core.OUTLINE); c.set(CX + 4, CY + 3, core.OUTLINE)
    return _icon("scrap", paint)


def icon_wood():
    def paint(c, rng):
        ramp = material_ramp(core.TIMBER, 5, 0.45, 1.45)
        # stacked logs (end-on circles + side)
        for i, (x, y) in enumerate([(11, 18), (21, 18), (16, 11)]):
            c.disc(x, y, 6, ramp[1])
            c.disc(x, y, 5, ramp[2])
            # rings
            c.disc(x, y, 3, ramp[1])
            c.disc(x, y, 1, ramp[3])
            c.set(x - 2, y - 2, ramp[4])
        # bark texture hint
        for _ in range(6):
            c.set(CX + rng.randint(-8, 8), CY + rng.randint(-7, 7), ramp[0])
    return _icon("wood", paint)


def icon_stone():
    def paint(c, rng):
        ramp = material_ramp(core.STONE, 5, 0.4, 1.55)
        for x, y, r in [(13, 19, 6), (21, 17, 5), (17, 12, 4)]:
            c.disc(x, y, r, ramp[1])
            c.disc(x - 1, y - 1, r - 1, ramp[2])
            c.set(x - r + 2, y - r + 2, ramp[4])
            # facets
            c.line(x - r + 1, y, x + r - 1, y - 1, ramp[0])
        for _ in range(5):
            c.set(CX + rng.randint(-7, 7), CY + rng.randint(-6, 7), ramp[3])
    return _icon("stone", paint)


def icon_iron_ore():
    def paint(c, rng):
        rock = material_ramp(shade(core.STONE, 0.85), 5, 0.4, 1.4)
        metal = material_ramp(lerp(core.RUST, core.STONE, 0.4), 4, 0.5, 1.7)
        c.disc(CX, CY + 2, 9, rock[1])
        c.disc(CX - 1, CY + 1, 8, rock[2])
        c.set(CX - 6, CY - 4, rock[4])
        # metallic ore veins glinting
        for _ in range(7):
            x = CX + rng.randint(-6, 6); y = CY + rng.randint(-5, 6)
            c.set(x, y, metal[2])
            c.set(x, y - 1, metal[-1])
            c.set(x + 1, y, metal[1])
    return _icon("iron_ore", paint)


def icon_coal():
    def paint(c, rng):
        ramp = material_ramp(shade(core.CHARCOAL, 1.3), 4, 0.4, 1.8)
        for x, y, r in [(13, 19, 6), (21, 18, 5), (17, 12, 5)]:
            c.disc(x, y, r, ramp[1])
            c.disc(x - 1, y - 1, r - 1, ramp[2])
            # glossy facet highlights (coal is shiny)
            c.set(x - r + 2, y - r + 2, ramp[-1])
            c.set(x - r + 3, y - r + 1, lerp(ramp[3], core.STORM, 0.4))
        # a faint ember hint
        c.set(CX, CY + 6, lerp(core.FORGE, ramp[1], 0.5))
    return _icon("coal", paint)


def icon_rope():
    def paint(c, rng):
        ramp = material_ramp(lerp(core.TIMBER, core.FOG, 0.3), 4, 0.55, 1.25)
        # coiled rope: concentric twisted rings
        for ring, r in enumerate((9, 6, 3)):
            for a in range(0, 360, 8):
                rad = math.radians(a)
                x = CX + int(math.cos(rad) * r)
                y = CY + 2 + int(math.sin(rad) * r * 0.6)
                twist = (a // 16) % 2
                c.set(x, y, ramp[1 + twist])
                c.set(x, y + 1, ramp[2 if twist else 0])
        # loose end
        c.line(CX + 9, CY + 4, CX + 12, CY + 9, ramp[1])
        c.set(CX + 12, CY + 9, ramp[3])
    return _icon("rope", paint)


def icon_nails():
    def paint(c, rng):
        ramp = material_ramp(shade(core.STONE, 0.8), 5, 0.4, 1.7)
        # a scatter of iron nails
        for i in range(5):
            x = CX + rng.randint(-7, 6)
            y = CY + rng.randint(-7, 7)
            ang = rng.uniform(0, math.pi)
            ln = rng.randint(7, 11)
            ex = x + int(math.cos(ang) * ln)
            ey = y + int(math.sin(ang) * ln)
            c.line(x, y, ex, ey, ramp[2])
            # head
            c.disc(x, y, 2, ramp[1])
            c.set(x - 1, y - 1, ramp[-1])
            # tip
            c.set(ex, ey, ramp[0])
    return _icon("nails", paint)


def icon_skynet_frame():
    def paint(c, rng):
        wood = material_ramp(shade(core.TIMBER, 0.9), 4, 0.5, 1.3)
        rope = material_ramp(lerp(core.TIMBER, core.FOG, 0.3), 3, 0.6, 1.2)
        # a hexagonal lashed frame (net hoop)
        pts = []
        for i in range(6):
            a = math.radians(i * 60 - 90)
            pts.append((CX + int(math.cos(a) * 11), CY + int(math.sin(a) * 11)))
        for i in range(6):
            x0, y0 = pts[i]
            x1, y1 = pts[(i + 1) % 6]
            c.line(x0, y0, x1, y1, wood[2])
            c.line(x0, y0 - 1, x1, y1 - 1, wood[3])
        # rope mesh inside
        for i in range(0, 6, 2):
            x0, y0 = pts[i]
            x1, y1 = pts[(i + 3) % 6]
            c.line(x0, y0, x1, y1, rope[1])
        # lashing knots at corners
        for x, y in pts:
            c.set(x, y, core.RUST)
    return _icon("skynet_frame", paint)


# ---- processed ------------------------------------------------------------
def icon_flour():
    def paint(c, rng):
        sack = material_ramp(lerp(core.FOG, (230, 225, 215), 0.4), 5, 0.55, 1.25)
        # plump flour sack
        for y in range(10, 28):
            t = (y - 10) / 18
            half = int(5 + math.sin(t * math.pi) * 7 + t * 1)
            for x in range(CX - half, CX + half + 1):
                i = 2 + (1 if x < CX else -1)
                c.set(x, y, sack[max(0, min(4, i))])
        # tied neck
        c.rect(CX - 3, 7, CX + 3, 10, sack[1])
        c.hline(CX - 4, CX + 4, 9, core.RUST)
        # flour spill + label stamp
        c.disc(CX, 29, 5, lerp((250, 248, 240), sack[3], 0.3))
        c.rect(CX - 4, 16, CX + 4, 21, lerp(sack[0], core.TIMBER, 0.5))  # label
        c.hline(CX - 3, CX + 3, 18, sack[3])
    return _icon("flour", paint)


def icon_dried_herbs():
    def paint(c, rng):
        ramp = material_ramp(lerp(core.LEAF_D, core.TIMBER, 0.45), 5, 0.45, 1.3)
        # a tied bundle of dried, browning herbs hanging
        for i in range(9):
            bx = CX + (i - 4) * 2
            ln = rng.randint(10, 15)
            for t in range(ln):
                yy = 11 + t
                xx = bx + int(math.sin(t * 0.3 + i) * 1)
                col = ramp[max(0, 3 - t // 5)]
                c.set(xx, yy, col)
        # twine binding at top
        c.rect(CX - 6, 8, CX + 6, 11, core.RUST)
        c.hline(CX - 6, CX + 6, 9, shade(core.RUST, 1.2))
        # hanging loop
        c.line(CX, 8, CX, 4, lerp(core.TIMBER, core.FOG, 0.3))
        c.disc(CX, 4, 2, (0, 0, 0, 0))
        c.ellipse(CX, 4, 2, 2, lerp(core.TIMBER, core.FOG, 0.3))
        c.set(CX, 4, (0, 0, 0, 0))
    return _icon("dried_herbs", paint)


# ---- tools ----------------------------------------------------------------
def _tool_handle(c, x0, y0, x1, y1):
    wood = material_ramp(core.TIMBER, 4, 0.5, 1.3)
    c.line(x0, y0, x1, y1, wood[1])
    # thickness
    dx = (x1 - x0); dy = (y1 - y0)
    ln = max(1, int(math.hypot(dx, dy)))
    for t in range(ln + 1):
        x = x0 + dx * t // ln
        y = y0 + dy * t // ln
        c.set(x, y, wood[2])
        c.set(x + 1, y, wood[1])
        c.set(x - 1, y, wood[3])


def icon_tool_hoe():
    def paint(c, rng):
        iron = material_ramp(shade(core.STONE, 0.75), 5, 0.4, 1.7)
        _tool_handle(c, 24, 6, 9, 24)
        # hoe blade at bottom-left
        c.rect(6, 23, 14, 27, iron[2])
        c.rect(6, 26, 14, 28, iron[1])
        c.set(7, 24, iron[-1])
        # ferrule
        c.rect(9, 22, 12, 25, core.RUST)
    return _icon("tool_hoe", paint)


def icon_tool_wateringcan():
    def paint(c, rng):
        metal = material_ramp(lerp(core.STONE, core.RUST, 0.35), 5, 0.4, 1.55)
        # can body
        for y in range(13, 26):
            for x in range(11, 23):
                i = 2 + (1 if x < 16 else -1)
                col = metal[max(0, min(4, i))]
                if dither_at(x, y, 0.15):
                    col = metal[1]
                c.set(x, y, col)
        c.set(11, 13, metal[-1])
        # spout
        c.line(22, 16, 28, 11, metal[2])
        c.line(22, 17, 28, 12, metal[1])
        # rose (sprinkler head)
        c.disc(28, 10, 3, metal[3])
        for _ in range(4):
            c.set(28 + rng.randint(-2, 2), 9 + rng.randint(-1, 2), metal[0])
        # handle arc
        c.line(13, 13, 13, 8, metal[2])
        c.line(13, 8, 20, 8, metal[2])
        c.line(20, 8, 21, 13, metal[2])
        # rivets
        c.set(12, 14, core.RUST); c.set(21, 14, core.RUST)
    return _icon("tool_wateringcan", paint)


def icon_tool_sickle():
    def paint(c, rng):
        iron = material_ramp(shade(core.STONE, 0.8), 5, 0.4, 1.8)
        _tool_handle(c, 9, 27, 18, 18)
        # curved blade
        for a in range(200, 350, 5):
            rad = math.radians(a)
            x = 17 + int(math.cos(rad) * 9)
            y = 12 + int(math.sin(rad) * 9)
            c.set(x, y, iron[2])
            x2 = 17 + int(math.cos(rad) * 11)
            y2 = 12 + int(math.sin(rad) * 11)
            c.set(x2, y2, iron[1])
            # inner edge highlight
            x3 = 17 + int(math.cos(rad) * 7)
            y3 = 12 + int(math.sin(rad) * 7)
            c.set(x3, y3, iron[-1])
        c.rect(16, 17, 19, 20, core.RUST)  # ferrule
    return _icon("tool_sickle", paint)


def icon_tool_hammer():
    def paint(c, rng):
        iron = material_ramp(shade(core.STONE, 0.7), 5, 0.4, 1.7)
        _tool_handle(c, 14, 28, 19, 9)
        # hammer head
        c.rect(11, 6, 25, 12, iron[2])
        c.rect(11, 6, 25, 8, iron[3])
        c.rect(11, 11, 25, 12, iron[1])
        # claw / peen ends
        c.rect(9, 7, 11, 11, iron[1])
        c.rect(25, 7, 27, 11, iron[2])
        c.set(12, 7, iron[-1])
        # band where head meets handle
        c.rect(17, 9, 21, 13, core.RUST)
    return _icon("tool_hammer", paint)


# ===========================================================================
ITEM_ICONS = {
    "sky_moss_seed": icon_sky_moss_seed, "cloud_root_seed": icon_cloud_root_seed,
    "wheat_seed": icon_wheat_seed, "herb_seed": icon_herb_seed,
    "sky_moss": icon_sky_moss, "cloud_root": icon_cloud_root,
    "wheat": icon_wheat, "herbs": icon_herbs,
    "scrap": icon_scrap, "wood": icon_wood, "stone": icon_stone,
    "iron_ore": icon_iron_ore, "coal": icon_coal, "rope": icon_rope,
    "nails": icon_nails, "skynet_frame": icon_skynet_frame,
    "flour": icon_flour, "dried_herbs": icon_dried_herbs,
}
TOOL_ICONS = {
    "hoe": icon_tool_hoe, "wateringcan": icon_tool_wateringcan,
    "sickle": icon_tool_sickle, "hammer": icon_tool_hammer,
}


def generate():
    for item_id, fn in ITEM_ICONS.items():
        core.save_single(fn(), f"items/icon_{item_id}.png")
    for tool_id, fn in TOOL_ICONS.items():
        core.save_single(fn(), f"ui/icon_tool_{tool_id}.png")


if __name__ == "__main__":
    generate()
    print("items done")
