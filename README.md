# AssetDesk

Find the endpoints that are out of warranty or keep coming back — before they turn into tickets.

Every help desk has the same two blind spots: the machines that quietly
fall out of warranty, and the machines that keep coming back. Both live
in someone's head or someone's spreadsheet, and both get found the
expensive way — after the ticket, not before. AssetDesk is a console tool
that imports the asset roster you already have, ties incidents to the
devices that generated them, and tells you which endpoints to refresh
before they become tickets.
## Run it

Needs the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). Nothing else.

```bash
git clone https://github.com/timothywheelspro/AssetDesk.git
cd AssetDesk
dotnet run
```

Sample data ships in `data/` and is copied next to the executable on build, so a clean clone runs with no setup. See `data/README.md` for the schema and for the deliberately broken file the importer has to survive.
