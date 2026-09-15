// AssetDesk — console endpoint inventory and incident tracker.
// SIS250 course project. Entry point; module work lands here and under Domain/.
//
// Module 6: assets come in through AssetCsvImporter, which never throws — bad
// rows are reported and skipped, good rows still load. The program then runs
// the importer against the deliberately broken file and a missing file to
// prove the contract. Incidents are still parsed inline; they get the same
// treatment when the course project integrates everything.

using AssetDesk;
using AssetDesk.Domain;
using AssetDesk.Import;

Console.WriteLine("AssetDesk");
Console.WriteLine("Find the endpoints that are out of warranty or keep coming back — before they turn into tickets.");
Console.WriteLine();

// ---------- Load: CSV rows -> objects -> Inventory ----------

string dataDir = Path.Combine(AppContext.BaseDirectory, "data");
string[] incidentLines = File.ReadAllLines(Path.Combine(dataDir, "incidents.csv"));

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

for (int j = 1; j < incidentLines.Length; j++)
{
    string[] f = incidentLines[j].Split(',');
    inventory.Log(new Incident(
        incidentId: f[0], assetTag: f[1],
        openedOn: DateOnly.Parse(f[2]),
        closedOn: f[3] == "" ? null : DateOnly.Parse(f[3]),
        priority: Enum.Parse<IncidentPriority>(f[4]),
        category: Enum.Parse<IncidentCategory>(f[5]),
        techId: f[6], description: f[7]));
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

    Console.WriteLine($"{a.AssetTag,-10} {a.AssetType,-11} {months,7}  {warranty,-12}  {a.CurrentValue(asOf),8:F2}  {incidentCount,3}  {openIncidents,4}  {flag}");
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
    Console.WriteLine($"      cycle {a.RefreshCycleMonths,2}mo  age {a.MonthsInService(asOf),2}mo  open {openCount}  value {a.CurrentValue(asOf),7:F2}  refresh: {a.IsRefreshEligible(asOf, openCount)}");
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

ImportResult missing = AssetCsvImporter.Import(Path.Combine(dataDir, "does-not-exist.csv"));
Console.WriteLine($"  does-not-exist.csv:   {missing}");
for (int r = 0; r < missing.Rejected.Length; r++)
{
    Console.WriteLine($"    ! {missing.Rejected[r]}");
}

bool canaryPassed = malformed.Imported.Length == 2 && malformed.Rejected.Length == 8
                    && missing.Imported.Length == 0 && missing.Rejected.Length == 1;
Console.WriteLine(canaryPassed ? "  CANARY PASSED: 2 imported, 8 rejected, 0 exceptions." : "  CANARY FAILED.");
