"""Background sky layers: 512x256, horizontally tileable.

sky_far : moody distant cloud bank (lighter, lower contrast)
sky_near: darker cloud wisps (higher contrast, faster parallax)

Horizontal seamlessness: all noise/cloud functions are periodic in x with
period = 512, so the left and right edges meet. We use sums of sines with
integer frequencies over the 512 width.
"""

from __future__ import annotations
import math
from PIL import Image
from . import core
from .core import Canvas, shade, lerp

W, H = 512, 256


def _periodic_noise(x, y, freqs, seed):
    """Deterministic periodic-in-x value in [0,1]."""
    v = 0.0
    amp = 1.0
    tot = 0.0
    rng = core.rng_for(f"{seed}")
    for k in freqs:
        ph = rng.uniform(0, math.tau)
        phy = rng.uniform(0, math.tau)
        v += amp * math.sin(x / W * math.tau * k + ph + math.sin(y * 0.02 * k + phy))
        tot += amp
        amp *= 0.55
    return (v / tot + 1) / 2


def sky_far():
    c = Canvas(W, H)
    rng = core.rng_for("sky_far")
    top = core.STORM
    bottom = lerp(core.STORM, core.CHARCOAL, 0.45)
    cloud = lerp(core.FOG, core.STORM, 0.4)
    cloud_lit = lerp(core.FOG, core.AMBER, 0.12)
    for y in range(H):
        ty = y / H
        sky = lerp(top, bottom, ty)
        for x in range(W):
            n = _periodic_noise(x, y, [1, 2, 3, 5], "sky_far_n")
            # horizontal cloud bands
            band = _periodic_noise(x, y * 0.3, [1, 2], "sky_far_b")
            cloudiness = (n * 0.6 + band * 0.4)
            # clouds sit in mid-upper region
            region = math.exp(-((ty - 0.45) ** 2) / 0.08)
            cv = cloudiness * region
            if cv > 0.55:
                t = (cv - 0.55) / 0.45
                col = lerp(sky, cloud, min(1, t * 1.4))
                # rim-lit cloud tops (warm)
                if n > 0.72 and band > 0.5:
                    col = lerp(col, cloud_lit, 0.4)
                c.setf(x, y, col + (255,))
            else:
                c.setf(x, y, sky + (255,))
    return c.to_image()


def sky_near():
    c = Canvas(W, H)
    rng = core.rng_for("sky_near")
    base = lerp(core.STORM, core.CHARCOAL, 0.6)
    wisp = shade(core.STORM, 0.7)
    wisp_lit = lerp(wisp, core.FOG, 0.4)
    # mostly transparent layer of dark wisps that overlays sky_far
    for y in range(H):
        ty = y / H
        for x in range(W):
            n = _periodic_noise(x, y, [2, 3, 5, 7], "sky_near_n")
            streak = _periodic_noise(x * 1.0, y * 0.15, [1, 3], "sky_near_s")
            v = n * 0.5 + streak * 0.5
            # darker wisps drift across lower 2/3
            region = math.exp(-((ty - 0.6) ** 2) / 0.12)
            cv = v * region
            if cv > 0.6:
                t = (cv - 0.6) / 0.4
                a = int(min(220, t * 260))
                a = (a // 20) * 20  # quantize for hard-ish bands
                col = wisp
                if n > 0.75:
                    col = wisp_lit
                c.setf(x, y, (col[0], col[1], col[2], a))
    return c.to_image()


def generate():
    core.save_single(sky_far(), "bg/sky_far.png")
    core.save_single(sky_near(), "bg/sky_near.png")


if __name__ == "__main__":
    generate()
    print("bg done")
