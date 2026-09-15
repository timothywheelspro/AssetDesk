// AssetDesk — console endpoint inventory and incident tracker.
// SIS250 course project. Entry point; module work lands here and under Domain/.
//
// Module 3: load the sample rosters into Inventory (fixed-size arrays), run the
// TriageRules over every row, and print the two reports the README promises.
// Parsing is deliberately naive here — the clean sample files are the only
// input until the resilient importer arrives in Module 7.

using AssetDesk;

Console.WriteLine("AssetDesk");
Console.WriteLine("Find the endpoints that are out of warranty or keep coming back — before they turn into tickets.");
Console.WriteLine();

// ---------- Load: CSV rows -> Inventory tables ----------

string dataDir = Path.Combine(AppContext.BaseDirectory, "data");
string[] assetLines = File.ReadAllLines(Path.Combine(dataDir, "assets.csv"));
string[] incidentLines = File.ReadAllLines(Path.Combine(dataDir, "incidents.csv"));

for (int i = 1; i < assetLines.Length; i++)   // start at 1: skip the header row
{
    string[] f = assetLines[i].Split(',');
    Inventory.Add(f[0], f[1], f[2], f[3], DateOnly.Parse(f[4]), decimal.Parse(f[5]), int.Parse(f[6]), f[7], f[8]);
}

for (int j = 1; j < incidentLines.Length; j++)
{
    string[] f = incidentLines[j].Split(',');
    Inventory.Log(f[0], f[1], DateOnly.Parse(f[2]), f[3], f[4], f[5], f[6], f[7]);
}

DateOnly asOf = DateOnly.FromDateTime(DateTime.Today);
const int RepeatThreshold = 2;

Console.WriteLine($"As of {asOf:yyyy-MM-dd}  |  {Inventory.AssetCount} assets, {Inventory.IncidentCount} incidents");
Console.WriteLine();

// ---------- Report 1: roster with warranty status and refresh flag ----------

Console.WriteLine("TAG        TYPE        AGE(mo)  WARRANTY      VALUE     INC  OPEN  FLAG");
Console.WriteLine("---------- ----------- -------  ------------  --------  ---  ----  -----------------");

for (int i = 0; i < Inventory.AssetCount; i++)
{
    string tag = Inventory.TagAt(i);
    string type = Inventory.TypeAt(i);
    string status = Inventory.StatusAt(i);
    DateOnly purchaseDate = Inventory.PurchaseDateAt(i);
    int warrantyMonths = Inventory.WarrantyMonthsAt(i);

    int months = TriageRules.MonthsInService(purchaseDate, asOf);
    bool outOfWarranty = TriageRules.IsOutOfWarranty(purchaseDate, warrantyMonths, asOf);
    decimal value = TriageRules.CurrentValue(Inventory.PurchaseCostAt(i), months, TriageRules.RefreshCycleMonths(type));
    int incidentCount = Inventory.IncidentCountFor(tag);
    int openIncidents = Inventory.OpenIncidentCount(tag);
    bool repeat = TriageRules.IsRepeatOffender(incidentCount, RepeatThreshold);
    bool refresh = TriageRules.IsActive(status)
                   && TriageRules.IsRefreshEligible(type, status, months, outOfWarranty, openIncidents);

    string warranty = outOfWarranty
        ? "EXPIRED"
        : "to " + TriageRules.WarrantyExpires(purchaseDate, warrantyMonths).ToString("yyyy-MM");

    string flag = "";
    if (!TriageRules.IsActive(status)) flag = status.ToLower();
    else if (refresh && repeat)        flag = "REFRESH + REPEAT";
    else if (refresh)                  flag = "REFRESH";
    else if (repeat)                   flag = "repeat";

    Console.WriteLine($"{tag,-10} {type,-11} {months,7}  {warranty,-12}  {value,8:F2}  {incidentCount,3}  {openIncidents,4}  {flag}");
}

Console.WriteLine();

// ---------- Report 2: the two blind spots, answered by Inventory's filters ----------

string[] outOfWarrantyTags = Inventory.OutOfWarranty(asOf);
string[] repeatTags = Inventory.RepeatOffenders(RepeatThreshold);
string[] refreshTags = Inventory.RefreshCandidates(asOf);

Console.WriteLine($"Out of warranty (active):   {outOfWarrantyTags.Length}");
Console.WriteLine($"Repeat offenders (>= {RepeatThreshold}):    {repeatTags.Length}   {string.Join(", ", repeatTags)}");
Console.WriteLine($"Refresh candidates:         {refreshTags.Length}   {string.Join(", ", refreshTags)}");
Console.WriteLine();
Console.Write(Inventory.SummaryByType());
Console.WriteLine();

// Highest-priority open incident, found with a running max over the incident file.
string topIncident = "";
int topRank = 0;

for (int j = 1; j < incidentLines.Length; j++)
{
    string[] inc = incidentLines[j].Split(',');
    int rank = TriageRules.PriorityRank(inc[4]);

    if (inc[3] == "" && rank > topRank)
    {
        topRank = rank;
        topIncident = $"{inc[0]} on {inc[1]} ({inc[4]}): {inc[7]}";
    }
}

Console.WriteLine($"Most urgent open incident:  {topIncident}");

// Search demo: the linear search answers a tag lookup in one call.
int row = Inventory.FindByTag("HR-L-0159");
Console.WriteLine(row >= 0
    ? $"Lookup HR-L-0159:           row {row}, {Inventory.ModelAt(row)}, assigned to {Inventory.AssignedToAt(row)}"
    : "Lookup HR-L-0159:           not found");
