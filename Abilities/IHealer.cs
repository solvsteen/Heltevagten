using Heltevagten.Heroes;

namespace Heltevagten.Abilities;

/// <summary>En evne-kontrakt for typer, der kan genoprette andres energi.</summary>
public interface IHealer
{
    /// <summary>Genopretter energi hos den valgte helt og beskriver handlingen.</summary>
    string Heal(Hero hero);
}