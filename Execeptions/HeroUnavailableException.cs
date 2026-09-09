namespace Heltevagten.Execeptions;

/// <summary>Forsøg på at tildele en optaget helt eller en helt uden energi.</summary>
public class HeroUnavailableException : Exception
{
    /// <summary>Beskriver hvorfor helten ikke kan sendes ud.</summary>
    public HeroUnavailableException(string message) : base(message)
    {
    }
}