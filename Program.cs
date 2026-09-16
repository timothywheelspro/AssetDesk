// AssetDesk — console endpoint inventory and incident tracker.
// SIS250 course project. Entry point; module work lands here and under Domain/.
//
// Module 6: assets come in through AssetCsvImporter, which never throws — bad
// rows are reported and skipped, good rows still load. The program then runs
// the importer against the deliberately broken file and a missing file to
// prove the contract. Incidents come in the same way, cross-checked against
// the inventory so a ticket can't point at a machine we don't track.

using AssetDesk;
using AssetDesk.Domain;
using AssetDesk.Import;

Console.WriteLine("AssetDesk");
Console.WriteLine("Find the endpoints that are out of warranty or keep coming back — before they turn into tickets.");
Console.WriteLine();

// ---------- Load: CSV rows -> objects -> Inventory ----------

string dataDir = Path.Combine(AppContext.BaseDirectory, "data");
Inventory inventory = new Inventory();

ImportResult assets = AssetCsvImporter.Import(Path.Combine(dataDir, "assets.csv"));
for (int i = 0; i < assets.Imported.Length; i++)
{
    inventory.Add(assets.Imported[i]);
}
Console.WriteLine($"assets.csv: {assets}");
for (int r = 0; r < assets.Rejected.Length; r++)
{
    Console.WriteLine($"  ! {assets.Rejected[r]}");
}

IncidentImportResult incidents = IncidentCsvImporter.Import(Path.Combine(dataDir, "incidents.csv"), inventory);
for (int j = 0; j < incidents.Imported.Length; j++)
{
    inventory.Log(incidents.Imported[j]);
}
Console.WriteLine($"incidents.csv: {incidents}");
for (int r = 0; r < incidents.Rejected.Length; r++)
{
    Console.WriteLine($"  ! {incidents.Rejected[r]}");
}

DateOnly asOf = DateOnly.FromDateTime(DateTime.Today);
const int RepeatThreshold = 2;

Console.WriteLine($"As of {asOf:yyyy-MM-dd}  |  {inventory.AssetCount} assets, {inventory.IncidentCount} incidents");
Console.WriteLine();

// ---------- Report 1: roster with warranty status and refresh flag ----------

Console.WriteLine("TAG        TYPE        AGE(mo)  WARRANTY      VALUE     INC  OPEN  FLAG");
Console.WriteLine("---------- ----------- -------  ------------  --------  ---  ----  -----------------");

for (int i = 0; i < inventory.AssetCount; i++)
{
    Asset a = inventory.AssetAt(i);

    int months = a.MonthsInService(asOf);
    bool outOfWarranty = a.IsOutOfWarranty(asOf);
    int incidentCount = inventory.IncidentCountFor(a.AssetTag);
    int openIncidents = inventory.OpenIncidentCount(a.AssetTag);
    bool repeat = TriageRules.IsRepeatOffender(incidentCount, RepeatThreshold);
    bool refresh = a.IsRefreshEligible(asOf, openIncidents);

    string warranty = outOfWarranty ? "EXPIRED" : "to " + a.WarrantyExpires.ToString("yyyy-MM");

    string flag = "";
    if (!a.IsActive)             flag = a.Status.ToString().ToLower();
    else if (refresh && repeat)  flag = "REFRESH + REPEAT";
    else if (refresh)            flag = "REFRESH";
    else if (repeat)             flag = "repeat";

    // Only assets that CAN depreciate are asked what they're worth. The roster
    // loop never names a subclass; it asks for the capability.
    string value = a is IDepreciable d ? d.CurrentValue(asOf).ToString("F2") : "expensed";

    Console.WriteLine($"{a.AssetTag,-10} {a.AssetType,-11} {months,7}  {warranty,-12}  {value,8}  {incidentCount,3}  {openIncidents,4}  {flag}");
}

Console.WriteLine();

// ---------- Report 2: the two blind spots ----------

Asset[] outOfWarrantyAssets = inventory.OutOfWarranty(asOf);
Asset[] repeatAssets = inventory.RepeatOffenders(RepeatThreshold);
Asset[] refreshAssets = inventory.RefreshCandidates(asOf);

Console.WriteLine($"Out of warranty (active):   {outOfWarrantyAssets.Length}");
Console.WriteLine($"Repeat offenders (>= {RepeatThreshold}):    {repeatAssets.Length}   {Tags(repeatAssets)}");
Console.WriteLine($"Refresh candidates:         {refreshAssets.Length}   {Tags(refreshAssets)}");
Console.WriteLine();
Console.Write(inventory.SummaryByType());
Console.WriteLine();

