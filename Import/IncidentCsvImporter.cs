// Import/IncidentCsvImporter.cs — Module 6: the file boundary for tickets.
//
// Same contract as AssetCsvImporter: Import never throws, every bad row is a
// RejectedRow with a line and a reason, good rows still come through.
//
// Two things are specific to incidents:
//   - The description is free text and may contain commas. A row with more
//     than the expected columns is not rejected; the extra pieces are joined
//     back into the description. A row with fewer columns is rejected.
//   - An incident points at an asset. When an Inventory is supplied, a ticket
//     whose asset_tag is not in it is rejected — a ticket against a machine
//     we don't track is a data problem, not a triage input.

using System.Globalization;
using AssetDesk.Domain;

namespace AssetDesk.Import;

public static class IncidentCsvImporter
{
    private const int ExpectedColumns = 8;
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>Import every well-formed row from an incidents CSV. Never throws.</summary>
    public static IncidentImportResult Import(string path, Inventory? inventory = null)
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

        int capacity = Math.Max(0, lines.Length - 1);
        Incident[] imported = new Incident[capacity];
        RejectedRow[] rejected = new RejectedRow[capacity];
        string[] seenIds = new string[capacity];
        int[] seenLines = new int[capacity];
        int importedCount = 0, rejectedCount = 0, seenCount = 0, totalRows = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string raw = lines[i];
            int lineNo = i + 1;

            if (raw.Trim() == "")
            {
                continue;
            }

            totalRows++;

            string? reason = ParseRow(raw, seenIds, seenLines, seenCount, inventory, out Incident? incident);

            if (incident != null)
            {
                imported[importedCount++] = incident;
                seenIds[seenCount] = incident.IncidentId;
                seenLines[seenCount] = lineNo;
                seenCount++;
            }
            else
            {
                rejected[rejectedCount++] = new RejectedRow(lineNo, reason ?? "rejected", raw);
            }
        }

        return new IncidentImportResult(Trim(imported, importedCount), Trim(rejected, rejectedCount), totalRows);
    }

    private static string? ParseRow(
        string raw, string[] seenIds, int[] seenLines, int seenCount, Inventory? inventory,
        out Incident? incident)
    {
        incident = null;
        string[] f = raw.Split(',');

        if (f.Length < ExpectedColumns)
        {
            return $"expected {ExpectedColumns} columns, found {f.Length}";
        }

        // Free-text description: rejoin anything past the last fixed column.
        string description = f[ExpectedColumns - 1];
        for (int extra = ExpectedColumns; extra < f.Length; extra++)
        {
            description += "," + f[extra];
        }

        string id = f[0].Trim();
        string tag = f[1].Trim().ToUpperInvariant();

        if (id == "")
        {
            return "incident_id is blank";
        }

        for (int s = 0; s < seenCount; s++)
        {
            if (seenIds[s] == id)
            {
                return $"duplicate incident_id {id} (first seen line {seenLines[s]})";
            }
        }

        if (tag == "")
        {
            return "asset_tag is blank";
        }

        if (inventory != null && inventory.FindByTag(tag) == null)
        {
            return $"asset_tag {tag} is not in the inventory";
        }

        if (!DateOnly.TryParseExact(f[2].Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly openedOn))
        {
            return $"opened_on '{f[2]}' is not a {DateFormat} date";
        }

        DateOnly? closedOn = null;
        if (f[3].Trim() != "")
        {
            if (!DateOnly.TryParseExact(f[3].Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly closed))
            {
                return $"closed_on '{f[3]}' is not a {DateFormat} date";
            }
            if (closed < openedOn)
            {
                return $"closed_on {closed:yyyy-MM-dd} is before opened_on {openedOn:yyyy-MM-dd}";
            }
            closedOn = closed;
        }

        if (!Enum.TryParse(f[4].Trim(), ignoreCase: false, out IncidentPriority priority))
        {
            return $"unknown priority '{f[4]}'";
        }

        if (!Enum.TryParse(f[5].Trim(), ignoreCase: false, out IncidentCategory category))
        {
            return $"unknown category '{f[5]}'";
        }

        try
        {
            incident = new Incident(id, tag, openedOn, closedOn, priority, category, f[6], description);
        }
        catch (ArgumentException ex)
        {
            return $"rejected by Incident constructor: {ex.Message}";
        }

        return null;
    }

    private static IncidentImportResult FileProblem(string reason) =>
        new IncidentImportResult(new Incident[0], new[] { new RejectedRow(0, reason, "") }, 0);

    private static T[] Trim<T>(T[] source, int count)
    {
        T[] exact = new T[count];
        for (int i = 0; i < count; i++) exact[i] = source[i];
        return exact;
    }
}
