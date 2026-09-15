// TriageRules.cs — the triage logic that is not about any one asset type.
//
// Module 3: every rule lived here as a static function over primitives,
// including the per-type policy as switch statements on a string.
// Module 5: the per-type policy moved into Laptop / Desktop / Peripheral
// overrides, so the switches are gone. What remains is the math and the
// rules that operate across assets or incidents rather than inside one.

using AssetDesk.Domain;

namespace AssetDesk;

public static class TriageRules
{
    // ---------- Date math (shared by Asset; kept here so it stays testable on primitives) ----------

    /// <summary>Whole months between purchase and asOf, never negative.</summary>
    public static int MonthsInService(DateOnly purchaseDate, DateOnly asOf)
    {
        int months = (asOf.Year - purchaseDate.Year) * 12 + (asOf.Month - purchaseDate.Month);

        // Haven't reached the purchase day-of-month yet, so the current month doesn't count.
        if (asOf.Day < purchaseDate.Day)
        {
            months--;
        }

        return months < 0 ? 0 : months;
    }

    // ---------- Cross-asset rules ----------

    /// <summary>Repeat offender: the asset has generated at least `threshold` incidents.</summary>
    public static bool IsRepeatOffender(int incidentCount, int threshold)
    {
        return incidentCount >= threshold;
    }

    /// <summary>Numeric rank so incidents can be compared: Critical = 4 … Low = 1, unknown = 0.</summary>
    public static int PriorityRank(string priority)
    {
        switch (priority)
        {
            case "Critical": return 4;
            case "High":     return 3;
            case "Medium":   return 2;
            case "Low":      return 1;
            default:         return 0;
        }
    }

    /// <summary>
    /// Copy of the input sorted highest priority first (selection sort on a fixed
    /// array — no LINQ). Ties keep their original order.
    /// </summary>
    public static Incident[] SortByPriority(Incident[] incidents)
    {
        Incident[] sorted = new Incident[incidents.Length];
        for (int i = 0; i < incidents.Length; i++) sorted[i] = incidents[i];

        for (int i = 0; i < sorted.Length - 1; i++)
        {
            int best = i;
            for (int j = i + 1; j < sorted.Length; j++)
            {
                if (sorted[j].PriorityRank > sorted[best].PriorityRank) best = j;
            }
            if (best != i)
            {
                // Shift the block right so equal-priority items keep their order.
                Incident top = sorted[best];
                for (int k = best; k > i; k--) sorted[k] = sorted[k - 1];
                sorted[i] = top;
            }
        }

        return sorted;
    }
}
