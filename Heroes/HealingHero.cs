namespace Heltevagten;

/// <summary>En konkret helt, der kan genoprette en anden helts energi.</summary>
public class HealingHero : Hero, IHealer
{
    /// <summary>Opretter en helbredende helt med basisklassens validering.</summary>
    public HealingHero(string name, int energyLevel) : base(name, energyLevel)
    {
    }
    
    /// <summary>Beskriver signaturhandlingen uden at ændre tilstanden.</summary>
    public override string UseSignatureMove()
    {
        return $"{Name} skaber et mystisk lys som healer";
    }
    
    /// <summary>Genopretter 20 energi gennem modtagerens kontrollerede metode.</summary>
    public string Heal(Hero hero)
    {
        ArgumentNullException.ThrowIfNull(hero);
        // Heal har en modtager; UseSignatureMove har ingen og er kun en beskrivelse.
        hero.RestoreEnergy(20);
        return $"{Name} hjælper {hero.Name} som nu har fået {hero.EnergyLevel} energi";
    }
}