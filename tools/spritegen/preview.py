"""Build review contact sheets.

preview.png       : every sprite scaled 3-4x nearest-neighbour, on a dark bg,
                    labelled per row.
preview_scene.png : a mock screenshot — terrain tiles composed into a small
                    dimetric island with structures, crops and the player, to
                    prove tiles seam at the 2:1 layout (math from CONVENTIONS).
"""

from __future__ import annotations
import os
import glob
from PIL import Image, ImageDraw, ImageFont

from . import core
from .core import grid_to_screen

ROOT = core.SPRITES_ROOT
DARK = (24, 22, 26, 255)
DARK2 = (32, 30, 34, 255)
LABEL = (200, 195, 205, 255)


def _load(path):
    return Image.open(os.path.join(ROOT, path)).convert("RGBA")


def _slice_strip(img, fw):
    n = img.width // fw
    return [img.crop((i * fw, 0, (i + 1) * fw, img.height)) for i in range(n)]


def _scale(img, s):
    return img.resize((img.width * s, img.height * s), Image.NEAREST)


def _font():
    try:
        return ImageFont.load_default()
    except Exception:
        return None


# Manifest of what to show, with frame widths for strips.
ROWS = [
    ("terrain fertile/rocky/cliff", [("terrain/tile_fertile_valley.png", 64),
                                     ("terrain/tile_rocky_plateau.png", 64),
                                     ("terrain/tile_cliff_edge.png", 64)], 3),
    ("terrain spring/wind/scaffold", [("terrain/tile_natural_spring.png", 64),
                                      ("terrain/tile_wind_corridor.png", 64),
                                      ("terrain/tile_scaffold.png", 64)], 3),
    ("overlays tilled/wet/dry", [("terrain/overlay_tilled.png", 64),
                                 ("terrain/overlay_wet.png", 64),
                                 ("terrain/overlay_dry.png", 64)], 3),
    ("player idle s/n/e/w", [("player/player_idle_s.png", 48),
                             ("player/player_idle_n.png", 48),
                             ("player/player_idle_e.png", 48),
                             ("player/player_idle_w.png", 48)], 3),
    ("player walk s", [("player/player_walk_s.png", 48)], 3),
    ("player walk n", [("player/player_walk_n.png", 48)], 3),
    ("player action s/e", [("player/player_action_s.png", 48),
                           ("player/player_action_e.png", 48)], 3),
    ("crop sky_moss", [("crops/crop_sky_moss.png", 64)], 3),
    ("crop cloud_root", [("crops/crop_cloud_root.png", 64)], 3),
    ("crop storm_wheat", [("crops/crop_storm_wheat.png", 64)], 3),
    ("crop herb_plant", [("crops/crop_herb_plant.png", 64)], 3),
    ("shelter / rain_catcher", [("structures/shelter.png", 128),
                                ("structures/rain_catcher.png", 64)], 2),
    ("windbreak/path/scaffolding", [("structures/windbreak.png", 64),
                                    ("structures/path.png", 64),
                                    ("structures/scaffolding.png", 64)], 2),
    ("skynet / drying_rack", [("structures/skynet.png", 96),
                              ("structures/drying_rack.png", 96)], 2),
    ("stone_mill (sails)", [("structures/stone_mill.png", 128)], 2),
    ("forge (ember pulse)", [("structures/forge.png", 128)], 2),
    ("crate/barrel/debris", [("structures/crate.png", 48),
                             ("structures/barrel.png", 48),
                             ("debris/debris_1.png", 48),
                             ("debris/debris_2.png", 48),
                             ("debris/debris_3.png", 48)], 3),
]


def _item_rows():
    rows = []
    items = sorted(glob.glob(os.path.join(ROOT, "items", "icon_*.png")))
    cells = [(os.path.relpath(p, ROOT).replace("\\", "/"), 32) for p in items]
    # chunk into rows of 9
    for i in range(0, len(cells), 9):
        rows.append((f"items {i//9+1}", cells[i:i+9], 4))
    tools = sorted(glob.glob(os.path.join(ROOT, "ui", "icon_tool_*.png")))
    tcells = [(os.path.relpath(p, ROOT).replace("\\", "/"), 32) for p in tools]
    rows.append(("tools", tcells, 4))
    return rows


