"""Player sprites: gritty scavenger-farmer, 48x64, 4 directions.

Per the avatar concept: stocky, weathered dark coat, big salvage backpack,
headband, muddy boots, leather straps. Holds a tool.

States:
  idle  : 4-frame breathing
  walk  : 6-frame leg-swing + bob
  action: 4-frame overhead tool swing

Directions: s (toward camera/down), n (back), e, w (e/w are real generated
files; w is a mirror of e per CONVENTIONS allowance, but we generate both).

The figure is built from parametric body parts so animation = transforming
part offsets, keeping a consistent silhouette and pixel-cluster discipline.
"""

from __future__ import annotations
import math
from PIL import Image
from . import core
from .core import Canvas, shade, lerp, material_ramp

W, H = 48, 64
GROUND_Y = 61  # feet baseline

# ---- palette for the character (weather-beaten) ----------------------------
SKIN = core.shade((196, 150, 116), 0.92)
SKIN_R = material_ramp(SKIN, 5, 0.55, 1.25)
COAT = lerp(core.CHARCOAL, core.TIMBER, 0.35)        # dark olive-brown coat
COAT_R = material_ramp(COAT, 5, 0.5, 1.5)
PANTS = lerp(core.CHARCOAL, core.STONE, 0.4)
PANTS_R = material_ramp(PANTS, 5, 0.5, 1.4)
BOOT = shade(core.TIMBER, 0.55)
BOOT_R = material_ramp(BOOT, 4, 0.5, 1.4)
PACK = shade(core.TIMBER, 0.8)
PACK_R = material_ramp(PACK, 5, 0.45, 1.45)
STRAP = shade(core.RUST, 0.75)
HEADBAND = core.RUST
HAIR = shade(core.TIMBER, 0.4)
HAIR_R = material_ramp(HAIR, 4, 0.5, 1.5)
TOOL_WOOD = material_ramp(core.TIMBER, 4, 0.5, 1.3)
TOOL_IRON = material_ramp(core.STONE, 5, 0.45, 1.6)
RIM = lerp(core.AMBER, core.FOG, 0.4)  # warm rim light for readability on dark bg


def _shaded_col(part_ramp, nx, ny, bias=0.0):
    lite = (-nx - ny * 0.6) * 0.5 + 0.5 + bias
    idx = int(lite * (len(part_ramp) - 1) + 0.5)
    return part_ramp[max(0, min(len(part_ramp) - 1, idx))]


# ---------------------------------------------------------------------------
# Limb / body primitives. We draw filled capsules and boxes with a ramp.
# ---------------------------------------------------------------------------
def fill_box(c, x0, y0, x1, y1, ramp, light_left=True):
    if x1 < x0: x0, x1 = x1, x0
    if y1 < y0: y0, y1 = y1, y0
    w = max(1, x1 - x0)
    h = max(1, y1 - y0)
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            nx = (x - x0) / w * 2 - 1
            ny = (y - y0) / h * 2 - 1
            if not light_left:
                nx = -nx
            col = _shaded_col(ramp, nx, ny)
            c.set(x, y, col)


def fill_capsule(c, cx, y0, y1, r, ramp, light_left=True):
    for y in range(y0, y1 + 1):
        # rounded ends
        rr = r
        if y < y0 + r:
            dy = (y0 + r) - y
            rr = int(round(math.sqrt(max(0, r * r - dy * dy))))
        elif y > y1 - r:
            dy = y - (y1 - r)
            rr = int(round(math.sqrt(max(0, r * r - dy * dy))))
        for x in range(cx - rr, cx + rr + 1):
            nx = (x - cx) / max(1, r)
            ny = (y - y0) / max(1, (y1 - y0)) * 2 - 1
            if not light_left:
                nx = -nx
            col = _shaded_col(ramp, nx, ny)
            c.set(x, y, col)


