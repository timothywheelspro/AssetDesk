// Inventory.cs — Module 3: the in-memory roster as fixed-size arrays with a count.
//
// This is the extractable array utility. It stores every asset and incident
// column in its own array (parallel arrays) with one count per table, and
// exposes search, filter, and summary routines over them. No List<T>, no LINQ.
//
// Parallel arrays are the pre-class way to hold a record; when Asset and
// Incident become classes these collapse into Asset[] and Incident[] and every
// public method below keeps its name and meaning.

namespace AssetDesk;

public static class Inventory
{
    // ---------- Capacity: fixed at compile time; Add/Log refuse when full ----------

    public const int MaxAssets = 64;
    public const int MaxIncidents = 256;

    // ---------- Asset table (one row per asset, indexed 0.._assetCount-1) ----------

    private static readonly string[]   _tags           = new string[MaxAssets];
    private static readonly string[]   _types          = new string[MaxAssets];
    private static readonly string[]   _serials        = new string[MaxAssets];
    private static readonly string[]   _models         = new string[MaxAssets];
    private static readonly DateOnly[] _purchaseDates  = new DateOnly[MaxAssets];
    private static readonly decimal[]  _purchaseCosts  = new decimal[MaxAssets];
    private static readonly int[]      _warrantyMonths = new int[MaxAssets];
    private static readonly string[]   _assignedTo     = new string[MaxAssets];
    private static readonly string[]   _statuses       = new string[MaxAssets];
    private static int _assetCount = 0;

    // ---------- Incident table ----------

    private static readonly string[]   _incidentIds    = new string[MaxIncidents];
    private static readonly string[]   _incidentTags   = new string[MaxIncidents];
    private static readonly DateOnly[] _openedOn       = new DateOnly[MaxIncidents];
    private static readonly string[]   _closedOn       = new string[MaxIncidents];   // "" = still open
    private static readonly string[]   _priorities     = new string[MaxIncidents];
    private static readonly string[]   _categories     = new string[MaxIncidents];
    private static readonly string[]   _techIds        = new string[MaxIncidents];
    private static readonly string[]   _descriptions   = new string[MaxIncidents];
    private static int _incidentCount = 0;

    public static int AssetCount => _assetCount;
    public static int IncidentCount => _incidentCount;

    // ---------- Loading ----------

    /// <summary>Append one asset. Returns false (and stores nothing) when the table is full.</summary>
    public static bool Add(
        string tag, string type, string serial, string model,
        DateOnly purchaseDate, decimal purchaseCost, int warrantyMonths,
        string assignedTo, string status)
    {
        if (_assetCount >= MaxAssets)
        {
            return false;
        }

        int i = _assetCount;
        _tags[i] = tag;
        _types[i] = type;
        _serials[i] = serial;
        _models[i] = model;
        _purchaseDates[i] = purchaseDate;
        _purchaseCosts[i] = purchaseCost;
        _warrantyMonths[i] = warrantyMonths;
        _assignedTo[i] = assignedTo;
        _statuses[i] = status;
        _assetCount++;
        return true;
    }

    /// <summary>Append one incident. Returns false when the table is full.</summary>
    public static bool Log(
        string incidentId, string assetTag, DateOnly openedOn, string closedOn,
        string priority, string category, string techId, string description)
    {
        if (_incidentCount >= MaxIncidents)
        {
            return false;
        }

        int i = _incidentCount;
        _incidentIds[i] = incidentId;
        _incidentTags[i] = assetTag;
        _openedOn[i] = openedOn;
        _closedOn[i] = closedOn;
        _priorities[i] = priority;
        _categories[i] = category;
        _techIds[i] = techId;
        _descriptions[i] = description;
        _incidentCount++;
        return true;
    }

    // ---------- Row access (by index, for callers that iterate the table) ----------

    public static string   TagAt(int i)            => _tags[i];
    public static string   TypeAt(int i)           => _types[i];
    public static string   ModelAt(int i)          => _models[i];
    public static DateOnly PurchaseDateAt(int i)   => _purchaseDates[i];
    public static decimal  PurchaseCostAt(int i)   => _purchaseCosts[i];
    public static int      WarrantyMonthsAt(int i) => _warrantyMonths[i];
    public static string   AssignedToAt(int i)     => _assignedTo[i];
    public static string   StatusAt(int i)         => _statuses[i];

