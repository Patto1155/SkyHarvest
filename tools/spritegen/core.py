"""Shared pixel-art library for SkyHarvest sprite generation.

Palette constants are taken verbatim from docs/CONVENTIONS.md (BINDING).
Drawing helpers: hard-pixel primitives, dithering, outlines, shading ramps,
iso-diamond fill, deterministic seeded RNG per sprite name.

NO anti-aliasing anywhere: every pixel is placed exactly. All blends are
nearest/threshold; alpha is either composited as hard layers or kept binary
except where a soft FX explicitly wants a gradient.
"""

from __future__ import annotations

import hashlib
import math
import random
from typing import Iterable, Sequence

from PIL import Image

# ---------------------------------------------------------------------------
# Output root
# ---------------------------------------------------------------------------
import os

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SPRITES_ROOT = os.path.join(REPO_ROOT, "Assets", "Resources", "Sprites")


# ---------------------------------------------------------------------------
# Palette (from CONVENTIONS.md — binding). All as RGB tuples.
# ---------------------------------------------------------------------------
def _h(s: str) -> tuple[int, int, int]:
    s = s.lstrip("#")
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16))


# Base / structural
CHARCOAL = _h("1d1a1d")   # base charcoal
STONE = _h("4a4a52")      # stone
TIMBER = _h("6b4a2f")     # timber
RUST = _h("8a4a2a")       # rust
# Warm accents
FORGE = _h("e07b2a")      # forge-orange
AMBER = _h("ffb347")      # lantern amber
CROP_GOLD = _h("d9a440")  # crop gold
# Foliage
LEAF_D = _h("5a7a3a")     # foliage dark
LEAF_L = _h("7a9a4a")     # foliage light
# Sky / atmosphere
STORM = _h("3a4150")      # sky storm-grey
FOG = _h("9aa0ab")        # fog
# Magic
MAGIC = _h("7a4fd0")      # purple (Skypillar / logo accent only)

# Derived working colours (kept in-palette family; weather-beaten, desaturated)
OUTLINE = _h("160f10")        # deep brown-charcoal outline (NOT pure black)
OUTLINE_WARM = _h("2a1410")   # warm outline for lit/wood materials
SHADOW = (0, 0, 0)            # used only with low alpha for cast shadows

TRANSPARENT = (0, 0, 0, 0)


# ---------------------------------------------------------------------------
# Colour math
# ---------------------------------------------------------------------------
def clamp8(v: float) -> int:
    return 0 if v < 0 else 255 if v > 255 else int(v)


def shade(c, factor: float):
    """Multiply toward black (factor<1) or white (factor>1, additive-ish)."""
    if factor <= 1.0:
        return (clamp8(c[0] * factor), clamp8(c[1] * factor), clamp8(c[2] * factor))
    # brighten: lerp toward white by (factor-1)
    t = min(1.0, factor - 1.0)
    return (
        clamp8(c[0] + (255 - c[0]) * t),
        clamp8(c[1] + (255 - c[1]) * t),
        clamp8(c[2] + (255 - c[2]) * t),
    )


def lerp(a, b, t: float):
    return (
        clamp8(a[0] + (b[0] - a[0]) * t),
        clamp8(a[1] + (b[1] - a[1]) * t),
        clamp8(a[2] + (b[2] - a[2]) * t),
    )


def mix(a, b, t: float):
    return lerp(a, b, t)


def ramp(base, dark, light, steps: int) -> list:
    """Build a shading ramp of `steps` tones from dark->base->light.

    Returns list[steps] where index 0 = darkest, last = lightest, with base
    near the middle. Used for deliberate 3-5 tone material shading.
    """
    out = []
    half = (steps - 1) / 2.0
    for i in range(steps):
        if i < half:
            t = i / half if half else 0
            out.append(lerp(dark, base, t))
        else:
            t = (i - half) / (steps - 1 - half) if (steps - 1 - half) else 0
            out.append(lerp(base, light, t))
    return out


