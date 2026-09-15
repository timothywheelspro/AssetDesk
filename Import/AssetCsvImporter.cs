// Import/AssetCsvImporter.cs — Module 6: the file boundary.
//
// Contract: Import never throws. A missing or unreadable file, a malformed
// row, a duplicate tag — every one of them becomes a RejectedRow with a line
// number and a reason, and the good rows still come through. Garbage stops
// here so nothing downstream has to check for it.
//
// Two kinds of failure are handled two ways on purpose:
//   - Expected bad input (wrong column count, "not-a-date", negative cost)
//     is detected with TryParse and if-checks. No exception is thrown for
//     a problem we can see coming.
//   - Things the file system does to us (file missing, locked, unreadable)
//     and the domain constructor's own validation are caught with try/catch,
//     because those are raised by code we don't control.

using System.Globalization;
using AssetDesk.Domain;

namespace AssetDesk.Import;

public static class AssetCsvImporter
{
    private const int ExpectedColumns = 9;
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>Import every well-formed row from an assets CSV. Never throws.</summary>
    public static ImportResult Import(string path)
    {
        string[] lines;

        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (FileNotFoundException)
        {
            return FileProblem($"file not found: {path}");
        }
        catch (DirectoryNotFoundException)
        {
            return FileProblem($"directory not found: {path}");
        }
        catch (UnauthorizedAccessException)
        {
            return FileProblem($"access denied: {path}");
        }
        catch (IOException ex)
        {
            return FileProblem($"could not read {path}: {ex.Message}");
        }

        // Upper bound: every line after the header could be a row. Sized once,
        // trimmed to exact length at the end — the fixed-array pattern from M3.
        int capacity = Math.Max(0, lines.Length - 1);
        Asset[] imported = new Asset[capacity];
        RejectedRow[] rejected = new RejectedRow[capacity];
        string[] seenTags = new string[capacity];
        int[] seenLines = new int[capacity];
        int importedCount = 0, rejectedCount = 0, seenCount = 0, totalRows = 0;

        for (int i = 1; i < lines.Length; i++)   // skip the header
        {
            string raw = lines[i];
            int lineNo = i + 1;                   // 1-based, header is line 1

            if (raw.Trim() == "")
            {
                continue;                          // blank line: skipped silently (documented decision)
            }

            totalRows++;

            string? reason = ParseRow(raw, seenTags, seenLines, seenCount, out Asset? asset);

            if (asset != null)
            {
                imported[importedCount++] = asset;
                seenTags[seenCount] = asset.AssetTag;
                seenLines[seenCount] = lineNo;
                seenCount++;
            }
            else
            {
                rejected[rejectedCount++] = new RejectedRow(lineNo, reason ?? "rejected", raw);
            }
        }

        return new ImportResult(Trim(imported, importedCount), Trim(rejected, rejectedCount), totalRows);
    }

    /// <summary>
    /// Parse one data row. Returns null and sets <paramref name="asset"/> on success;
    /// returns the reason and leaves <paramref name="asset"/> null on failure.
    /// </summary>
    private static string? ParseRow(string raw, string[] seenTags, int[] seenLines, int seenCount, out Asset? asset)
    {
        asset = null;
        string[] f = raw.Split(',');

        if (f.Length != ExpectedColumns)
        {
            return $"expected {ExpectedColumns} columns, found {f.Length}";
        }

        string tag = f[0].Trim();
        string type = f[1].Trim();
        string serial = f[2];
        string model = f[3];

        if (tag == "")
        {
            return "asset_tag is blank";
        }

        string tagKey = tag.ToUpperInvariant();
        for (int s = 0; s < seenCount; s++)
        {
            if (seenTags[s] == tagKey)
            {
                return $"duplicate asset_tag {tagKey} (first seen line {seenLines[s]})";
            }
        }

        if (!DateOnly.TryParseExact(f[4].Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly purchaseDate))
        {
            return $"purchase_date '{f[4]}' is not a {DateFormat} date";
        }

        if (!decimal.TryParse(f[5].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal cost))
        {
            return $"purchase_cost '{f[5]}' is not a number";
        }
        if (cost < 0m)
        {
            return $"purchase_cost {cost} is negative";
        }

        if (!int.TryParse(f[6].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int warrantyMonths))
        {
            return $"warranty_months '{f[6]}' is not an integer";
        }
        if (warrantyMonths < 0)
        {
            return $"warranty_months {warrantyMonths} is negative";
        }

        if (!Enum.TryParse(f[8].Trim(), ignoreCase: false, out AssetStatus status))
        {
            return $"unknown status '{f[8]}'";
        }

        // The `type` column is the factory switch. The domain constructors do
        // their own validation and throw on anything we missed above; that
        // throw is caught here so the contract holds.
        try
        {
            switch (type)
            {
                case "Laptop":
                    asset = new Laptop(tag, serial, model, purchaseDate, cost, warrantyMonths, f[7], status);
                    break;
                case "Desktop":
                    asset = new Desktop(tag, serial, model, purchaseDate, cost, warrantyMonths, f[7], status);
                    break;
                case "Peripheral":
                    asset = new Peripheral(tag, serial, model, purchaseDate, cost, warrantyMonths, f[7], status);
                    break;
                default:
                    return $"unknown type '{type}'";
            }
        }
        catch (ArgumentException ex)
        {
            return $"rejected by {type} constructor: {ex.Message}";
        }

        return null;
    }

    private static ImportResult FileProblem(string reason) =>
        new ImportResult(new Asset[0], new[] { new RejectedRow(0, reason, "") }, 0);

    private static T[] Trim<T>(T[] source, int count)
    {
        T[] exact = new T[count];
        for (int i = 0; i < count; i++) exact[i] = source[i];
        return exact;
    }
}
