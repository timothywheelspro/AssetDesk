// AssetDesk — console endpoint inventory and incident tracker.
// SIS250 course project. Entry point; module work lands here and under Domain/.
//
// Module 3: read the sample rosters, run every asset through the TriageRules
// functions, and print the two reports the README promises. Parsing is
// deliberately naive here — the clean sample files are the only input until the
// resilient importer arrives in Module 7.

using AssetDesk;

Console.WriteLine("AssetDesk");
Console.WriteLine("Find the endpoints that are out of warranty or keep coming back — before they turn into tickets.");
Console.WriteLine();

string dataDir = Path.Combine(AppContext.BaseDirectory, "data");
string[] assetLines = File.ReadAllLines(Path.Combine(dataDir, "assets.csv"));
string[] incidentLines = File.ReadAllLines(Path.Combine(dataDir, "incidents.csv"));

DateOnly asOf = DateOnly.FromDateTime(DateTime.Today);
const int RepeatThreshold = 2;

Console.WriteLine($"As of {asOf:yyyy-MM-dd}  |  {assetLines.Length - 1} assets, {incidentLines.Length - 1} incidents");
Console.WriteLine();

// ---------- Report 1: roster with warranty status and refresh flag ----------

Console.WriteLine("TAG        TYPE        AGE(mo)  WARRANTY      VALUE     INC  OPEN  FLAG");
Console.WriteLine("---------- ----------- -------  ------------  --------  ---  ----  -----------------");

int outOfWarrantyCount = 0;
int repeatOffenderCount = 0;
int refreshCount = 0;

for (int i = 1; i < assetLines.Length; i++)   // start at 1: skip the header row
{
    string[] f = assetLines[i].Split(',');

    string tag = f[0];
    string type = f[1];
    DateOnly purchaseDate = DateOnly.Parse(f[4]);
    decimal purchaseCost = decimal.Parse(f[5]);
    int warrantyMonths = int.Parse(f[6]);
    string status = f[8];

    // Count this asset's incidents by scanning the incident file (M2 nested loop;
    // Inventory will index this properly in M4).
    int incidentCount = 0;
    int openIncidents = 0;

    for (int j = 1; j < incidentLines.Length; j++)
    {
        string[] inc = incidentLines[j].Split(',');

        if (inc[1] == tag)
        {
            incidentCount++;

            if (inc[3] == "")   // blank closed_on means still open
            {
                openIncidents++;
            }
        }
    }

    int months = TriageRules.MonthsInService(purchaseDate, asOf);
    bool outOfWarranty = TriageRules.IsOutOfWarranty(purchaseDate, warrantyMonths, asOf);
    decimal value = TriageRules.CurrentValue(purchaseCost, months, TriageRules.RefreshCycleMonths(type));
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

    if (outOfWarranty && TriageRules.IsActive(status)) outOfWarrantyCount++;
    if (repeat) repeatOffenderCount++;
    if (refresh) refreshCount++;

    Console.WriteLine($"{tag,-10} {type,-11} {months,7}  {warranty,-12}  {value,8:F2}  {incidentCount,3}  {openIncidents,4}  {flag}");
}

Console.WriteLine();

// ---------- Report 2: the two blind spots, summarised ----------

Console.WriteLine($"Out of warranty (active):   {outOfWarrantyCount}");
Console.WriteLine($"Repeat offenders (>= {RepeatThreshold}):    {repeatOffenderCount}");
Console.WriteLine($"Refresh candidates:         {refreshCount}");
Console.WriteLine();

// Highest-priority open incident, found with a running max (M2 loop + M3 function).
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
