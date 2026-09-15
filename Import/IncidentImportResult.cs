// Import/IncidentImportResult.cs — Module 6: what an incident import produced.
// Same shape as ImportResult; kept as its own class rather than a generic so
// the pattern stays readable at this point in the course.

using AssetDesk.Domain;

namespace AssetDesk.Import;

public sealed class IncidentImportResult
{
    public Incident[] Imported { get; }
    public RejectedRow[] Rejected { get; }

    /// <summary>Data rows examined (header and blank lines excluded). 0 when the file could not be read.</summary>
    public int TotalRows { get; }

    public bool HasRejections => Rejected.Length > 0;

    public IncidentImportResult(Incident[] imported, RejectedRow[] rejected, int totalRows)
    {
        Imported = imported;
        Rejected = rejected;
        TotalRows = totalRows;
    }

    public override string ToString() =>
        $"{Imported.Length} imported, {Rejected.Length} rejected, {TotalRows} rows examined";
}
