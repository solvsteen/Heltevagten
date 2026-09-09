using Heltevagten.Heroes;
using Heltevagten.Incidents;
using Heltevagten.Execeptions;

namespace Heltevagten.Dispatching;

/// <summary>
/// Registrerer helte og hændelser og styrer deres livscyklus.
/// Valget af helt delegeres til den strategi, som constructoren modtager.
/// </summary>
public class DispatchCenter
{
    private readonly List<Hero> heroes = new List<Hero>();
    private readonly List<Incident> incidents = new List<Incident>();
    private readonly IDispatchStrategy dispatchStrategy;

    /// <summary>
    /// Beskyttet visning af listen. AsReadOnly forhindrer, at kalderen kan caste
    /// tilbage til List og tilføje eller fjerne elementer uden validering.
    /// Heltens offentlige metoder kan stadig kaldes.
    /// </summary>
    public IReadOnlyCollection<Hero> Heroes => heroes.AsReadOnly();

    /// <summary>Beskyttet visning af indmeldte hændelser.</summary>
    public IReadOnlyCollection<Incident> Incidents => incidents.AsReadOnly();

    /// <summary>Modtager en afhængighed udefra: constructor injection.</summary>
    public DispatchCenter(IDispatchStrategy dispatchStrategy)
    {
        ArgumentNullException.ThrowIfNull(dispatchStrategy);
        this.dispatchStrategy = dispatchStrategy;
    }

    /// <summary>Registrerer en helt, men ikke samme objekt to gange.</summary>
    public void RegisterHero(Hero hero)
    {
        ArgumentNullException.ThrowIfNull(hero);
        if (heroes.Contains(hero))
        {
            throw new InvalidOperationException("Helten er allerede registreret.");
        }

        heroes.Add(hero);
    }

    /// <summary>Registrerer en ny hændelse uden tidligere tildeling.</summary>
    public void ReportIncident(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);
        if (incidents.Contains(incident) || incident.IsResolved || incident.AssignedHero is not null)
        {
            throw new InvalidOperationException("Hændelsen er allerede registreret, tildelt eller løst.");
        }

        incidents.Add(incident);
    }

    /// <summary>Bruger strategien og tildeler dens valgte helt.</summary>
    /// <exception cref="NoSuitableHeroFoundException">Ingen kandidat blev fundet.</exception>
    public Hero DispatchHero(Incident incident)
    {
        ValidateIncidentForAssignment(incident);
        Hero? selectedHero = dispatchStrategy.SelectHero(incident, Heroes);

        if (selectedHero is null)
        {
            throw new NoSuitableHeroFoundException("Ingen ledig helt med energi blev fundet.");
        }

        // Strategien vælger kun. Vagtcentralen kontrollerer og ændrer status,
        // så også en ny strategi skal gennem de samme tildelingsregler.
        AssignHero(selectedHero, incident);
        return selectedHero;
    }

    /// <summary>Tildeler manuelt en registreret helt til en indmeldt hændelse.</summary>
    /// <exception cref="HeroUnavailableException">Helten er optaget eller uden energi.</exception>
    public void AssignHero(Hero hero, Incident incident)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ValidateIncidentForAssignment(incident);

        if (!heroes.Contains(hero))
        {
            throw new InvalidOperationException("Helten er ikke registreret i denne vagtcentral.");
        }

        if (!hero.IsAvailable || hero.EnergyLevel <= 0)
        {
            throw new HeroUnavailableException($"{hero.Name} er optaget eller mangler energi.");
        }

        // Alt valideres før ændringer. Én udrykning koster én energi.
        // Løsningen er sekventiel; disse trin er ikke en trådsikker transaktion.
        hero.UseEnergy(1);
        incident.AssignHero(hero);
        hero.SetAvailability(false);
    }

    /// <summary>
    /// Løser en tildelt hændelse, frigiver helten og kalder den medsendte handling.
    /// Callbacket er til logning eller anden valgfri efterbehandling.
    /// </summary>
    public void ResolveIncident(Incident incident, Action<Incident> onResolved)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(onResolved);

        if (!incidents.Contains(incident))
        {
            throw new InvalidOperationException("Hændelsen er ikke indmeldt her.");
        }

        Hero? assignedHero = incident.AssignedHero;
        if (incident.IsResolved || assignedHero is null)
        {
            throw new InvalidOperationException("Hændelsen skal være tildelt og endnu ikke løst.");
        }

        incident.MarkAsResolved();
        assignedHero.SetAvailability(true);

        // Action<Incident> modtager en Incident og returnerer void.
        // Domænet er allerede konsistent, hvis den eksterne callback skulle fejle.
        onResolved(incident);
    }

    /// <summary>Samler fælles kontrol til både automatisk og manuel tildeling.</summary>
    private void ValidateIncidentForAssignment(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);
        if (!incidents.Contains(incident))
        {
            throw new InvalidOperationException("Hændelsen er ikke indmeldt her.");
        }

        if (incident.IsResolved || incident.AssignedHero is not null)
        {
            throw new InvalidOperationException("Hændelsen er allerede tildelt eller løst.");
        }
    }
}

