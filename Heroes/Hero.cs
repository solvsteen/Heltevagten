namespace Heltevagten.Heroes;

/// <summary>
/// Fælles tilstand og regler for alle helte. Abstract betyder,
/// at man kun kan oprette konkrete underklasser, eksempelvis FlyingHero.
/// </summary>
public abstract class Hero
{
    // Const betyder, at værdierne ikke kan ændres, mens programmet kører.
    private const int MinimumEnergyLevel = 0;
    private const int MaximumEnergyLevel = 100;
    private int _energyLevel;

    /// <summary>Heltens navn, som kun sættes ved oprettelse.</summary>
    public string Name { get; }

    /// <summary>
    /// Giver læseadgang til det private felt. Pilen er en kort getter:
    /// den returnerer _energyLevel, hver gang propertyen læses.
    /// </summary>
    public int EnergyLevel => _energyLevel;

    /// <summary>
    /// Angiver, om helten er fri for en aktiv opgave.
    /// En ledig helt kan stadig mangle energi til at blive sendt ud.
    /// </summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Opretter den fælles del af en konkret helt.</summary>
    /// <exception cref="ArgumentException">Navnet mangler.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Energi er uden for 0–100.</exception>
    protected Hero(string name, int energyLevel)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("En helt skal have et navn.", nameof(name));
        }

        if (energyLevel < MinimumEnergyLevel || energyLevel > MaximumEnergyLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(energyLevel), "Energi skal være mellem 0 og 100.");
        }

        Name = name.Trim();
        _energyLevel = energyLevel;
        IsAvailable = true;
    }

    /// <summary>Bruger positiv energi, hvis helten har nok.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Beløbet er ikke positivt.</exception>
    /// <exception cref="InvalidOperationException">Helten har ikke nok energi.</exception>
    public void UseEnergy(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Beløbet skal være større end 0.");
        }

        // Kontrollen sker før ændringen, så en fejl ikke efterlader negativ energi.
        if (amount > _energyLevel)
        {
            throw new InvalidOperationException($"{Name} har ikke nok energi.");
        }

        // -= betyder: træk amount fra den nuværende værdi og gem resultatet.
        _energyLevel -= amount;
    }

    /// <summary>Genopretter positiv energi, dog højst til 100.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Beløbet er ikke positivt.</exception>
    public void RestoreEnergy(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Beløbet skal være større end 0.");
        }

        // Math.Min vælger det mindste tal: 90 + 30 sammenlignes med 100 og giver 100.
        // (long) udfører additionen som et større heltal, så selv int.MaxValue
        // ikke giver overflow. Resultatet er højst 100 og kan sikkert blive int igen.
        _energyLevel = (int)Math.Min((long)_energyLevel + amount, MaximumEnergyLevel);
    }

    /// <summary>
    /// Ændrer tilgængelighed fra domænelogikken. Internal giver adgang fra hele
    /// samme assembly, herunder DispatchCenter; det er ikke adgang kun for én klasse.
    /// </summary>
    internal void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
    }

    /// <summary>
    /// Returnerer en beskrivelse af heltens særlige handling.
    /// Underklasserne skal override metoden. Den ændrer ikke energi.
    /// </summary>
    public abstract string UseSignatureMove();
}

