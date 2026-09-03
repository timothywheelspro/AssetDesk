// AssetDesk — console endpoint inventory and incident tracker.
// SIS250 course project. Entry point; module work lands here and under Domain/.

Console.WriteLine("AssetDesk");
Console.WriteLine("Find the endpoints that are out of warranty or keep coming back — before they turn into tickets.");
Console.WriteLine();

// Prove the toolchain and the data files are wired up before any real logic exists.
string dataDir = Path.Combine(AppContext.BaseDirectory, "data");

foreach (string fileName in new[] { "assets.csv", "incidents.csv", "technicians.csv" })
{
    string path = Path.Combine(dataDir, fileName);
    string status = File.Exists(path)
        ? $"{File.ReadAllLines(path).Length - 1} rows"
        : "missing";

    Console.WriteLine($"{fileName,-16} {status}");
}
