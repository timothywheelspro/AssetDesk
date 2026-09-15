# Sample data

Ships with the repo so a clean clone runs with no setup. Tags, models, and
dates are realistic but fictional.

## assets.csv — 30 rows

| Column | Type | Notes |
|---|---|---|
| `asset_tag` | string | Primary key. `HR-L-` laptops, `HR-D-` desktops, `HR-P-` peripherals. |
| `type` | `Laptop` / `Desktop` / `Peripheral` | Selects the subclass on import. |
| `serial` | string | May be blank for peripherals. |
| `model` | string | |
| `purchase_date` | `yyyy-MM-dd` | |
| `purchase_cost` | decimal | USD. |
| `warranty_months` | int | |
| `assigned_to` | string | Blank means unassigned / in stock. |
| `status` | `InService` / `InRepair` / `Retired` / `Disposed` | |

Deliberately mixed so the triage rules have something to find: several assets
are out of warranty as of late 2026, a few are unassigned, two are retired or
disposed, and `HR-L-0142`, `HR-L-0159`, `HR-D-0031`, and `HR-P-0230` each have
repeat incidents.

## incidents.csv — 18 rows

| Column | Type | Notes |
|---|---|---|
| `incident_id` | string | |
| `asset_tag` | string | Must match an `assets.csv` tag. |
| `opened_on` | `yyyy-MM-dd` | |
| `closed_on` | `yyyy-MM-dd` | Blank means still open. |
| `priority` | `Low` / `Medium` / `High` / `Critical` | |
| `category` | `Hardware` / `Software` / `Network` / `Access` | |
| `tech_id` | string | Blank means unassigned. |
| `description` | string | |

## technicians.csv — 3 rows

`tech_id`, `name`, `tier`.

## assets.malformed.csv — the file the importer has to survive

Same schema as `assets.csv`, but every row after the first is broken in a
different way. Expected result: **2 imported, 8 rejected**, no exception.

| Row | Problem |
|---|---|
| 2 | Valid — control row |
| 3 | `purchase_date` is not a date |
| 4 | Unknown `type` (`Tablet`) |
| 5 | Negative `purchase_cost` |
| 6 | `warranty_months` is not an integer |
| 7 | Blank `asset_tag` |
| 8 | Wrong column count |
| 9 | Duplicate `asset_tag` (same as row 2) |
| 10 | Unknown `status` |
| 11 | Blank line |
| 12 | Valid — proves good rows after bad ones still import |

Whether the blank line counts as a rejection or is skipped silently is a
design choice; pick one and document it.

## incidents.malformed.csv — the file the incident importer has to survive

Same schema as `incidents.csv`. Expected result: **2 imported, 8 rejected**, no exception.

| Row | Problem |
|---|---|
| 2 | Valid — control row |
| 3 | `opened_on` is not a date |
| 4 | `closed_on` is before `opened_on` |
| 5 | Unknown `priority` (`Urgent`) |
| 6 | Unknown `category` (`Facilities`) |
| 7 | Blank `incident_id` |
| 8 | Duplicate `incident_id` (same as row 2) |
| 9 | `asset_tag` not in the inventory (`HR-X-9999`) |
| 10 | Too few columns |
| 11 | Valid — description contains commas, which must be rejoined, not treated as extra columns |
| 12 | Blank line — skipped silently |
