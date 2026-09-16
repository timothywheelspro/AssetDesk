#!/usr/bin/env python3
"""Render slices of run-output.txt as terminal-styled SVGs. Output text is never
retyped: this reads the captured file so the screenshots are the real run."""
import html, pathlib, sys

HERE = pathlib.Path(__file__).parent
LINES = (HERE / "run-output.txt").read_text().splitlines()

def render(name, start, end, title):
    body = LINES[start - 1:end]
    lh, pad, fs = 19, 18, 13
    w = int(9.2 * max(len(l) for l in body)) + 2 * pad
    w = max(w, 640)
    h = 2 * pad + lh * (len(body) + 2)
    # Square canvas with the window centered vertically: qlmanage pads thumbnails
    # to a square and sips crops from the center, so this makes the crop exact.
    S = max(w, h); top = (S - h) // 2
    out = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{S}" height="{S}" viewBox="0 0 {S} {S}" font-family="SF Mono, Menlo, Consolas, monospace" font-size="{fs}" data-window="{w}x{h}">',
           f'<g transform="translate(0,{top})">',
           f'<rect width="{w}" height="{h}" rx="10" fill="#1e1e1e"/>',
           '<circle cx="20" cy="18" r="6" fill="#ff5f56"/><circle cx="40" cy="18" r="6" fill="#ffbd2e"/><circle cx="60" cy="18" r="6" fill="#27c93f"/>',
           f'<text x="{w/2}" y="23" fill="#9a9a9a" text-anchor="middle" font-size="12">{html.escape(title)}</text>']
    y = pad + lh * 2
    for line in body:
        color = "#d4d4d4"
        s = line.rstrip()
        if s.startswith("  !") or s.startswith("    !"): color = "#f48771"
        elif s.startswith("    +"): color = "#89d185"
        elif "CANARY PASSED" in s: color = "#89d185"
        elif "REFRESH + REPEAT" in s: color = "#e5c07b"
        elif s.endswith("REFRESH"): color = "#e5c07b"
        elif s.startswith("TAG ") or s.startswith("----"): color = "#9cdcfe"
        elif s.startswith("AssetDesk") or s.startswith("Out of") or s.startswith("Repeat") or s.startswith("Refresh cand") or s.startswith("Most urgent") or s.startswith("Importer canary") or s.startswith("Refresh policy") or s.startswith("Open incidents by"): color = "#9cdcfe"
        out.append(f'<text x="{pad}" y="{y}" fill="{color}" xml:space="preserve">{html.escape(s)}</text>')
        y += lh
    out.append("</g></svg>")
    (HERE / f"{name}.svg").write_text("\n".join(out))
    print(f"{name}.svg  {len(body)} lines")

render("01-roster",   1, 47, "dotnet run — roster with warranty status, value, and refresh flags")
render("02-triage",  48, 69, "dotnet run — open incidents by priority, lookup, polymorphic refresh policy")
render("03-importer", 71, 96, "dotnet run — importer canary: malformed files and a missing file, 0 exceptions")
