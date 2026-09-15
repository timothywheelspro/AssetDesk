// Domain/Asset.cs — Module 4: one tracked endpoint as a class.
//
// Identity is fixed at construction and validated once, in the constructor, so
// no code path can produce an invalid asset. Custody (who has it, what state
// it's in) is mutable. The warranty math that lived in TriageRules as static
// functions over primitives now lives here as instance members over the
// asset's own fields. The per-type policy still branches on AssetType; when
// inheritance arrives that branch becomes three subclasses.

namespace AssetDesk.Domain;

/// <summary>Lifecycle state of a tracked endpoint.</summary>
public enum AssetStatus
{
    InService,
    InRepair,
    Retired,
    Disposed
}

public class Asset
{
    // ---------- Identity: fixed at intake, never mutated ----------

    /// <summary>Inventory tag, normalised to upper-case. The primary key across the whole app.</summary>
    public string AssetTag { get; }
    public string AssetType { get; }          // "Laptop" | "Desktop" | "Peripheral"
    public string SerialNumber { get; }
    public string Model { get; }
    public DateOnly PurchaseDate { get; }
    public decimal PurchaseCost { get; }
    public int WarrantyMonths { get; }

    // ---------- Custody: changes over the asset's life ----------

    /// <summary>User or location currently holding the asset. Empty means unassigned / in stock.</summary>
    public string AssignedTo { get; set; }
    public AssetStatus Status { get; set; }

    public Asset(
        string assetTag,
        string assetType,
        string serialNumber,
        string model,
        DateOnly purchaseDate,
        decimal purchaseCost,
        int warrantyMonths,
        string assignedTo = "",
        AssetStatus status = AssetStatus.InService)
    {
        if (string.IsNullOrWhiteSpace(assetTag))
            throw new ArgumentException("Asset tag is required.", nameof(assetTag));
        if (!TriageRules.IsKnownType(assetType))
            throw new ArgumentException($"Unknown asset type '{assetType}'.", nameof(assetType));
        if (purchaseCost < 0m)
            throw new ArgumentOutOfRangeException(nameof(purchaseCost), "Cost cannot be negative.");
        if (warrantyMonths < 0)
            throw new ArgumentOutOfRangeException(nameof(warrantyMonths), "Warranty months cannot be negative.");

        AssetTag = assetTag.Trim().ToUpperInvariant();
        AssetType = assetType;
        SerialNumber = serialNumber?.Trim() ?? string.Empty;
        Model = model?.Trim() ?? string.Empty;
        PurchaseDate = purchaseDate;
        PurchaseCost = purchaseCost;
        WarrantyMonths = warrantyMonths;
        AssignedTo = assignedTo?.Trim() ?? string.Empty;
        Status = status;
    }

    // ---------- Shared behaviour: the asset answers questions about itself ----------

    public DateOnly WarrantyExpires => PurchaseDate.AddMonths(WarrantyMonths);

    public bool IsOutOfWarranty(DateOnly asOf) => asOf > WarrantyExpires;

    /// <summary>Whole months between purchase and asOf, never negative.</summary>
    public int MonthsInService(DateOnly asOf) => TriageRules.MonthsInService(PurchaseDate, asOf);

    /// <summary>Retired and Disposed assets are out of scope for triage.</summary>
    public bool IsActive => Status != AssetStatus.Retired && Status != AssetStatus.Disposed;

    /// <summary>Planned service life for this type. Zero means "replace on failure".</summary>
    public int RefreshCycleMonths => TriageRules.RefreshCycleMonths(AssetType);

    /// <summary>Book value on the given date under this type's depreciation policy.</summary>
    public decimal CurrentValue(DateOnly asOf) =>
        TriageRules.CurrentValue(PurchaseCost, MonthsInService(asOf), RefreshCycleMonths);

    /// <summary>Should this asset be flagged for replacement, given how many of its incidents are open?</summary>
    public bool IsRefreshEligible(DateOnly asOf, int openIncidentCount) =>
        IsActive && TriageRules.IsRefreshEligible(
            AssetType, Status.ToString(), MonthsInService(asOf), IsOutOfWarranty(asOf), openIncidentCount);

    /// <summary>One fixed-width roster line.</summary>
    public string ToRosterLine() =>
        $"{AssetTag,-10} {AssetType,-11} {Model,-24} {Status,-10} {(AssignedTo == "" ? "(unassigned)" : AssignedTo)}";

    public override string ToString() => ToRosterLine();
}
