// TriageRules.cs — Module 3: the triage logic as pure, testable functions.
//
// Every function here takes primitives and returns a value. No state, no I/O, no
// classes. That keeps each rule independently checkable from Program.cs (M3) and
// lets the same math move into the Asset hierarchy in M5/M6 without changing what
// it computes. Policies come from docs/ARCHITECTURE.md §Subclass policies.

namespace AssetDesk;

public static class TriageRules
{
    // ---------- Warranty math ----------

    /// <summary>Date the warranty runs out.</summary>
    public static DateOnly WarrantyExpires(DateOnly purchaseDate, int warrantyMonths)
    {
        return purchaseDate.AddMonths(warrantyMonths);
    }

    /// <summary>True once the as-of date is past the warranty end date.</summary>
    public static bool IsOutOfWarranty(DateOnly purchaseDate, int warrantyMonths, DateOnly asOf)
    {
        return asOf > WarrantyExpires(purchaseDate, warrantyMonths);
    }

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

    // ---------- Per-type policy (decisions) ----------

    /// <summary>Planned service life by asset type. Zero means "no schedule; replace on failure".</summary>
    public static int RefreshCycleMonths(string assetType)
    {
        switch (assetType)
        {
            case "Laptop":     return 36;
            case "Desktop":    return 60;
            case "Peripheral": return 0;
            default:           return 0;
        }
    }

    /// <summary>Straight-line book value: cost shrinks to zero evenly over lifeMonths.</summary>
    public static decimal CurrentValue(decimal purchaseCost, int monthsInService, int lifeMonths)
    {
        if (lifeMonths <= 0)
        {
            return 0m; // expensed at purchase (peripherals)
        }

        decimal remainingFraction = 1m - (decimal)monthsInService / lifeMonths;

        if (remainingFraction < 0m) remainingFraction = 0m;
        if (remainingFraction > 1m) remainingFraction = 1m;

        return Math.Round(purchaseCost * remainingFraction, 2);
    }

    /// <summary>
    /// Should this asset be flagged for replacement? Age-driven for computers,
    /// failure-driven for peripherals — so the rule branches on type, not just on a number.
    /// </summary>
    public static bool IsRefreshEligible(
        string assetType,
        string status,
        int monthsInService,
        bool outOfWarranty,
        int openIncidents)
    {
        int cycle = RefreshCycleMonths(assetType);

        if (assetType == "Laptop")
        {
            return monthsInService >= cycle || (outOfWarranty && openIncidents >= 2);
        }

        if (assetType == "Desktop")
        {
            return monthsInService >= cycle || (outOfWarranty && openIncidents >= 3);
        }

        if (assetType == "Peripheral")
        {
            return status == "InRepair" || (outOfWarranty && openIncidents >= 1);
        }

        return false;
    }

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

    /// <summary>Retired and Disposed assets are out of scope for triage.</summary>
    public static bool IsActive(string status)
    {
        return status != "Retired" && status != "Disposed";
    }
}
