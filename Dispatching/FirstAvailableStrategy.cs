using Heltevagten.Heroes;
using Heltevagten.Incidents;
using Heltevagten.Utilities;

namespace Heltevagten.Dispatching;

/// <summary>Vælger den første egnede helt i registreringsrækkefølge.</summary>
public class FirstAvailableStrategy : IDispatchStrategy
{
    /// <summary>Genbruger den generiske søgning til en samling af helte.</summary>
    public Hero? SelectHero(Incident incident, IEnumerable<Hero> heroes)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(heroes);

        // Lambda: hero er input; udtrykket efter => er betingelsen.
        return SearchTool.FindFirst(heroes, hero => hero.IsAvailable && hero.EnergyLevel > 0);
    }
}

