// Domain/Incident.cs — Module 4: one help-desk ticket as a class.

namespace AssetDesk.Domain;

public enum IncidentPriority { Low, Medium, High, Critical }

public enum IncidentCategory { Hardware, Software, Network, Access }

public class Incident
{
    public string IncidentId { get; }
    public string AssetTag { get; }
    public DateOnly OpenedOn { get; }
    public DateOnly? ClosedOn { get; private set; }   // null = still open
    public IncidentPriority Priority { get; }
    public IncidentCategory Category { get; }
    public string TechId { get; set; }                // empty = unassigned
    public string Description { get; }

    public Incident(
        string incidentId,
        string assetTag,
        DateOnly openedOn,
        DateOnly? closedOn,
        IncidentPriority priority,
        IncidentCategory category,
        string techId,
        string description)
    {
        if (string.IsNullOrWhiteSpace(incidentId))
            throw new ArgumentException("Incident id is required.", nameof(incidentId));
        if (string.IsNullOrWhiteSpace(assetTag))
            throw new ArgumentException("Asset tag is required.", nameof(assetTag));
        if (closedOn.HasValue && closedOn.Value < openedOn)
            throw new ArgumentOutOfRangeException(nameof(closedOn), "Cannot close before opening.");

        IncidentId = incidentId.Trim();
        AssetTag = assetTag.Trim().ToUpperInvariant();
        OpenedOn = openedOn;
        ClosedOn = closedOn;
        Priority = priority;
        Category = category;
        TechId = techId?.Trim() ?? string.Empty;
        Description = description?.Trim() ?? string.Empty;
    }

    public bool IsOpen => !ClosedOn.HasValue;

    /// <summary>Days the ticket has been (or was) open.</summary>
    public int DaysOpen(DateOnly asOf)
    {
        DateOnly end = ClosedOn ?? asOf;
        return Math.Max(0, end.DayNumber - OpenedOn.DayNumber);
    }

    public void Close(DateOnly on)
    {
        if (on < OpenedOn)
            throw new ArgumentOutOfRangeException(nameof(on), "Cannot close before opening.");
        ClosedOn = on;
    }

    public int PriorityRank => TriageRules.PriorityRank(Priority.ToString());

    public override string ToString() =>
        $"{IncidentId} on {AssetTag} ({Priority}): {Description}";
}