def material_ramp(base, steps: int = 5, dark_f: float = 0.45, light_f: float = 1.55):
    """Convenience ramp purely from a base colour."""
    return ramp(base, shade(base, dark_f), shade(base, light_f), steps)


# ---------------------------------------------------------------------------
# Deterministic RNG per sprite name
# ---------------------------------------------------------------------------
def rng_for(name: str) -> random.Random:
    seed = int(hashlib.sha256(name.encode("utf-8")).hexdigest()[:16], 16)
    return random.Random(seed)


# ---------------------------------------------------------------------------
# Canvas helper
# ---------------------------------------------------------------------------
class Canvas:
    """RGBA pixel canvas with hard-pixel drawing helpers (no AA)."""

    def __init__(self, w: int, h: int):
        self.w = w
        self.h = h
        self.px = [[(0, 0, 0, 0) for _ in range(w)] for _ in range(h)]

    # -- low level ---------------------------------------------------------
    def _norm(self, c):
        if len(c) == 3:
            return (c[0], c[1], c[2], 255)
        return c

    def set(self, x: int, y: int, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            c = self._norm(c)
            if c[3] >= 255:
                self.px[y][x] = c
            elif c[3] <= 0:
                return
            else:
                self.px[y][x] = self._blend(self.px[y][x], c)

    def setf(self, x: int, y: int, c):
        """Force-set, replacing entirely (used for clearing)."""
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[y][x] = self._norm(c)

    def get(self, x: int, y: int):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[y][x]
        return (0, 0, 0, 0)

    @staticmethod
    def _blend(dst, src):
        sa = src[3] / 255.0
        da = dst[3] / 255.0
        out_a = sa + da * (1 - sa)
        if out_a <= 0:
            return (0, 0, 0, 0)
        r = (src[0] * sa + dst[0] * da * (1 - sa)) / out_a
        g = (src[1] * sa + dst[1] * da * (1 - sa)) / out_a
        b = (src[2] * sa + dst[2] * da * (1 - sa)) / out_a
        return (clamp8(r), clamp8(g), clamp8(b), clamp8(out_a * 255))

    # -- primitives --------------------------------------------------------
    def hline(self, x0, x1, y, c):
        if x1 < x0:
            x0, x1 = x1, x0
        for x in range(x0, x1 + 1):
            self.set(x, y, c)

    def vline(self, x, y0, y1, c):
        if y1 < y0:
            y0, y1 = y1, y0
        for y in range(y0, y1 + 1):
            self.set(x, y, c)

    def rect(self, x0, y0, x1, y1, c):
        if x1 < x0:
            x0, x1 = x1, x0
        if y1 < y0:
            y0, y1 = y1, y0
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.set(x, y, c)

    def rect_outline(self, x0, y0, x1, y1, c):
        self.hline(x0, x1, y0, c)
        self.hline(x0, x1, y1, c)
        self.vline(x0, y0, y1, c)
        self.vline(x1, y0, y1, c)

    def line(self, x0, y0, x1, y1, c):
        dx = abs(x1 - x0)
        dy = -abs(y1 - y0)
        sx = 1 if x0 < x1 else -1
        sy = 1 if y0 < y1 else -1
        err = dx + dy
        while True:
            self.set(x0, y0, c)
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy

    def disc(self, cx, cy, r, c):
        for y in range(cy - r, cy + r + 1):
            for x in range(cx - r, cx + r + 1):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r * r:
                    self.set(x, y, c)

    def ellipse(self, cx, cy, rx, ry, c):
        for y in range(cy - ry, cy + ry + 1):
            for x in range(cx - rx, cx + rx + 1):
                if rx > 0 and ry > 0 and ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0:
                    self.set(x, y, c)

    # -- iso diamond -------------------------------------------------------
    def diamond_fill(self, cx, top_y, half_w, half_h, c):
        """Fill a 2:1 diamond whose top vertex is at (cx, top_y).

        half_w = horizontal half-extent at mid; half_h = vertical half (so the
        diamond is 2*half_w wide and 2*half_h tall). Returns the scanline x
        bounds per row for further detailing.
        """
        bounds = {}
        for row in range(2 * half_h + 1):
            y = top_y + row
            # distance from vertical centre of diamond
            d = abs(row - half_h)
            frac = 1.0 - d / half_h if half_h else 0
            xext = int(round(half_w * frac))
            x0 = cx - xext
            x1 = cx + xext
            self.hline(x0, x1, y, c)
            bounds[y] = (x0, x1)
        return bounds

    def diamond_bounds(self, cx, top_y, half_w, half_h):
        bounds = {}
        for row in range(2 * half_h + 1):
            y = top_y + row
            d = abs(row - half_h)
            frac = 1.0 - d / half_h if half_h else 0
            xext = int(round(half_w * frac))
            bounds[y] = (cx - xext, cx + xext)
        return bounds

    # -- outline -----------------------------------------------------------
    def auto_outline(self, color, where=None, diagonal=True):
        """Draw 1px outline around all opaque pixels into transparent neighbours."""
        adds = []
        offs = [(-1, 0), (1, 0), (0, -1), (0, 1)]
        if diagonal:
            offs += [(-1, -1), (1, -1), (-1, 1), (1, 1)]
        for y in range(self.h):
            for x in range(self.w):
                if self.px[y][x][3] > 0:
                    continue
                hit = False
                for dx, dy in offs:
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < self.w and 0 <= ny < self.h and self.px[ny][nx][3] > 200:
                        if where is None or where(x, y):
                            hit = True
                            break
                if hit:
                    adds.append((x, y))
        for x, y in adds:
            self.setf(x, y, color)

    def shadow_ellipse(self, cx, cy, rx, ry, alpha=90):
        for y in range(cy - ry, cy + ry + 1):
            for x in range(cx - rx, cx + rx + 1):
                if rx > 0 and ry > 0 and ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0:
                    self.set(x, y, (0, 0, 0, alpha))

    # -- export ------------------------------------------------------------
    def to_image(self) -> Image.Image:
        img = Image.new("RGBA", (self.w, self.h))
        flat = []
        for row in self.px:
            flat.extend(row)
        img.putdata(flat)
        return img

    def paste_canvas(self, other: "Canvas", ox: int, oy: int):
        for y in range(other.h):
            for x in range(other.w):
                p = other.px[y][x]
                if p[3] > 0:
                    self.set(ox + x, oy + y, p)


# ---------------------------------------------------------------------------
# Isometric volume primitives (2:1 dimetric — matches the 64x32 tile diamond)
#
# A box "sits" on a diamond footprint whose BOTTOM vertex is at (cx, base_y).
# half_w/half_h are the footprint half-extents (use 32/16 for a full 1x1 tile).
# `height` is how many pixels the box rises. Faces are passed as either a flat
# RGB(A) colour or a callable f(u, v) -> colour, with u across the face [0..1]
# and v up the face [0..1] (v=0 at the base, 1 at the top) so callers can paint
# planks / stone / grain per face. Light convention = upper-LEFT, matching the
# rest of the art: top brightest, left face mid, right face darkest.
# ---------------------------------------------------------------------------
def _rcol(col, x, y, v):
    """Resolve a face colour: either a flat RGB(A) tuple or a callable f(x, y, v)."""
    return col(x, y, v) if callable(col) else col


def iso_face_left(canvas, cx, base_y, half_w, half_h, height, col):
    """Left-front face: the footprint edge from the left vertex to the bottom vertex, extruded up."""
    for x in range(cx - half_w, cx + 1):
        u = (x - (cx - half_w)) / half_w if half_w else 0.0
        by = (base_y - half_h) + u * half_h          # bottom edge slopes down toward cx
        top = by - height
        span = by - top
        for yy in range(int(round(top)), int(round(by)) + 1):
            v = (by - yy) / span if span else 0.0
            canvas.set(x, yy, _rcol(col, x, yy, v))


def iso_face_right(canvas, cx, base_y, half_w, half_h, height, col):
    """Right-front face: bottom vertex to right vertex, extruded up."""
    for x in range(cx, cx + half_w + 1):
        u = (x - cx) / half_w if half_w else 0.0
        by = base_y - u * half_h                      # bottom edge slopes up toward the right vertex
        top = by - height
        span = by - top
        for yy in range(int(round(top)), int(round(by)) + 1):
            v = (by - yy) / span if span else 0.0
            canvas.set(x, yy, _rcol(col, x, yy, v))


def iso_top(canvas, cx, base_y, half_w, half_h, height, col):
    """Top diamond face, lifted `height` px above the footprint."""
    bottom_vy = base_y - height
    top_y = bottom_vy - 2 * half_h
    for row in range(2 * half_h + 1):
        y = top_y + row
        d = abs(row - half_h)
        frac = 1.0 - d / half_h if half_h else 0.0
        xext = int(round(half_w * frac))
        for x in range(cx - xext, cx + xext + 1):
            vv = row / (2 * half_h) if half_h else 0.0
            canvas.set(x, y, _rcol(col, x, y, vv))


def iso_box(canvas, cx, base_y, half_w, half_h, height,
            top, left, right, draw_top=True):
    """Draw a full iso box. Faces drawn back-to-front so the top reads cleanly."""
    iso_face_left(canvas, cx, base_y, half_w, half_h, height, left)
    iso_face_right(canvas, cx, base_y, half_w, half_h, height, right)
    if draw_top:
        iso_top(canvas, cx, base_y, half_w, half_h, height, top)


def fill_poly(canvas, pts, color_fn):
    """Scanline-fill a convex polygon. color_fn(x, y) -> colour (or a flat colour)."""
    flat = color_fn if callable(color_fn) else (lambda x, y: color_fn)
    ys = [p[1] for p in pts]
    y0, y1 = int(math.floor(min(ys))), int(math.ceil(max(ys)))
    n = len(pts)
    for y in range(y0, y1 + 1):
        xs = []
        for i in range(n):
            ax, ay = pts[i]
            bx, by = pts[(i + 1) % n]
            if (ay <= y < by) or (by <= y < ay):
                t = (y - ay) / (by - ay)
                xs.append(ax + (bx - ax) * t)
        if not xs:
            continue
        xl, xr = int(round(min(xs))), int(round(max(xs)))
        for x in range(xl, xr + 1):
            canvas.set(x, y, flat(x, y))


def iso_frustum(canvas, cx, base_y, hw_b, hh_b, hw_t, hh_t, height,
                top, left, right, draw_top=True):
    """Tapered iso box (a frustum): top diamond smaller than the bottom, centred
    over it. Faces are trapezoids. Face fns take (x, y, v) with v=0 at the base
    and v=1 at the top; `top` takes (x, y, v) with v across the diamond depth.
    Use for furnace / kiln / barrel-ish bodies that narrow as they rise."""
    cyb = base_y - hh_b              # bottom diamond centre
    cyt = cyb - height               # top diamond centre
    Bb = (cx, base_y)
    Lb = (cx - hw_b, cyb)
    Rb = (cx + hw_b, cyb)
    Bt = (cx, cyt + hh_t)
    Lt = (cx - hw_t, cyt)
    Rt = (cx + hw_t, cyt)
    Tt = (cx, cyt - hh_t)

    def _wrap(fn, ymin, ymax):
        if not callable(fn):
            return lambda x, y: fn
        span = max(1, ymax - ymin)
        return lambda x, y: fn(x, y, (ymax - y) / span)

    # left-front trapezoid
    fill_poly(canvas, [Lb, Bb, Bt, Lt], _wrap(left, min(Lt[1], Bt[1]), max(Lb[1], Bb[1])))
    # right-front trapezoid
    fill_poly(canvas, [Bb, Rb, Rt, Bt], _wrap(right, min(Bt[1], Rt[1]), max(Bb[1], Rb[1])))
    if draw_top:
        fill_poly(canvas, [Lt, Tt, Rt, Bt], _wrap(top, Tt[1], Bt[1]))
    return dict(cyt=cyt, hw_t=hw_t, hh_t=hh_t)


# ---------------------------------------------------------------------------
# Dithering patterns (Bayer-style, deterministic, hard pixels)
# ---------------------------------------------------------------------------
_BAYER4 = [
    [0, 8, 2, 10],
    [12, 4, 14, 6],
    [3, 11, 1, 9],
    [15, 7, 13, 5],
]


def dither_at(x: int, y: int, level: float) -> bool:
    """Ordered dither test. level 0..1 = probability the pixel is 'on'."""
    t = (_BAYER4[y & 3][x & 3] + 0.5) / 16.0
    return level > t


def checker(x: int, y: int) -> bool:
    return (x + y) & 1 == 0


def speckle(rng: random.Random, density: float) -> bool:
    return rng.random() < density


# ---------------------------------------------------------------------------
# High-level material texture helpers
# ---------------------------------------------------------------------------
def dither_region(canvas: Canvas, x0, y0, x1, y1, base, accent, level, mask=None):
    """Fill a region with base, scattering accent via ordered dither."""
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if mask and not mask(x, y):
                continue
            c = accent if dither_at(x, y, level) else base
            canvas.set(x, y, c)


def wood_grain(canvas: Canvas, x0, y0, x1, y1, tones, rng, vertical=True):
    """Plank/wood grain shading. tones = ramp (dark->light)."""
    n = len(tones)
    for x in range(x0, x1 + 1):
        # per-column base tone wobble = grain
        col = (x - x0)
        base_i = 1 + int((math.sin(col * 0.9) + 1) * 0.5 * (n - 2))
        for y in range(y0, y1 + 1):
            row = (y - y0)
            streak = math.sin(col * 1.7 + row * 0.25) + math.sin(col * 0.4)
            i = base_i + (1 if streak > 1.1 else -1 if streak < -1.1 else 0)
            i = max(0, min(n - 1, i))
            c = tones[i]
            # occasional dark grain knot line
            if rng.random() < 0.015:
                c = tones[0]
            canvas.set(x, y, c)


def save_strip(frames: Sequence[Image.Image], rel_path: str, frame_w: int, frame_h: int):
    """Compose frames horizontally and save under SPRITES_ROOT/rel_path."""
    n = len(frames)
    strip = Image.new("RGBA", (frame_w * n, frame_h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        if f.size != (frame_w, frame_h):
            raise ValueError(f"{rel_path} frame {i} is {f.size}, expected {(frame_w, frame_h)}")
        strip.paste(f, (i * frame_w, 0))
    out = os.path.join(SPRITES_ROOT, rel_path)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    strip.save(out)
    return out


def save_single(img: Image.Image, rel_path: str):
    out = os.path.join(SPRITES_ROOT, rel_path)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    img.save(out)
    return out


# ---------------------------------------------------------------------------
# Coordinates (from CONVENTIONS "Coordinates" section)
# ---------------------------------------------------------------------------
PIXELS_PER_UNIT = 64
ELEVATION_WORLD_STEP = 0.25  # plausible; tiles are 1.0 x 0.5 world units


def grid_to_world(gx, gy, elevation=0.0):
    wx = (gx - gy) * 0.5
    wy = (gx + gy) * -0.25 + elevation * ELEVATION_WORLD_STEP
    return wx, wy


def grid_to_screen(gx, gy, origin_x, origin_y, elevation=0.0):
    """World->pixel. Screen y grows downward, world y grows up, so negate.

    Tile diamond top face: 64 wide, 32 tall. So x step is 32px per half-unit,
    y step 16px per quarter-unit -> matches a 64x32 diamond per cell.
    """
    wx, wy = grid_to_world(gx, gy, elevation)
    sx = origin_x + wx * PIXELS_PER_UNIT
    sy = origin_y - wy * PIXELS_PER_UNIT
    return int(round(sx)), int(round(sy))
