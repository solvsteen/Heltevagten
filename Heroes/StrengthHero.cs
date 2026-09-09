using Heltevagten.Abilities;
namespace Heltevagten.Heroes;

/// <summary>En konkret helt med superstyrke.</summary>
public class StrengthHero : Hero, ISuperStrong
{
    /// <summary>Opretter en stærk helt med basisklassens validering.</summary>
    public StrengthHero(string name, int energyLevel) : base(name, energyLevel)
    {
    }

    /// <summary>Override gør, at denne handling kaldes gennem en Hero-reference.</summary>
    public override string UseSignatureMove()
    {
        return $"{Name} løfter den kæmpestore gummibåd fri af havnen.";
    }

    /// <summary>Opfylder ISuperStrong-kontrakten.</summary>
    public string LiftHeavyObject()
    {
        return $"{Name} løfter en tung forhindring væk fra vejen.";
    }
}