// Open incidents, most urgent first.
Incident[] open = TriageRules.SortByPriority(inventory.OpenIncidents());
Console.WriteLine($"Most urgent open incident:  {(open.Length > 0 ? open[0].ToString() : "(none)")}");
Console.WriteLine($"Open incidents by priority: {open.Length}");
for (int j = 0; j < open.Length; j++)
{
    Console.WriteLine($"  {open[j].Priority,-9} {open[j].IncidentId}  {open[j].AssetTag}  {open[j].DaysOpen(asOf),3}d  {open[j].Description}");
}
Console.WriteLine();

// Search: the linear search hands back the object, not a row number.
Asset? found = inventory.FindByTag("HR-L-0159");
Console.WriteLine(found != null
    ? $"Lookup HR-L-0159:           {found.Model}, assigned to {found.AssignedTo}, {found.Status}"
    : "Lookup HR-L-0159:           not found");
Console.WriteLine();

// Polymorphism check: one loop, one call, three different policies answering.
Console.WriteLine("Refresh policy by type (one call, three overrides):");
string[] sampleTags = { "HR-L-0171", "HR-D-0031", "HR-P-0230" };
for (int i = 0; i < sampleTags.Length; i++)
{
    Asset? a = inventory.FindByTag(sampleTags[i]);
    if (a == null) continue;
    int openCount = inventory.OpenIncidentCount(a.AssetTag);
    Console.WriteLine($"  {a.ToRosterLine()}");
    string worth = a is IDepreciable dep ? $"value {dep.CurrentValue(asOf),7:F2}" : "value expensed";
    Console.WriteLine($"      cycle {a.RefreshCycleMonths,2}mo  age {a.MonthsInService(asOf),2}mo  open {openCount}  {worth}  refresh: {a.IsRefreshEligible(asOf, openCount)}");
}

// Local helper: comma-joined tags from an Asset[].
static string Tags(Asset[] assets)
{
    string s = "";
    for (int i = 0; i < assets.Length; i++) s += (i == 0 ? "" : ", ") + assets[i].AssetTag;
    return s;
}

// ---------- Module 6 canary: the importer must survive the broken file and a missing file ----------

Console.WriteLine();
Console.WriteLine("Importer canary (must not throw):");

ImportResult malformed = AssetCsvImporter.Import(Path.Combine(dataDir, "assets.malformed.csv"));
Console.WriteLine($"  assets.malformed.csv: {malformed}");
for (int r = 0; r < malformed.Rejected.Length; r++)
{
    Console.WriteLine($"    ! {malformed.Rejected[r]}");
}
for (int i = 0; i < malformed.Imported.Length; i++)
{
    Console.WriteLine($"    + {malformed.Imported[i].AssetTag} imported");
}

IncidentImportResult malformedIncidents = IncidentCsvImporter.Import(Path.Combine(dataDir, "incidents.malformed.csv"), inventory);
Console.WriteLine($"  incidents.malformed.csv: {malformedIncidents}");
for (int r = 0; r < malformedIncidents.Rejected.Length; r++)
{
    Console.WriteLine($"    ! {malformedIncidents.Rejected[r]}");
}
for (int j = 0; j < malformedIncidents.Imported.Length; j++)
{
    Console.WriteLine($"    + {malformedIncidents.Imported[j].IncidentId} imported: {malformedIncidents.Imported[j].Description}");
}

ImportResult missing = AssetCsvImporter.Import(Path.Combine(dataDir, "does-not-exist.csv"));
Console.WriteLine($"  does-not-exist.csv:   {missing}");
for (int r = 0; r < missing.Rejected.Length; r++)
{
    Console.WriteLine($"    ! {missing.Rejected[r]}");
}

bool canaryPassed = malformed.Imported.Length == 2 && malformed.Rejected.Length == 8
                    && malformedIncidents.Imported.Length == 2 && malformedIncidents.Rejected.Length == 8
                    && missing.Imported.Length == 0 && missing.Rejected.Length == 1;
Console.WriteLine(canaryPassed
    ? "  CANARY PASSED: assets 2/8, incidents 2/8, missing file 0/1, 0 exceptions."
    : "  CANARY FAILED.");
