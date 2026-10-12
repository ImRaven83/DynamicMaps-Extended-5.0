"""Pre-render DynamicMaps Extended's Abstract SVG artwork to PNG for SPT 5.0.

DynamicMaps 2.0 (SPT 5.0 / IL2CPP) no longer renders SVG map layers at runtime. It loads
the PNG that sits beside each referenced SVG (same name, .png extension). This script
renders every bundled Abstract SVG to that sibling PNG.

Requires: python -m pip install cairosvg
Usage:    python tools/render-abstract-svgs.py [--force]
"""
import argparse
import pathlib
import sys

import cairosvg

ROOT = pathlib.Path(__file__).resolve().parent.parent
SOURCES = [
    'Maps/StyleAssets/Woods/Abstract',
    'Maps/StyleAssets/Streets/Abstract',
    'Maps/StyleAssets/Factory/Abstract',
    'Maps/Interchange_Backport/Abstract',
]
# Longest output side in pixels. Factory is a small map, so a smaller texture keeps the
# same on-screen density as the larger maps.
LONGEST_SIDE = {'Maps/StyleAssets/Factory/Abstract': 2048}
DEFAULT_LONGEST_SIDE = 4096


def svg_size(path):
    import xml.etree.ElementTree as ET
    root = ET.parse(path).getroot()
    view_box = root.get('viewBox')
    if view_box:
        _, _, w, h = (float(v) for v in view_box.replace(',', ' ').split())
        return w, h
    return float(root.get('width').rstrip('px')), float(root.get('height').rstrip('px'))


def multiple_of_four(value):
    return max(4, int(round(value / 4.0)) * 4)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--force', action='store_true', help='re-render existing PNGs')
    args = parser.parse_args()

    rendered = 0
    for folder in SOURCES:
        longest = LONGEST_SIDE.get(folder, DEFAULT_LONGEST_SIDE)
        for svg in sorted((ROOT / folder).glob('*.svg')):
            png = svg.with_suffix('.png')
            if png.exists() and not args.force:
                continue
            w, h = svg_size(svg)
            scale = longest / max(w, h)
            out_w, out_h = multiple_of_four(w * scale), multiple_of_four(h * scale)
            cairosvg.svg2png(url=str(svg), write_to=str(png), output_width=out_w, output_height=out_h)
            print(f'{svg.relative_to(ROOT)} -> {png.name} ({out_w}x{out_h})')
            rendered += 1
    print(f'Rendered {rendered} PNG(s).')
    return 0


if __name__ == '__main__':
    sys.exit(main())
