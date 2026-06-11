"""UI: panels, buttons, slots, card frame, logo.

Dark leather/parchment with riveted metal edges, 9-sliceable (uniform borders
per CONVENTIONS: panel 16px, button 8px, slot/card 6px).

9-slice rule: the corner regions must be identical across the four corners
(after mirroring) and the edges must be uniform so Unity's sliced Image tiles
the centre cleanly. We draw a symmetric metal frame + flat-ish centre.
"""

from __future__ import annotations
import math
from PIL import Image
from . import core
from .core import Canvas, shade, lerp, material_ramp, dither_at

LEATHER = material_ramp(shade(core.TIMBER, 0.55), 5, 0.5, 1.3)
PARCH = material_ramp(lerp(core.TIMBER, core.FOG, 0.45), 5, 0.6, 1.2)
METAL = material_ramp(shade(core.STONE, 0.85), 5, 0.4, 1.6)
RUSTM = material_ramp(core.RUST, 5, 0.45, 1.35)


def _riveted_frame(c, x0, y0, x1, y1, border, metal=METAL, rivets=True):
    """Draw a uniform metal frame `border` thick around the rect edges."""
    # outer dark line
    c.rect_outline(x0, y0, x1, y1, core.OUTLINE)
    for b in range(border):
        i_top = 3 - b * 3 // max(1, border)
        # top + bottom bands
        c.hline(x0 + b, x1 - b, y0 + b, metal[max(0, min(4, 3 - b))])
        c.hline(x0 + b, x1 - b, y1 - b, metal[max(0, min(4, 1))])
        c.vline(x0 + b, y0 + b, y1 - b, metal[max(0, min(4, 3 - b))])
        c.vline(x1 - b, y0 + b, y1 - b, metal[max(0, min(4, 1))])
    # corner plates
    cp = border
    for (cxa, cya, sx, sy) in [(x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)]:
        for dy in range(cp):
            for dx in range(cp):
                xx = cxa + sx * dx
                yy = cya + sy * dy
                i = 2 + (1 if (dx + dy) < cp else -1)
                c.set(xx, yy, RUSTM[max(0, min(4, i))])
        # rivet
        if rivets:
            rx = cxa + sx * (cp // 2)
            ry = cya + sy * (cp // 2)
            c.set(rx, ry, METAL[-1])
            c.set(rx + sx, ry + sy, METAL[0])


def panel():
    """48x48, 16px border parchment/leather panel."""
    W = Hh = 48
    b = 16
    c = Canvas(W, Hh)
    rng = core.rng_for("ui_panel")
    # leather centre fill (entire panel, frame drawn over edges)
    for y in range(Hh):
        for x in range(W):
            i = 2
            if dither_at(x, y, 0.12):
                i = 1
            col = LEATHER[i]
            # subtle stitched centre, kept uniform-ish
            c.set(x, y, col)
    # worn scratches in the centre (within the 9-slice centre so it tiles ok-ish;
    # keep light)
    for _ in range(6):
        x = rng.randint(b, W - b); y = rng.randint(b, Hh - b)
        c.set(x, y, LEATHER[0])
    _riveted_frame(c, 0, 0, W - 1, Hh - 1, b)
    # inner stitch line just inside the frame
    c.rect_outline(b - 2, b - 2, W - b + 1, Hh - b + 1, shade(core.TIMBER, 0.4))
    return c.to_image()


def button(pressed=False):
    """48x24, 8px border metal-framed."""
    W, Hh = 48, 24
    b = 8
    c = Canvas(W, Hh)
    # face
    face = material_ramp(shade(core.TIMBER, 0.7), 5, 0.5, 1.3)
    for y in range(Hh):
        for x in range(W):
            # pressed = darker + shifted highlight
            i = 2 + (1 if y < Hh // 2 else -1)
            if pressed:
                i = 2 + (-1 if y < Hh // 2 else 0)
            col = face[max(0, min(4, i))]
            if dither_at(x, y, 0.1):
                col = face[1]
            c.set(x, y, col)
    _riveted_frame(c, 0, 0, W - 1, Hh - 1, b, metal=RUSTM if not pressed else METAL)
    if pressed:
        # inner shadow to read as depressed
        c.hline(b, W - b, b, core.OUTLINE)
        c.vline(b, b, Hh - b, shade(core.OUTLINE, 1.5))
    else:
        # top bevel highlight
        c.hline(b, W - b, b, METAL[-1])
    return c.to_image()


def slot():
    """40x40, 6px border metallic inventory slot."""
    W = Hh = 40
    b = 6
    c = Canvas(W, Hh)
    # dark recessed interior
    inner = material_ramp(shade(core.CHARCOAL, 1.4), 4, 0.5, 1.4)
    for y in range(Hh):
        for x in range(W):
            d = min(x, y, W - 1 - x, Hh - 1 - y)
            i = 1 if d > b else 2
            c.set(x, y, inner[i])
    # inner recessed shadow (top-left dark, bottom-right light = concave)
    c.hline(b, W - b, b, core.OUTLINE)
    c.vline(b, b, Hh - b, core.OUTLINE)
    c.hline(b, W - b, Hh - b - 1, inner[3])
    c.vline(W - b - 1, b, Hh - b, inner[3])
    _riveted_frame(c, 0, 0, W - 1, Hh - 1, b)
    return c.to_image()


def card_frame():
    """40x40, 6px painterly metallic card frame drawn OVER icons.

    Centre MUST be transparent so the underlying icon shows. Only the border
    ring is opaque (riveted ornate metal like the concept UI cards).
    """
    W = Hh = 40
    b = 6
    c = Canvas(W, Hh)
    # ornate frame: gradient metal with rust patina + corner flourishes
    for y in range(Hh):
        for x in range(W):
            d = min(x, y, W - 1 - x, Hh - 1 - y)
            if d >= b:
                continue  # leave centre transparent
            # bevelled metal: outer rust, inner steel
            t = d / b
            base = lerp(RUSTM[1], METAL[3], t)
            # top-left lit
            if x < W // 2 and y < Hh // 2:
                base = shade(base, 1.15)
            elif x > W // 2 and y > Hh // 2:
                base = shade(base, 0.85)
            c.set(x, y, base)
    # outer + inner dark outlines
    c.rect_outline(0, 0, W - 1, Hh - 1, core.OUTLINE)
    c.rect_outline(b - 1, b - 1, W - b, Hh - b, shade(core.STONE, 0.45))
    c.rect_outline(b - 2, b - 2, W - b + 1, Hh - b + 1, METAL[-1])
    # corner flourishes / rivets
    for (cxa, cya, sx, sy) in [(2, 2, 1, 1), (W - 3, 2, -1, 1), (2, Hh - 3, 1, -1), (W - 3, Hh - 3, -1, -1)]:
        c.disc(cxa, cya, 1, METAL[-1])
        c.set(cxa, cya, RUSTM[0])
        # little diagonal flourish into the corner
        c.set(cxa + sx, cya + sy, RUSTM[3])
    return c.to_image()


def logo():
    """256x96 'SKY HARVEST' title with magic purple accent."""
    W, Hh = 256, 96
    c = Canvas(W, Hh)
    rng = core.rng_for("ui_logo")
    # No bg (transparent) so it can overlay menu.
    # Pixel block font: draw each letter from a 5x7 cell scaled.
    title1 = "SKY"
    title2 = "HARVEST"
    metal_face = material_ramp(lerp(core.STONE, core.CROP_GOLD, 0.3), 5, 0.4, 1.7)

    font = _font5x7()

    def draw_word(word, ox, oy, scale, ramp):
        cw = (5 * scale + scale)  # letter + gap
        for li, ch in enumerate(word):
            glyph = font.get(ch)
            if not glyph:
                continue
            for ry in range(7):
                for rx in range(5):
                    if glyph[ry][rx] == "1":
                        for sy in range(scale):
                            for sx in range(scale):
                                px = ox + li * cw + rx * scale + sx
                                py = oy + ry * scale + sy
                                # vertical metal gradient + top bevel
                                grad = ry / 7
                                col = ramp[max(0, min(4, 1 + int((1 - grad) * 3)))]
                                c.set(px, py, col)
        return ox + len(word) * cw

    # SKY large, gold metal
    draw_word(title1, 70, 8, 6, metal_face)
    # HARVEST smaller below, steel
    steel = material_ramp(core.STONE, 5, 0.4, 1.7)
    draw_word(title2, 28, 54, 5, steel)
    # outline pass
    c.auto_outline(core.OUTLINE)
    # bevel/emboss: brighten top edges, darken bottom
    img0 = c
    # warm rim on tops
    for y in range(Hh):
        for x in range(W):
            if c.px[y][x][3] > 200 and c.px[y][x][:3] != core.OUTLINE:
                if c.get(x, y - 1)[3] < 100:
                    cur = c.px[y][x]
                    c.setf(x, y, lerp(cur[:3], core.AMBER, 0.5) + (255,))
    # magic purple accent: a glowing rune-swirl flanking the SKY word (Skypillar)
    for side in (50, 206):
        for a in range(0, 720, 12):
            rad = math.radians(a)
            r = a / 720 * 12
            x = side + int(math.cos(rad) * r)
            y = 26 + int(math.sin(rad) * r)
            col = lerp(core.MAGIC, (180, 140, 255), (a / 720))
            c.set(x, y, col)
        c.disc(side, 26, 2, lerp(core.MAGIC, (220, 200, 255), 0.6))
    # subtle floating-ember sparkle
    for _ in range(20):
        x = rng.randint(10, W - 10); y = rng.randint(2, Hh - 2)
        if c.get(x, y)[3] < 30:
            c.set(x, y, (core.AMBER[0], core.AMBER[1], core.AMBER[2], 120))
    return c.to_image()


def _font5x7():
    F = {
        "S": ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
        "K": ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
        "Y": ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
        "H": ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
        "A": ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
        "R": ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
        "V": ["10001", "10001", "10001", "10001", "01010", "01010", "00100"],
        "E": ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
        "T": ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
    }
    return F


def generate():
    core.save_single(panel(), "ui/panel.png")
    core.save_single(button(False), "ui/button.png")
    core.save_single(button(True), "ui/button_pressed.png")
    core.save_single(slot(), "ui/slot.png")
    core.save_single(card_frame(), "ui/card_frame.png")
    core.save_single(logo(), "ui/logo.png")


if __name__ == "__main__":
    generate()
    print("ui done")
