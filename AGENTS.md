# AssetDesk — Agent Constraints

Read this before touching code. It applies to every agent (Claude, Gemini/Antigravity, Copilot)
and to the human. The full design is in `docs/ARCHITECTURE.md`; this file is the short list of
rules that are easy to violate by accident.

## 1. Build in module order — one commit per module

This is a DeVry SIS250 course project. The git history is the deliverable as much as the code:
each module's work lands the week it is taught, so the log is dated proof of build-first.

| Module | Lands | Uses only |
|---|---|---|
| M1 | `Program` intake + warranty-age/cost output | I/O, types, arithmetic |
| M2 | `Program` loops over the CSV rows | decisions, loops |
| M3 | `TriageRules` (static pure functions) **and** `Inventory` — **fixed-size arrays + a count field. NOT `List<T>`.** Parallel arrays until `Asset` exists. | methods, arrays |
| M4 | *(confirm from zyBook — not yet known; do not assume)* | |
| M5 | `Asset`, `Incident`, `Technician` classes; constructor validation | classes |
| M6 | `abstract Asset` → `sealed Laptop/Desktop/Peripheral` | inheritance, polymorphism |
| M7 | `AssetCsvImporter`, `ImportResult`, `RejectedRow` — **never throws** | exceptions, file I/O |
| M8 | Integration + README + screenshots | course project |

**Do not build ahead.** If a later module's construct would make this week's code nicer,
write the simpler version now and refactor it when that module arrives. The refactor commit
is itself evidence (e.g. M3's static warranty math and parallel arrays move into `Asset` / `Asset[]` when classes arrive).

Commit prefix: `feat(M3): ...`, `refactor(M6): ...`, `docs(M8): ...`.

## 2. Hard technical constraints

- Target `net8.0`. `DateOnly` is confirmed and preferred over `DateTime` for dates.
- `Inventory` (M3) uses fixed-size arrays with `_assetCount` / `_incidentCount`. Parallel primitive arrays in M3; refactored to `Asset[]` / `Incident[]` when classes arrive (M5 in the July plan — confirm).
  No `List<T>`, no LINQ over collections until M8 unless the module explicitly covers it.
- `AssetCsvImporter` (M7) returns an `ImportResult`; every failure mode lands in `Rejected`
  with line number and reason. Acceptance test: `data/assets.malformed.csv` →
  **2 imported, 8 rejected, 0 exceptions.** Blank line is skipped silently (documented choice).
- Refresh policy per type is in `docs/ARCHITECTURE.md` §Subclass policies. Do not invent numbers.
- Sample data in `data/` is fixed. Do not edit it to make a rule pass.
- `Program.cs` must run from a clean clone with `dotnet run` and no arguments.

## 3. Verification before claiming done

- `dotnet build` with zero warnings, `dotnet run` exits 0.
- State the actual console output in the commit body or PR description, not a paraphrase.
- Do not report a file as existing, a test as passing, or a push as landed without having
  run the command in this session.

## 4. Reviewing another agent's work

Review the diff (`git log -p`) against `docs/ARCHITECTURE.md` and this file.
Never review from another agent's summary of what it did.
