// AssetDesk — console endpoint inventory and incident tracker.
// SIS250 course project. Entry point; module work lands here and under Domain/.
//
// Module 5: the `type` column picks a subclass (Laptop / Desktop / Peripheral)
// in CreateAsset, and everything downstream works through the abstract Asset
// contract — the roster loop never asks what kind of asset it is holding.
// Parsing is still deliberately naive — the clean sample files are the only
// input until the resilient importer arrives.

using AssetDesk;
using AssetDesk.Domain;

Console.WriteLine("AssetDesk");
Console.WriteLine("Find the endpoints that are out of warranty or keep coming back — before they turn into tickets.");
Console.WriteLine();

// ---------- Load: CSV rows -> objects -> Inventory ----------

string dataDir = Path.Combine(AppContext.BaseDirectory, "data");
string[] assetLines = File.ReadAllLines(Path.Combine(dataDir, "assets.csv"));
string[] incidentLines = File.ReadAllLines(Path.Combine(dataDir, "incidents.csv"));

Inventory inventory = new Inventory();

for (int i = 1; i < assetLines.Length; i++)   // start at 1: skip the header row
{
    string[] f = assetLines[i].Split(',');
    inventory.Add(CreateAsset(
        type: f[1], tag: f[0], serial: f[2], model: f[3],
        purchaseDate: DateOnly.Parse(f[4]), cost: decimal.Parse(f[5]), warrantyMonths: int.Parse(f[6]),
        assignedTo: f[7], status: Enum.Parse<AssetStatus>(f[8])));
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

// Factory: the `type` column decides which subclass is built. This is the only
// place in the program that names Laptop, Desktop, or Peripheral.
static Asset CreateAsset(
    string type, string tag, string serial, string model,
    DateOnly purchaseDate, decimal cost, int warrantyMonths,
    string assignedTo, AssetStatus status)
{
    switch (type)
    {
        case "Laptop":     return new Laptop(tag, serial, model, purchaseDate, cost, warrantyMonths, assignedTo, status);
        case "Desktop":    return new Desktop(tag, serial, model, purchaseDate, cost, warrantyMonths, assignedTo, status);
        case "Peripheral": return new Peripheral(tag, serial, model, purchaseDate, cost, warrantyMonths, assignedTo, status);
        default:           throw new ArgumentException($"Unknown asset type '{type}' for {tag}.", nameof(type));
    }
}

// Local helper: comma-joined tags from an Asset[].
static string Tags(Asset[] assets)
{
    string s = "";
    for (int i = 0; i < assets.Length; i++) s += (i == 0 ? "" : ", ") + assets[i].AssetTag;
    return s;
}
