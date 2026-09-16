// Domain/Laptop.cs — Module 5: portable endpoint. Three-year refresh,
// straight-line depreciation over the cycle.

namespace AssetDesk.Domain;

public sealed class Laptop : Asset, IDepreciable
{
    public bool HasDock { get; set; }

    public Laptop(
        string assetTag, string serialNumber, string model,
        DateOnly purchaseDate, decimal purchaseCost, int warrantyMonths,
        string assignedTo = "", AssetStatus status = AssetStatus.InService,
        bool hasDock = false)
        : base(assetTag, serialNumber, model, purchaseDate, purchaseCost, warrantyMonths, assignedTo, status)
    {
        HasDock = hasDock;
    }

    public override string AssetType => "Laptop";
    public override int RefreshCycleMonths => 36;

    // IDepreciable: written down over the same span as the refresh cycle.
    public int DepreciationLifeMonths => RefreshCycleMonths;
    public decimal CurrentValue(DateOnly asOf) => StraightLineValue(asOf, DepreciationLifeMonths);

    // Age-driven: past the cycle, or out of warranty and generating repeat tickets.
    public override bool IsRefreshEligible(DateOnly asOf, int openIncidentCount) =>
        IsActive
        && (MonthsInService(asOf) >= RefreshCycleMonths
            || (IsOutOfWarranty(asOf) && openIncidentCount >= 2));

    public override string ToRosterLine() =>
        base.ToRosterLine() + (HasDock ? "  [dock]" : string.Empty);
}
