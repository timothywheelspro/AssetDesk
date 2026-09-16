# Status — read this before touching the code

_Last updated 2026-09-15 (Fall session, week 3). Full session narrative lives in Notion:
"Session summary 2026-09-15" under the SIS250 course page._

## Where the code is

`main` at `1127ebe`. Modules 1–6 implemented, one dated commit per module, each reviewed
against `AGENTS.md` + `docs/ARCHITECTURE.md` by a second agent working from the diff.
README and screenshots done. `dotnet build -warnaserror` clean; last output line is
`CANARY PASSED: assets 2/8, incidents 2/8, missing file 0/1, 0 exceptions.`

| Module | Canvas due | Commit(s) |
|---|---|---|
| M3 methods + arrays | Sep 21 | `c7cadf7`, `77ae9d4` |
| M4 classes | Sep 28 | `54ddbb8` |
| M5 inheritance | Oct 5 | `33dfe26` |
| M6 files, exceptions, interfaces | Oct 12 | `4f99bc8`, `ab417d8`, `1127ebe` |
| M7 careers (no code) | Oct 19 | — |
| M8 course completion | — | README `3d63d2f` |

## Invariants the reviewer checks

- No `List<T>`, no LINQ. Fixed arrays + counts, count-then-fill filters.
- `IDepreciable` on `Laptop`/`Desktop` only. `Peripheral` has no `CurrentValue`; callers ask
  `a is IDepreciable`. Never add a zero stub.
- Importers never throw. `data/*.malformed.csv` each yield 2 imported / 8 rejected.
- Same counts at every commit: 17 out of warranty, 4 repeat offenders, 10 refresh candidates.
- Every commit body carries real `dotnet run` output and `git show --stat` (run first).

## What would restart work

1. Module 6 "AI Coding and OOP" assignment prompt (75 pts) — code is ready; likely a write-up.
2. Optional GUI project (unlocks Oct 4) — `Inventory` and the importers are UI-agnostic.
3. M8 submission format.
4. Nothing else. Do not build ahead of a prompt.

## Regenerating screenshots after any output change

```bash
dotnet run > docs/screenshots/run-output.txt
python3 docs/screenshots/render.py          # SVGs from the capture
# then per file: qlmanage -t -s 2400 -o docs/screenshots <f>.svg; sips -c <h> <w> <f>.png
```
(`qlmanage` pads to a square; the SVG centers the window so `sips`'s centered crop is exact.)

## Housekeeping open

- Set the GitHub About field (tagline) + topics — browser only.
- `AGENTS.md` labels the README commit M7; Canvas says the project completes in M8.
