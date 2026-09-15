// Domain/Desktop.cs — Module 5: fixed endpoint. Five-year refresh,
// straight-line depreciation over the cycle, higher tolerance for tickets
// because a desktop is cheaper to service in place.

namespace AssetDesk.Domain;

public sealed class Desktop : Asset
{
    /// <summary>Physical location (room, desk). Empty when unknown.</summary>
    public string Location { get; set; }

    public Desktop(
        string assetTag, string serialNumber, string model,
        DateOnly purchaseDate, decimal purchaseCost, int warrantyMonths,
        string assignedTo = "", AssetStatus status = AssetStatus.InService,
        string location = "")
        : base(assetTag, serialNumber, model, purchaseDate, purchaseCost, warrantyMonths, assignedTo, status)
    {
        Location = location?.Trim() ?? string.Empty;
    }

    public override string AssetType => "Desktop";
    public override int RefreshCycleMonths => 60;

    public override decimal CurrentValue(DateOnly asOf) =>
        StraightLineValue(asOf, RefreshCycleMonths);

    // Age-driven, with a higher ticket threshold than a laptop.
    public override bool IsRefreshEligible(DateOnly asOf, int openIncidentCount) =>
        IsActive
        && (MonthsInService(asOf) >= RefreshCycleMonths
            || (IsOutOfWarranty(asOf) && openIncidentCount >= 3));

    public override string ToRosterLine() =>
        base.ToRosterLine() + (Location == "" ? string.Empty : $"  @{Location}");
}
