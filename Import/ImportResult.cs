// Import/ImportResult.cs — Module 6: what an import produced. Two exact-size
// arrays and a count; the caller decides what to do about rejections.

using AssetDesk.Domain;

namespace AssetDesk.Import;

public sealed class ImportResult
{
    public Asset[] Imported { get; }
    public RejectedRow[] Rejected { get; }

    /// <summary>Data rows examined (header and blank lines excluded). 0 when the file could not be read.</summary>
    public int TotalRows { get; }

    public bool HasRejections => Rejected.Length > 0;

    public ImportResult(Asset[] imported, RejectedRow[] rejected, int totalRows)
    {
        Imported = imported;
        Rejected = rejected;
        TotalRows = totalRows;
    }

    public override string ToString() =>
        $"{Imported.Length} imported, {Rejected.Length} rejected, {TotalRows} rows examined";
}
