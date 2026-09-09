using Heltevagten.Abilities;
namespace Heltevagten.Heroes;


/// <summary>En konkret helt, der arver fælles regler og implementerer flyveevnen.</summary>
public class FlyingHero : Hero, IFlyable
{
    /// <summary>Sender navn og startenergi videre til basisklassens constructor.</summary>
    public FlyingHero(string name, int energyLevel) : base(name, energyLevel)
    {
        // Hero udfører den fælles validering, så den ikke skal gentages her.
    }

    /// <summary>Heltens egen implementation af den abstrakte signaturhandling.</summary>
    public override string UseSignatureMove()
    {
        return $"{Name} flyver op og redder katten fra træet.";
    }

    /// <summary>Opfylder IFlyable-kontrakten.</summary>
    public string Fly()
    {
        return $"{Name} flyver over byen og spejder efter problemer.";
    }
}