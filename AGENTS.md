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
| M4 | `Domain/Asset`, `Incident`, `Technician` classes + enums; constructor validation; `Inventory` becomes an instance class holding `Asset[]` / `Incident[]` | classes, encapsulation |
| M5 | `abstract Asset` → `sealed Laptop` / `Desktop` / `Peripheral`; policy moves from `TriageRules` switches into overrides | inheritance, abstract members, polymorphism |
| M6 | `Import/AssetCsvImporter`, `IncidentCsvImporter` — **never throws**; every failure is a `RejectedRow` with line + reason. `IDepreciable` on `Laptop`/`Desktop` only | exceptions, file I/O, interfaces |
| M7 | Careers module — no code | |
| M8 | Course completion — README, screenshots, integration | course project |
| M5 | `Asset`, `Incident`, `Technician` classes; constructor validation | classes |
| M6 | `abstract Asset` → `sealed Laptop/Desktop/Peripheral` | inheritance, polymorphism |
| M7 | `AssetCsvImporter`, `ImportResult`, `RejectedRow` — **never throws** | exceptions, file I/O |
| M8 | Integration + README + screenshots | course project |

**Do not build ahead.** If a later module's construct would make this week's code nicer,
write the simpler version now and refactor it when that module arrives. The refactor commit
is itself evidence (M3 static math → M4 `Asset` members → M5 subclass overrides; each step is a dated commit).

Commit prefix: `feat(M3): ...`, `refactor(M6): ...`, `docs(M8): ...`.

## 2. Hard technical constraints

- Target `net8.0`. `DateOnly` is confirmed and preferred over `DateTime` for dates.
- `Inventory` (M3) uses fixed-size arrays with `_assetCount` / `_incidentCount`. Parallel primitive arrays in M3; refactored to `Asset[]` / `Incident[]` in M4 (classes, confirmed 2026-09-15).
  No `List<T>`, no LINQ over collections until M8 unless the module explicitly covers it.
- `AssetCsvImporter` (M6) returns an `ImportResult`; every failure mode lands in `Rejected`
  with line number and reason. Acceptance test: `data/assets.malformed.csv` →
  **2 imported, 8 rejected, 0 exceptions.** Blank line is skipped silently (documented choice).
- Refresh policy per type is in `docs/ARCHITECTURE.md` §Subclass policies. Do not invent numbers.
- `IDepreciable` is implemented by `Laptop` and `Desktop` only. `Peripheral` has no `CurrentValue`;
  callers ask `a is IDepreciable` — never add a zero-returning stub to satisfy a caller.
- Sample data in `data/` is fixed. Do not edit it to make a rule pass.
- `Program.cs` must run from a clean clone with `dotnet run` and no arguments.

## 3. Verification before claiming done

- `dotnet build` with zero warnings, `dotnet run` exits 0.
- State the actual console output in the commit body or PR description, not a paraphrase.
- Do not report a file as existing, a test as passing, or a push as landed without having
  run the command in this session.
- Before any claim about what a diff did or didn't touch goes into a commit body, a post,
  or a review, run `git show --stat <sha>` (and `git diff --numstat` for the file in
  question) and state what it says. "Didn't change a line" is a number, not an impression.

## 4. Reviewing another agent's work

Review the diff (`git log -p`) against `docs/ARCHITECTURE.md` and this file.
Never review from another agent's summary of what it did.