def _ui_fx_rows():
    return [
        ("ui panel/button/slot/card", [("ui/panel.png", 48), ("ui/button.png", 48),
                                       ("ui/button_pressed.png", 48), ("ui/slot.png", 40),
                                       ("ui/card_frame.png", 40)], 3),
        ("ui logo", [("ui/logo.png", 256)], 1),
        ("fx rain/wind/fog/sparkle", [("fx/rain_drop.png", 4), ("fx/wind_streak.png", 32),
                                      ("fx/fog_blob.png", 64), ("fx/sparkle.png", 16)], 4),
        ("bg sky_far", [("bg/sky_far.png", 512)], 1),
        ("bg sky_near (on dark)", [("bg/sky_near.png", 512)], 1),
    ]


def build_contact_sheet():
    font = _font()
    all_rows = ROWS + _item_rows() + _ui_fx_rows()
    pad = 8
    label_w = 150
    row_gap = 10
    # Pre-render each row to its own strip image
    rendered = []
    for label, cells, scale in all_rows:
        imgs = []
        for path, fw in cells:
            try:
                img = _load(path)
            except FileNotFoundError:
                continue
            if img.width > fw:  # strip
                for fr in _slice_strip(img, fw):
                    imgs.append(_scale(fr, scale))
            else:
                imgs.append(_scale(img, scale))
        if not imgs:
            continue
        h = max(i.height for i in imgs)
        w = sum(i.width + pad for i in imgs)
        rendered.append((label, imgs, w, h))

    total_w = label_w + max(r[2] for r in rendered) + pad * 2
    total_h = sum(r[3] + row_gap for r in rendered) + pad * 2
    sheet = Image.new("RGBA", (total_w, total_h), DARK)
    draw = ImageDraw.Draw(sheet)
    y = pad
    for idx, (label, imgs, w, h) in enumerate(rendered):
        # zebra background band
        band = DARK2 if idx % 2 else DARK
        draw.rectangle([0, y - 2, total_w, y + h + row_gap - 4], fill=band)
        draw.text((6, y + h // 2 - 4), label, fill=LABEL, font=font)
        x = label_w
        for im in imgs:
            sheet.alpha_composite(im, (x, y + (h - im.height)))
            x += im.width + pad
        y += h + row_gap
    out = os.path.join(os.path.dirname(__file__), "preview.png")
    sheet.convert("RGB").save(out)
    print("wrote", out, sheet.size)


# ---------------------------------------------------------------------------
# Scene mock — prove tiles seam at the dimetric layout.
# ---------------------------------------------------------------------------
def build_scene():
    """Compose a small floating island and place things on it.

    Uses grid_to_screen with the diamond pivot at the top-of-diamond. A 64x80
    tile's diamond top face is its top 32 rows; tiles drawn back-to-front
    (sorted by gx+gy) seam along their diamond edges.
    """
    SW, SH = 640, 480
    scene = Image.new("RGBA", (SW, SH), (0, 0, 0, 255))
    # paste tiled sky background
    sky_far = _load("bg/sky_far.png")
    sky_near = _load("bg/sky_near.png")
    for ox in range(0, SW, sky_far.width):
        scene.alpha_composite(sky_far, (ox, 0))
        scene.alpha_composite(sky_near, (ox, 80))

    # island grid: an irregular blob
    island = {}
    shape = [
        (0, 0), (1, 0), (2, 0), (3, 0),
        (0, 1), (1, 1), (2, 1), (3, 1), (4, 1),
        (0, 2), (1, 2), (2, 2), (3, 2), (4, 2),
        (1, 3), (2, 3), (3, 3), (4, 3),
        (2, 4), (3, 4),
    ]
    import random
    rnd = random.Random(7)
    for (gx, gy) in shape:
        # assign a tile type
        edge = (gx, gy) in [(0, 0), (0, 1), (0, 2), (4, 1), (4, 2), (4, 3), (3, 4)]
        if (gx, gy) == (2, 2):
            t = "natural_spring"
        elif edge:
            t = "cliff_edge"
        elif rnd.random() < 0.35:
            t = "rocky_plateau"
        else:
            t = "fertile_valley"
        island[(gx, gy)] = t

    tiles = {
        "fertile_valley": _slice_strip(_load("terrain/tile_fertile_valley.png"), 64),
        "rocky_plateau": _slice_strip(_load("terrain/tile_rocky_plateau.png"), 64),
        "cliff_edge": _slice_strip(_load("terrain/tile_cliff_edge.png"), 64),
        "natural_spring": _slice_strip(_load("terrain/tile_natural_spring.png"), 64),
    }

    # screen origin: tile diamond-top placement. For tile of size 64x80, the
    # diamond top vertex sits at local (32,0). grid_to_screen gives the diamond
    # CENTRE we want; we offset so the pasted tile's top-left lands right.
    # Place: pixel of diamond-top-centre = origin + iso. We paste tile so that
    # its (32,16) (diamond CENTER) aligns to computed screen point.
    ORX, ORY = 300, 150

    def tile_screen(gx, gy):
        # 64x32 diamond per cell: x half-step 32, y quarter-step 16
        sx = ORX + (gx - gy) * 32
        sy = ORY + (gx + gy) * 16
        return sx, sy

    # draw terrain back-to-front
    order = sorted(island.keys(), key=lambda c: (c[0] + c[1], c[0]))
    tops = {}  # remember diamond-top centre for placement of objects
    for (gx, gy) in order:
        ttype = island[(gx, gy)]
        frame = tiles[ttype][(gx * 7 + gy * 3) % len(tiles[ttype])]
        cx, cy = tile_screen(gx, gy)
        # paste so diamond CENTER (local 32,16) lands at (cx,cy)
        px = cx - 32
        py = cy - 16
        scene.alpha_composite(frame, (px, py))
        tops[(gx, gy)] = (cx, cy)

    # overlays: tilled soil on a few fertile cells
    tilled = _load("terrain/overlay_tilled.png")
    for cell in [(1, 1), (2, 1), (1, 2), (3, 2)]:
        if cell in tops:
            cx, cy = tops[cell]
            scene.alpha_composite(tilled, (cx - 32, cy - 16))

    # objects: (image_path, frame_w, frame_idx, grid_cell, foot_dy)
    def place(path, fw, frame_idx, cell, anchor="bottom"):
        if cell not in tops:
            return
        img = _slice_strip(_load(path), fw)[frame_idx]
        cx, cy = tops[cell]
        # bottom-center pivot sits on the diamond top-center (the cell's surface)
        px = cx - img.width // 2
        py = cy - img.height + 4  # +4 so feet sink slightly into tile
        scene.alpha_composite(img, (px, py))

    # sort objects back-to-front too
    objects = [
        ("structures/shelter.png", 128, 0, (0, 0)),
        ("structures/stone_mill.png", 128, 1, (4, 1)),
        ("structures/forge.png", 128, 2, (4, 3)),
        ("structures/rain_catcher.png", 64, 1, (3, 0)),
        ("structures/windbreak.png", 64, 0, (0, 2)),
        ("structures/drying_rack.png", 96, 1, (3, 3)),
        ("crops/crop_storm_wheat.png", 64, 3, (1, 1)),
        ("crops/crop_storm_wheat.png", 64, 3, (2, 1)),
        ("crops/crop_herb_plant.png", 64, 3, (1, 2)),
        ("crops/crop_sky_moss.png", 64, 3, (3, 2)),
        ("structures/crate.png", 48, 0, (2, 3)),
        ("structures/barrel.png", 48, 0, (1, 3)),
        ("debris/debris_2.png", 48, 0, (4, 2)),
        ("player/player_idle_s.png", 48, 0, (2, 0)),
    ]
    objects.sort(key=lambda o: (o[3][0] + o[3][1], o[3][0]))
    for path, fw, fi, cell in objects:
        place(path, fw, fi, cell)

    out = os.path.join(os.path.dirname(__file__), "preview_scene.png")
    scene.convert("RGB").save(out)
    print("wrote", out, scene.size)


def main():
    build_contact_sheet()
    build_scene()


if __name__ == "__main__":
    main()
