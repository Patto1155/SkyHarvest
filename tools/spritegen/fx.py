"""FX sprites: rain drop, wind streak, fog blob, sparkle.

These DO use soft alpha where it reads better (fog blob, sparkle glow), but
edges stay hard-pixel; no full-canvas anti-aliasing.
"""

from __future__ import annotations
import math
from PIL import Image
from . import core
from .core import Canvas, shade, lerp


def rain_drop():
    """4x12 vertical streak."""
    c = Canvas(4, 12)
    col = lerp(core.STORM, core.FOG, 0.5)
    for y in range(12):
        t = y / 12
        a = int(60 + 160 * t)  # brighter/denser toward bottom
        c.set(1, y, (col[0], col[1], col[2], a))
        c.set(2, y, (col[0], col[1], col[2], max(0, a - 40)))
    # bright head
    c.set(1, 11, (220, 230, 240, 255))
    c.set(2, 11, (200, 215, 230, 200))
    return c.to_image()


def wind_streak():
    """32x4 horizontal motion streak (soft ends)."""
    c = Canvas(32, 4)
    col = lerp(core.FOG, (220, 225, 230), 0.4)
    for x in range(32):
        # taper alpha at both ends, peak in middle-right
        t = x / 31
        env = math.sin(t * math.pi)
        a = int(env * 200)
        c.set(x, 1, (col[0], col[1], col[2], a))
        c.set(x, 2, (col[0], col[1], col[2], int(a * 0.6)))
    return c.to_image()


def fog_blob():
    """64x32 soft alpha blob."""
    c = Canvas(64, 32)
    col = core.FOG
    cx, cy = 32, 16
    for y in range(32):
        for x in range(64):
            d = math.hypot((x - cx) / 30.0, (y - cy) / 14.0)
            if d <= 1.0:
                a = int((1 - d) * (1 - d) * 150)
                # quantize alpha to a few steps (hard-ish, not smooth gradient)
                a = (a // 24) * 24
                # internal wispy variation
                w = math.sin(x * 0.3) * math.cos(y * 0.4)
                a = max(0, a + int(w * 15))
                c.set(x, y, (col[0], col[1], col[2], min(160, a)))
    return c.to_image()


def sparkle():
    """4 frames 16x16 harvest/collect pop (expanding 4-point star)."""
    frames = []
    cx = cy = 8
    cols = [
        lerp(core.AMBER, (255, 250, 200), 0.5),
        core.AMBER,
        core.CROP_GOLD,
        lerp(core.CROP_GOLD, core.FORGE, 0.4),
    ]
    sizes = [2, 5, 7, 4]
    aoverall = [255, 255, 220, 140]
    for f in range(4):
        c = Canvas(16, 16)
        r = sizes[f]
        col = cols[f]
        a = aoverall[f]
        # 4-point star arms
        for d in range(r + 1):
            fade = int(a * (1 - d / (r + 1)))
            c.set(cx + d, cy, (col[0], col[1], col[2], fade))
            c.set(cx - d, cy, (col[0], col[1], col[2], fade))
            c.set(cx, cy + d, (col[0], col[1], col[2], fade))
            c.set(cx, cy - d, (col[0], col[1], col[2], fade))
        # diagonal smaller arms
        dr = r // 2
        for d in range(dr + 1):
            fade = int(a * 0.6 * (1 - d / (dr + 1)))
            for sx, sy in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
                c.set(cx + sx * d, cy + sy * d, (col[0], col[1], col[2], fade))
        # bright core
        c.set(cx, cy, (255, 250, 220, a))
        if f >= 1:
            c.set(cx, cy - 1, (255, 250, 220, a))
            c.set(cx - 1, cy, (255, 250, 220, a))
        # outer twinkle dots on later frames
        if f >= 2:
            for ang in (30, 150, 210, 330):
                ax = cx + int(math.cos(math.radians(ang)) * (r + 2))
                ay = cy + int(math.sin(math.radians(ang)) * (r + 2))
                c.set(ax, ay, (col[0], col[1], col[2], int(a * 0.5)))
        frames.append(c.to_image())
    return frames


def generate():
    core.save_single(rain_drop(), "fx/rain_drop.png")
    core.save_single(wind_streak(), "fx/wind_streak.png")
    core.save_single(fog_blob(), "fx/fog_blob.png")
    core.save_strip(sparkle(), "fx/sparkle.png", 16, 16)


if __name__ == "__main__":
    generate()
    print("fx done")
