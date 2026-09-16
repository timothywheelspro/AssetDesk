// Domain/Peripheral.cs — Module 5: monitors, docks, headsets, printers.
// Failure-driven, not age-driven: no scheduled refresh; replace when it breaks
// and isn't worth fixing. Does NOT implement IDepreciable — a peripheral is
// expensed at purchase, so "what is it worth today?" is not a question it answers.

namespace AssetDesk.Domain;

public sealed class Peripheral : Asset
{
    /// <summary>Monitor, Dock, Headset, Printer, Keyboard — or "Other".</summary>
    public string PeripheralKind { get; }

    public Peripheral(
        string assetTag, string serialNumber, string model,
        DateOnly purchaseDate, decimal purchaseCost, int warrantyMonths,
        string assignedTo = "", AssetStatus status = AssetStatus.InService,
        string? peripheralKind = null)
        : base(assetTag, serialNumber, model, purchaseDate, purchaseCost, warrantyMonths, assignedTo, status)
    {
        PeripheralKind = peripheralKind ?? KindFromModel(Model);
    }

    public override string AssetType => "Peripheral";
    public override int RefreshCycleMonths => 0;      // no schedule

    // Failure-driven: in repair, or out of warranty with any open ticket
    // (cheaper to replace than to fix).
    public override bool IsRefreshEligible(DateOnly asOf, int openIncidentCount) =>
        IsActive
        && (Status == AssetStatus.InRepair
            || (IsOutOfWarranty(asOf) && openIncidentCount >= 1));

    public override string ToRosterLine() =>
        base.ToRosterLine() + $"  ({PeripheralKind})";

    /// <summary>Best-effort kind from the model string when the roster doesn't say.</summary>
    public static string KindFromModel(string model)
    {
        string m = model.ToLowerInvariant();
        if (m.Contains("monitor"))  return "Monitor";
        if (m.Contains("dock"))     return "Dock";
        if (m.Contains("headset"))  return "Headset";
        if (m.Contains("laserjet") || m.Contains("printer")) return "Printer";
        if (m.Contains("keys") || m.Contains("keyboard"))    return "Keyboard";
        return "Other";
    }
}
