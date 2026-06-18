#!/usr/bin/env python3
"""Prepare carved_stair_face.png for the tier-boundary cliff (32×64 px, SW pivot).

The game keeps the normal terrain tile and swaps only the cliff parallelogram
(same slot as IsoTierFace). Input: isolated Gemini stair PNG with fake bg.

Output:
  Assets/Resources/Sprites/structures/carved_stair_face.png
  Assets/StreamingAssets/carved_stair_face.png
"""
from __future__ import annotations

import argparse
import os
import sys

try:
    from PIL import Image
    import numpy as np
except ImportError:
    print("Pillow + numpy required: pip install pillow numpy", file=sys.stderr)
    sys.exit(1)

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Resources", "Sprites", "structures", "carved_stair_face.png")
STREAM_OUT = os.path.join(ROOT, "Assets", "StreamingAssets", "carved_stair_face.png")
# Matches ProcGfx.IsoTierFace: halfW=32, H=16+TierFaceH(48)=64
FACE_W, FACE_H = 32, 64


def strip_fake_bg(im: Image.Image) -> Image.Image:
    a = np.array(im.convert("RGBA"), dtype=np.int16)
    r, g, b = a[:, :, 0], a[:, :, 1], a[:, :, 2]
    avg = (r + g + b) / 3.0
    spread = np.maximum(np.maximum(np.abs(r - g), np.abs(g - b)), np.abs(r - b))
    bg = (avg >= 215) & (spread <= 18)
    a[bg, 3] = 0
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def crop_opaque(im: Image.Image, pad: int = 2) -> Image.Image:
    sa = np.array(im)[:, :, 3]
    ys, xs = np.where(sa > 24)
    if len(xs) == 0:
        return im
    x0, x1 = max(0, xs.min() - pad), min(im.width - 1, xs.max() + pad)
    y0, y1 = max(0, ys.min() - pad), min(im.height - 1, ys.max() + pad)
    return im.crop((x0, y0, x1 + 1, y1 + 1))


def make_face(stair_src: Image.Image) -> Image.Image:
    stair = crop_opaque(strip_fake_bg(stair_src))
    face = Image.new("RGBA", (FACE_W, FACE_H), (0, 0, 0, 0))
    stair_fit = stair.resize((FACE_W, FACE_H), Image.Resampling.NEAREST)
    face.paste(stair_fit, (0, 0), stair_fit)
    return face


def save(im: Image.Image) -> None:
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    im.save(OUT)
    os.makedirs(os.path.dirname(STREAM_OUT), exist_ok=True)
    im.save(STREAM_OUT)
    print(f"wrote {OUT} ({FACE_W}x{FACE_H})")
    print(f"wrote {STREAM_OUT}")


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--from", dest="src", required=True)
    ap.add_argument("--preview", help="Optional enlarged preview path")
    args = ap.parse_args()
    im = make_face(Image.open(args.src))
    save(im)
    if args.preview:
        os.makedirs(os.path.dirname(args.preview) or ".", exist_ok=True)
        im.resize((128, 256), Image.Resampling.NEAREST).save(args.preview)
        print(f"preview {args.preview}")


if __name__ == "__main__":
    main()
