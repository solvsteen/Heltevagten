using Heltevagten.Heroes;

namespace Heltevagten.Incidents;

/// <summary>En indmeldt hændelse og dens tildelings- og løsningsstatus.</summary>
public class Incident
{
    /// <summary>Unik identifikation, også når to hændelser har samme beskrivelse.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Hvad der er sket.</summary>
    public string Description { get; }

    /// <summary>Hvor hjælpen er nødvendig.</summary>
    public string Location { get; }

    /// <summary>Hvor alvorlig hændelsen er.</summary>
    public Severity Severity { get; }

    /// <summary>Om hændelsen er afsluttet.</summary>
    public bool IsResolved { get; private set; }

    /// <summary>
    /// Den tildelte helt. ? betyder, at værdien må være null før tildeling.
    /// Referencen bevares efter løsning som historik.
    /// </summary>
    public Hero? AssignedHero { get; private set; }

    /// <summary>Opretter en uløst hændelse uden en tildelt helt.</summary>
    public Incident(string description, string location, Severity severity)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Beskrivelsen må ikke være tom.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("Placeringen må ikke være tom.", nameof(location));
        }

        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity), "Ukendt alvorlighedsgrad.");
        }

        Description = description.Trim();
        Location = location.Trim();
        Severity = severity;
    }

    /// <summary>Tilknytter en helt. Vagtcentralen kontrollerer også heltens status.</summary>
    internal void AssignHero(Hero hero)
    {
        ArgumentNullException.ThrowIfNull(hero);
        if (IsResolved || AssignedHero is not null)
        {
            throw new InvalidOperationException("Hændelsen er allerede tildelt eller løst.");
        }

        AssignedHero = hero;
    }

    /// <summary>Afslutter en tildelt hændelse præcis én gang.</summary>
    internal void MarkAsResolved()
    {
        if (IsResolved || AssignedHero is null)
        {
            throw new InvalidOperationException("Kun en aktiv, tildelt hændelse kan løses.");
        }

        IsResolved = true;
    }
}