    // ---------- Search ----------

    /// <summary>Linear search by tag. Returns the row index, or -1 when not found.</summary>
    public static int FindByTag(string tag)
    {
        for (int i = 0; i < _assetCount; i++)
        {
            if (_tags[i] == tag)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>How many incidents (open or closed) reference this tag.</summary>
    public static int IncidentCountFor(string tag)
    {
        int n = 0;

        for (int j = 0; j < _incidentCount; j++)
        {
            if (_incidentTags[j] == tag)
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>How many incidents for this tag are still open (blank closed_on).</summary>
    public static int OpenIncidentCount(string tag)
    {
        int n = 0;

        for (int j = 0; j < _incidentCount; j++)
        {
            if (_incidentTags[j] == tag && _closedOn[j] == "")
            {
                n++;
            }
        }

        return n;
    }

    // ---------- Filters: each returns an exact-size array of tags ----------
    // Two passes: count the matches, size the result, fill it. That is the price
    // of a fixed-size array and the reason List<T> exists — which is the point.

    public static string[] OutOfWarranty(DateOnly asOf)
    {
        int matches = 0;
        for (int i = 0; i < _assetCount; i++)
        {
            if (IsOutOfWarrantyRow(i, asOf)) matches++;
        }

        string[] result = new string[matches];
        int k = 0;
        for (int i = 0; i < _assetCount; i++)
        {
            if (IsOutOfWarrantyRow(i, asOf)) result[k++] = _tags[i];
        }

        return result;
    }

    public static string[] RepeatOffenders(int threshold)
    {
        int matches = 0;
        for (int i = 0; i < _assetCount; i++)
        {
            if (TriageRules.IsRepeatOffender(IncidentCountFor(_tags[i]), threshold)) matches++;
        }

        string[] result = new string[matches];
        int k = 0;
        for (int i = 0; i < _assetCount; i++)
        {
            if (TriageRules.IsRepeatOffender(IncidentCountFor(_tags[i]), threshold)) result[k++] = _tags[i];
        }

        return result;
    }

    public static string[] RefreshCandidates(DateOnly asOf)
    {
        int matches = 0;
        for (int i = 0; i < _assetCount; i++)
        {
            if (IsRefreshEligibleRow(i, asOf)) matches++;
        }

        string[] result = new string[matches];
        int k = 0;
        for (int i = 0; i < _assetCount; i++)
        {
            if (IsRefreshEligibleRow(i, asOf)) result[k++] = _tags[i];
        }

        return result;
    }

    // ---------- Summary ----------

    /// <summary>Asset count and total purchase cost per type, one line each.</summary>
    public static string SummaryByType()
    {
        string[] types = { "Laptop", "Desktop", "Peripheral" };
        string summary = "";

        for (int t = 0; t < types.Length; t++)
        {
            int count = 0;
            decimal cost = 0m;

            for (int i = 0; i < _assetCount; i++)
            {
                if (_types[i] == types[t])
                {
                    count++;
                    cost += _purchaseCosts[i];
                }
            }

            summary += $"{types[t],-11} {count,3} assets  {cost,10:F2}\n";
        }

        return summary;
    }

    // ---------- Row-level helpers: glue between a table row and TriageRules ----------

    private static bool IsOutOfWarrantyRow(int i, DateOnly asOf)
    {
        return TriageRules.IsActive(_statuses[i])
            && TriageRules.IsOutOfWarranty(_purchaseDates[i], _warrantyMonths[i], asOf);
    }

    private static bool IsRefreshEligibleRow(int i, DateOnly asOf)
    {
        if (!TriageRules.IsActive(_statuses[i]))
        {
            return false;
        }

        int months = TriageRules.MonthsInService(_purchaseDates[i], asOf);
        bool outOfWarranty = TriageRules.IsOutOfWarranty(_purchaseDates[i], _warrantyMonths[i], asOf);
        int open = OpenIncidentCount(_tags[i]);

        return TriageRules.IsRefreshEligible(_types[i], _statuses[i], months, outOfWarranty, open);
    }
}