# ---------------------------------------------------------------------------
# Head builders per direction
# ---------------------------------------------------------------------------
def draw_head(c, cx, cy, facing):
    # head ~ 11 wide 12 tall
    if facing in ("s", "e", "w"):
        # face visible (e/w in profile-ish but we keep frontal-lean for clarity)
        fill_capsule(c, cx, cy - 6, cy + 5, 5, SKIN_R)
        # hair top + sides
        for y in range(cy - 8, cy - 1):
            rr = 5 - max(0, (y - (cy - 6)))
            rr = max(2, 5 - abs(y - (cy - 6)) // 2)
            for x in range(cx - 5, cx + 6):
                if abs(x - cx) <= rr + 1 and y < cy - 2:
                    c.set(x, y, _shaded_col(HAIR_R, (x - cx) / 5, -0.5))
        # headband
        for x in range(cx - 5, cx + 6):
            c.set(x, cy - 2, HEADBAND)
            c.set(x, cy - 3, shade(HEADBAND, 1.2) if x < cx else HEADBAND)
        # band knot tail
        c.set(cx + 5, cy - 1, shade(HEADBAND, 0.8))
        c.set(cx + 6, cy, shade(HEADBAND, 0.7))
        if facing == "s":
            # eyes (dark) + brow shadow
            c.set(cx - 2, cy, core.OUTLINE)
            c.set(cx + 2, cy, core.OUTLINE)
            c.hline(cx - 3, cx + 3, cy - 1, shade(SKIN, 0.8))
            # stubble / jaw shadow
            c.hline(cx - 3, cx + 3, cy + 4, shade(SKIN, 0.75))
            # nose
            c.set(cx, cy + 1, shade(SKIN, 0.82))
        else:
            # profile: one eye, nose bump on the facing side
            side = 1 if facing == "e" else -1
            c.set(cx + side * 2, cy, core.OUTLINE)
            c.set(cx + side * 5, cy + 1, SKIN_R[3])  # nose
            c.set(cx + side * 5, cy + 2, shade(SKIN, 0.85))
            c.hline(cx - 3, cx + 3, cy + 4, shade(SKIN, 0.75))
    else:  # n -> back of head, all hair + band
        fill_capsule(c, cx, cy - 6, cy + 5, 5, HAIR_R)
        for x in range(cx - 5, cx + 6):
            c.set(x, cy - 2, HEADBAND)
        c.set(cx + 5, cy - 1, shade(HEADBAND, 0.8))
        c.set(cx + 6, cy, shade(HEADBAND, 0.7))
        # neck
    # neck
    c.rect(cx - 2, cy + 5, cx + 2, cy + 6, shade(SKIN, 0.8))


# ---------------------------------------------------------------------------
# Tool drawing (axe/hoe hybrid: a hafted tool)
# ---------------------------------------------------------------------------
def draw_tool(c, hx, hy, angle, length=20):
    """Draw a hafted tool from hand (hx,hy) along angle (radians, screen)."""
    ex = hx + int(math.cos(angle) * length)
    ey = hy + int(math.sin(angle) * length)
    # haft
    dx = math.cos(angle + math.pi / 2)
    dy = math.sin(angle + math.pi / 2)
    for t in range(length + 1):
        x = hx + math.cos(angle) * t
        y = hy + math.sin(angle) * t
        for w in (-1, 0, 1):
            col = TOOL_WOOD[2 + (1 if w < 0 else -1)]
            c.set(int(round(x + dx * w)), int(round(y + dy * w)), col)
    # iron head at the end (axe/hoe blade block)
    for ox in range(-3, 4):
        for oy in range(-3, 4):
            px = ex + ox
            py = ey + oy
            if abs(ox) + abs(oy) <= 4:
                col = TOOL_IRON[2 + (1 if ox < 0 else -1)]
                c.set(px, py, col)
    # blade highlight
    c.set(ex - 2, ey - 2, TOOL_IRON[-1])


# ---------------------------------------------------------------------------
# Body assembly
# ---------------------------------------------------------------------------
def draw_body(c, facing, bob=0, larm=0.0, rarm=0.0, lleg=0, rleg=0,
              tool_angle=None, pack=True):
    cx = 24
    hip_y = 44 + bob
    # cast shadow
    c.shadow_ellipse(cx, GROUND_Y + 1, 11, 3, alpha=80)

    # --- legs ---
    leg_top = hip_y
    leg_bot = GROUND_Y - 3
    lx = cx - 4
    rx = cx + 4
    # leg swing offsets shift the foot horizontally
    fill_box(c, lx - 2 + lleg, leg_top, lx + 2 + lleg, leg_bot, PANTS_R)
    fill_box(c, rx - 2 + rleg, leg_top, rx + 2 + rleg, leg_bot, PANTS_R, light_left=False)
    # boots
    fill_box(c, lx - 3 + lleg, leg_bot, lx + 3 + lleg, GROUND_Y, BOOT_R)
    fill_box(c, rx - 3 + rleg, leg_bot, rx + 3 + rleg, GROUND_Y, BOOT_R, light_left=False)
    # boot toe forward
    c.set(lx - 3 + lleg, GROUND_Y, shade(BOOT, 0.7))
    c.set(rx + 3 + rleg, GROUND_Y, shade(BOOT, 0.7))

    # --- backpack (drawn behind torso for s/e/w, in front silhouette for n) ---
    if pack and facing != "n":
        fill_box(c, cx - 9, hip_y - 16, cx - 5, hip_y - 2, PACK_R)
        fill_box(c, cx + 5, hip_y - 16, cx + 9, hip_y - 2, PACK_R, light_left=False)
        # actually for s view the pack is mostly hidden; show shoulder humps
        c.rect(cx - 9, hip_y - 17, cx - 5, hip_y - 15, shade(PACK, 0.7))

    # --- torso / coat ---
    torso_top = hip_y - 18
    fill_box(c, cx - 7, torso_top, cx + 7, hip_y, COAT_R)
    # coat is open: vertical seam + inner darker
    c.vline(cx, torso_top + 2, hip_y, shade(COAT, 0.6))
    c.vline(cx - 1, torso_top + 2, hip_y, shade(COAT, 0.7))
    # straps crossing chest (for s/e/w)
    if facing != "n":
        for t in range(20):
            x = cx - 6 + t * 0.6
            y = torso_top + 1 + t * 0.85
            c.set(int(x), int(y), STRAP)
            c.set(int(48 - x - 1) if False else int(cx + 6 - t * 0.6), int(y), shade(STRAP, 0.8))
    else:
        # back view: big backpack dominates
        fill_box(c, cx - 9, torso_top - 1, cx + 9, hip_y - 2, PACK_R)
        # pack flap + buckles
        c.rect(cx - 8, torso_top + 4, cx + 8, torso_top + 7, shade(PACK, 0.7))
        c.set(cx - 5, torso_top + 5, core.RUST)
        c.set(cx + 5, torso_top + 5, core.RUST)
        # bedroll on top
        c.rect(cx - 8, torso_top - 2, cx + 8, torso_top, shade(core.RUST, 0.8))
        # straps
        c.vline(cx - 5, torso_top, hip_y - 4, STRAP)
        c.vline(cx + 5, torso_top, hip_y - 4, STRAP)

    # belt
    c.hline(cx - 7, cx + 7, hip_y - 1, shade(core.TIMBER, 0.5))
    c.set(cx, hip_y - 1, core.AMBER)  # buckle glint

    # --- arms --- (shoulders at torso_top+2)
    sh_y = torso_top + 3
    # left arm
    la_x = cx - 7
    lhand = _arm(c, la_x, sh_y, larm, COAT_R, SKIN_R, side=-1)
    # right arm
    ra_x = cx + 7
    rhand = _arm(c, ra_x, sh_y, rarm, COAT_R, SKIN_R, side=1)

    # --- head ---
    head_cy = torso_top - 4
    draw_head(c, cx, head_cy, facing)

    # --- tool in right hand ---
    if tool_angle is not None:
        draw_tool(c, rhand[0], rhand[1], tool_angle)

    return rhand


def _arm(c, sx, sy, swing, coat_ramp, skin_ramp, side=1):
    """Draw an arm hanging from shoulder with a swing angle (radians)."""
    length = 15
    ex = sx + int(math.sin(swing) * length * side * 0.5 + math.sin(swing) * 2)
    # arm goes down with swing tilt
    ang = math.pi / 2 + swing * 0.5 * side
    hx = sx + int(math.cos(ang) * length)
    hy = sy + int(math.sin(ang) * length)
    # upper+forearm as a thick line
    for t in range(length + 1):
        x = sx + (hx - sx) * t / length
        y = sy + (hy - sy) * t / length
        for w in (-1, 0, 1, 2):
            nx = w / 2
            col = coat_ramp[2 + (1 if w < 0 else -1)] if t < length - 3 else skin_ramp[2]
            c.set(int(round(x + w)), int(round(y)), col)
    # hand
    c.disc(hx, hy + 1, 2, skin_ramp[2])
    c.set(hx - 1, hy, skin_ramp[3])
    return (hx, hy + 1)


# ---------------------------------------------------------------------------
# Frame generators
# ---------------------------------------------------------------------------
def make_frame(facing, bob=0, larm=0, rarm=0, lleg=0, rleg=0, tool_angle=None,
               rim=True):
    c = Canvas(W, H)
    draw_body(c, facing, bob=bob, larm=larm, rarm=rarm, lleg=lleg, rleg=rleg,
              tool_angle=tool_angle)
    # outline (deep brown-charcoal) then warm rim light on upper-left edges
    c.auto_outline(core.OUTLINE)
    if rim:
        _rim_light(c)
    return c


def _rim_light(c):
    """Add a 1px warm rim on the upper-left silhouette so the dark figure reads."""
    for y in range(c.h):
        for x in range(c.w):
            if c.px[y][x][3] > 200:
                # is the up-left neighbour empty/outline?
                up = c.get(x, y - 1)
                left = c.get(x - 1, y)
                if up[3] < 40 or (up[:3] == core.OUTLINE and left[3] < 40):
                    # only on actual top edge
                    if c.get(x, y - 1)[3] < 200:
                        cur = c.px[y][x]
                        c.setf(x, y, lerp(cur[:3], RIM, 0.5) + (255,))
                        break_inner = False


def mirror(img: Image.Image) -> Image.Image:
    return img.transpose(Image.FLIP_LEFT_RIGHT)


def idle_frames(facing):
    frames = []
    # breathing: subtle bob + arm sway, 4 frames
    bobs = [0, -1, 0, 1] if False else [0, 0, 1, 0]
    for i in range(4):
        b = [0, 0, 1, 0][i]
        sway = [0.0, 0.05, 0.0, -0.05][i]
        c = make_frame(facing, bob=b, larm=sway, rarm=-sway)
        frames.append(c.to_image())
    return frames


def walk_frames(facing):
    frames = []
    # 6-frame cycle: legs swing opposite, body bobs at contact
    for i in range(6):
        ph = i / 6.0 * 2 * math.pi
        swing = math.sin(ph)
        lleg = int(round(swing * 3))
        rleg = -lleg
        bob = 1 if abs(swing) > 0.7 else 0  # lowest at contact
        # arms counter-swing
        larm = -swing * 0.6
        rarm = swing * 0.6
        c = make_frame(facing, bob=bob, larm=larm, rarm=rarm, lleg=lleg, rleg=rleg)
        frames.append(c.to_image())
    return frames


def action_frames(facing):
    frames = []
    # overhead tool swing: raise -> apex -> strike -> recover
    angles = [-2.4, -2.0, -0.4, 0.5]  # tool angle relative to hand, screen rads
    bobs = [0, -1, 1, 0]
    rarms = [-0.9, -1.1, 0.4, 0.1]
    for i in range(4):
        c = make_frame(facing, bob=bobs[i], rarm=rarms[i], larm=0.2,
                       tool_angle=angles[i])
        frames.append(c.to_image())
    return frames


# ---------------------------------------------------------------------------
def generate():
    for facing in ("s", "n", "e", "w"):
        # e/w: generate e, mirror for w to guarantee consistency, but the spec
        # wants both files present. We render e then mirror to w.
        if facing == "w":
            # mirror the e-frames
            idle = [mirror(f) for f in idle_frames("e")]
            walk = [mirror(f) for f in walk_frames("e")]
            action = [mirror(f) for f in action_frames("e")]
        else:
            idle = idle_frames(facing)
            walk = walk_frames(facing)
            action = action_frames(facing)
        core.save_strip(idle, f"player/player_idle_{facing}.png", W, H)
        core.save_strip(walk, f"player/player_walk_{facing}.png", W, H)
        core.save_strip(action, f"player/player_action_{facing}.png", W, H)


if __name__ == "__main__":
    generate()
    print("player done")
