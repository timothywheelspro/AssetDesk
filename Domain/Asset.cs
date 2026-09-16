// Domain/Asset.cs — Module 5: the base contract for every tracked endpoint.
//
// Identity is fixed at construction and validated once, here, so no subclass
// can produce an invalid asset. Shared math (warranty, months in service,
// straight-line depreciation) is written once. What genuinely differs per
// type — the refresh schedule and when to replace — is abstract, because the
// policies differ in kind, not just in number. Book value is NOT here: only
// some assets depreciate, and those implement IDepreciable.

namespace AssetDesk.Domain;

/// <summary>Lifecycle state of a tracked endpoint.</summary>
public enum AssetStatus
{
    InService,
    InRepair,
    Retired,
    Disposed
}

public abstract class Asset
{
    // ---------- Identity: fixed at intake, never mutated ----------

    /// <summary>Inventory tag, normalised to upper-case. The primary key across the whole app.</summary>
    public string AssetTag { get; }
    public string SerialNumber { get; }
    public string Model { get; }
    public DateOnly PurchaseDate { get; }
    public decimal PurchaseCost { get; }
    public int WarrantyMonths { get; }

    // ---------- Custody: changes over the asset's life ----------

    /// <summary>User or location currently holding the asset. Empty means unassigned / in stock.</summary>
    public string AssignedTo { get; set; }
    public AssetStatus Status { get; set; }

    protected Asset(
        string assetTag,
        string serialNumber,
        string model,
        DateOnly purchaseDate,
        decimal purchaseCost,
        int warrantyMonths,
        string assignedTo,
        AssetStatus status)
    {
        if (string.IsNullOrWhiteSpace(assetTag))
            throw new ArgumentException("Asset tag is required.", nameof(assetTag));
        if (purchaseCost < 0m)
            throw new ArgumentOutOfRangeException(nameof(purchaseCost), "Cost cannot be negative.");
        if (warrantyMonths < 0)
            throw new ArgumentOutOfRangeException(nameof(warrantyMonths), "Warranty months cannot be negative.");

        AssetTag = assetTag.Trim().ToUpperInvariant();
        SerialNumber = serialNumber?.Trim() ?? string.Empty;
        Model = model?.Trim() ?? string.Empty;
        PurchaseDate = purchaseDate;
        PurchaseCost = purchaseCost;
        WarrantyMonths = warrantyMonths;
        AssignedTo = assignedTo?.Trim() ?? string.Empty;
        Status = status;
    }

    // ---------- The contract: every concrete type MUST answer these ----------

    /// <summary>Type label used in reports: "Laptop", "Desktop", "Peripheral".</summary>
    public abstract string AssetType { get; }

    /// <summary>Planned service life in months. Zero means "no schedule; replace on failure".</summary>
    public abstract int RefreshCycleMonths { get; }

    /// <summary>
    /// True when this asset should be flagged for replacement. Age-driven for computers,
    /// failure-driven for peripherals — which is why it is abstract and not a number.
    /// </summary>
    public abstract bool IsRefreshEligible(DateOnly asOf, int openIncidentCount);

    // ---------- Shared behaviour: written once here, inherited by every type ----------

    public DateOnly WarrantyExpires => PurchaseDate.AddMonths(WarrantyMonths);

    public bool IsOutOfWarranty(DateOnly asOf) => asOf > WarrantyExpires;

    /// <summary>Whole months between purchase and asOf, never negative.</summary>
    public int MonthsInService(DateOnly asOf) => TriageRules.MonthsInService(PurchaseDate, asOf);

    /// <summary>Retired and Disposed assets are out of scope for triage.</summary>
    public bool IsActive => Status != AssetStatus.Retired && Status != AssetStatus.Disposed;

    /// <summary>Straight-line depreciation to zero over lifeMonths. IDepreciable subclasses call this.</summary>
    protected decimal StraightLineValue(DateOnly asOf, int lifeMonths)
    {
        if (lifeMonths <= 0) return 0m;

        decimal remainingFraction = 1m - (decimal)MonthsInService(asOf) / lifeMonths;
        if (remainingFraction < 0m) remainingFraction = 0m;
        if (remainingFraction > 1m) remainingFraction = 1m;

        return Math.Round(PurchaseCost * remainingFraction, 2);
    }

    /// <summary>One fixed-width roster line. Subclasses may override to append type-specific detail.</summary>
    public virtual string ToRosterLine() =>
        $"{AssetTag,-10} {AssetType,-11} {Model,-24} {Status,-10} {(AssignedTo == "" ? "(unassigned)" : AssignedTo)}";

    public override string ToString() => ToRosterLine();
}
