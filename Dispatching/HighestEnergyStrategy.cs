using Heltevagten.Heroes;
using Heltevagten.Incidents;

namespace Heltevagten.Dispatching;

/// <summary>Vælger den ledige helt med mest energi. Ved lighed vinder den første.</summary>
public class HighestEnergyStrategy : IDispatchStrategy
{
    /// <summary>Sammenligner egnede kandidater uden at tildele dem.</summary>
    public Hero? SelectHero(Incident incident, IEnumerable<Hero> heroes)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(heroes);
        Hero? selectedHero = null;

        foreach (Hero hero in heroes)
        {
            if (!hero.IsAvailable || hero.EnergyLevel <= 0)
            {
                continue;
            }

            if (selectedHero is null || hero.EnergyLevel > selectedHero.EnergyLevel)
            {
                selectedHero = hero;
            }
        }

        return selectedHero;
    }
}

