using Heltevagten.Heroes;
using Heltevagten.Incidents;

namespace Heltevagten.Dispatching;

/// <summary>
/// Kontrakten for valg af helt. Strategien vælger, men ændrer ikke objekternes status.
/// En egnet helt er ledig og har mindst én energi.
/// </summary>
public interface IDispatchStrategy
{
    /// <summary>Vælger en helt fra samlingen eller returnerer null ved intet match.</summary>
    Hero? SelectHero(Incident incident, IEnumerable<Hero> heroes);
}

