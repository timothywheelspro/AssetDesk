// Inventory.cs — Module 4: the in-memory roster as an instance class holding
// Asset[] and Incident[] with a count each.
//
// In Module 3 this was seventeen parallel primitive arrays. Now that Asset and
// Incident are classes, each table is one array of objects. Every public
// method keeps its Module 3 name and meaning; what changed is that a row is
// now a thing you can hand to someone. Still no List<T>, still no LINQ.

using AssetDesk.Domain;

namespace AssetDesk;

public class Inventory
{
    public const int MaxAssets = 64;
    public const int MaxIncidents = 256;

    private readonly Asset[] _assets = new Asset[MaxAssets];
    private readonly Incident[] _incidents = new Incident[MaxIncidents];
    private int _assetCount = 0;
    private int _incidentCount = 0;

    public int AssetCount => _assetCount;
    public int IncidentCount => _incidentCount;

    // ---------- Loading ----------

    /// <summary>Append one asset. Returns false (and stores nothing) when the table is full.</summary>
    public bool Add(Asset asset)
    {
        if (_assetCount >= MaxAssets) return false;
        _assets[_assetCount++] = asset;
        return true;
    }

    /// <summary>Append one incident. Returns false when the table is full.</summary>
    public bool Log(Incident incident)
    {
        if (_incidentCount >= MaxIncidents) return false;
        _incidents[_incidentCount++] = incident;
        return true;
    }

    // ---------- Row access ----------

    public Asset AssetAt(int i) => _assets[i];
    public Incident IncidentAt(int j) => _incidents[j];

    // ---------- Search ----------

    /// <summary>Linear search by tag. Returns the asset, or null when not found.</summary>
    public Asset? FindByTag(string tag)
    {
        string key = tag.Trim().ToUpperInvariant();
        for (int i = 0; i < _assetCount; i++)
        {
            if (_assets[i].AssetTag == key) return _assets[i];
        }
        return null;
    }

    /// <summary>How many incidents (open or closed) reference this tag.</summary>
    public int IncidentCountFor(string tag)
    {
        int n = 0;
        for (int j = 0; j < _incidentCount; j++)
        {
            if (_incidents[j].AssetTag == tag) n++;
        }
        return n;
    }

    /// <summary>How many incidents for this tag are still open.</summary>
    public int OpenIncidentCount(string tag)
    {
        int n = 0;
        for (int j = 0; j < _incidentCount; j++)
        {
            if (_incidents[j].AssetTag == tag && _incidents[j].IsOpen) n++;
        }
        return n;
    }

    /// <summary>Exact-size copy of every open incident.</summary>
    public Incident[] OpenIncidents()
    {
        int matches = 0;
        for (int j = 0; j < _incidentCount; j++) if (_incidents[j].IsOpen) matches++;

        Incident[] result = new Incident[matches];
        int k = 0;
        for (int j = 0; j < _incidentCount; j++) if (_incidents[j].IsOpen) result[k++] = _incidents[j];
        return result;
    }

    // ---------- Filters: each returns an exact-size Asset[] ----------
    // Two passes: count the matches, size the result, fill it.

    public Asset[] OutOfWarranty(DateOnly asOf)
    {
        int matches = 0;
        for (int i = 0; i < _assetCount; i++)
        {
            if (_assets[i].IsActive && _assets[i].IsOutOfWarranty(asOf)) matches++;
        }

        Asset[] result = new Asset[matches];
        int k = 0;
        for (int i = 0; i < _assetCount; i++)
        {
            if (_assets[i].IsActive && _assets[i].IsOutOfWarranty(asOf)) result[k++] = _assets[i];
        }
        return result;
    }

    public Asset[] RepeatOffenders(int threshold)
    {
        int matches = 0;
        for (int i = 0; i < _assetCount; i++)
        {
            if (TriageRules.IsRepeatOffender(IncidentCountFor(_assets[i].AssetTag), threshold)) matches++;
        }

        Asset[] result = new Asset[matches];
        int k = 0;
        for (int i = 0; i < _assetCount; i++)
        {
            if (TriageRules.IsRepeatOffender(IncidentCountFor(_assets[i].AssetTag), threshold)) result[k++] = _assets[i];
        }
        return result;
    }

    public Asset[] RefreshCandidates(DateOnly asOf)
    {
        int matches = 0;
        for (int i = 0; i < _assetCount; i++)
        {
            if (_assets[i].IsRefreshEligible(asOf, OpenIncidentCount(_assets[i].AssetTag))) matches++;
        }

        Asset[] result = new Asset[matches];
        int k = 0;
        for (int i = 0; i < _assetCount; i++)
        {
            if (_assets[i].IsRefreshEligible(asOf, OpenIncidentCount(_assets[i].AssetTag))) result[k++] = _assets[i];
        }
        return result;
    }

    // ---------- Summary ----------

    /// <summary>Asset count and total purchase cost per type, one line each.</summary>
    public string SummaryByType()
    {
        string[] types = { "Laptop", "Desktop", "Peripheral" };
        string summary = "";

        for (int t = 0; t < types.Length; t++)
        {
            int count = 0;
            decimal cost = 0m;

            for (int i = 0; i < _assetCount; i++)
            {
                if (_assets[i].AssetType == types[t])
                {
                    count++;
                    cost += _assets[i].PurchaseCost;
                }
            }

            summary += $"{types[t],-11} {count,3} assets  {cost,10:F2}\n";
        }

        return summary;
    }
}
