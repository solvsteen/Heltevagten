namespace Heltevagten.Abilities;

/// <summary>En evne-kontrakt for typer, der kan flyve.</summary>
public interface IFlyable
{
    /// <summary>Beskriver en flyvehandling.</summary>
    string Fly();
}