// Domain/IDepreciable.cs — Module 6: a capability, not a kind of thing.
//
// Laptops and desktops lose book value over a planned life. Peripherals don't:
// they are expensed the day they're bought. That is not a difference in the
// number (a zero-month life) — it is the absence of the capability. An
// interface says exactly that: some assets can answer "what are you worth
// today?" and some can't be asked. Asset stays an abstract class because it
// owns real state and shared math; IDepreciable is orthogonal to that
// hierarchy, which is what interfaces are for.

namespace AssetDesk.Domain;

public interface IDepreciable
{
    /// <summary>Months over which the purchase cost is written down to zero.</summary>
    int DepreciationLifeMonths { get; }

    /// <summary>Book value on the given date.</summary>
    decimal CurrentValue(DateOnly asOf);
}
