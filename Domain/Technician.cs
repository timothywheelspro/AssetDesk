// Domain/Technician.cs — Module 4: who works the tickets.

namespace AssetDesk.Domain;

public class Technician
{
    public string TechId { get; }
    public string Name { get; }
    public int Tier { get; }

    public Technician(string techId, string name, int tier)
    {
        if (string.IsNullOrWhiteSpace(techId))
            throw new ArgumentException("Tech id is required.", nameof(techId));
        if (tier < 1 || tier > 3)
            throw new ArgumentOutOfRangeException(nameof(tier), "Tier must be 1–3.");

        TechId = techId.Trim();
        Name = name?.Trim() ?? string.Empty;
        Tier = tier;
    }

    public override string ToString() => $"{TechId} {Name} (Tier {Tier})";
}
