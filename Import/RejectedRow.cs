// Import/RejectedRow.cs — Module 6: one row the importer refused, and why.

namespace AssetDesk.Import;

public sealed class RejectedRow
{
    /// <summary>1-based line number in the file. 0 means the file itself was the problem.</summary>
    public int LineNumber { get; }
    public string Reason { get; }
    public string RawLine { get; }

    public RejectedRow(int lineNumber, string reason, string rawLine)
    {
        LineNumber = lineNumber;
        Reason = reason;
        RawLine = rawLine;
    }

    public override string ToString() =>
        LineNumber == 0 ? $"file: {Reason}" : $"line {LineNumber,3}: {Reason}";
}
