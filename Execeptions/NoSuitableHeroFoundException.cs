namespace Heltevagten;

/// <summary>Strategien kunne ikke finde en ledig helt med energi.</summary>
public class NoSuitableHeroFoundException : Exception
{
    /// <summary>Beskriver den mislykkede søgning.</summary>
    public NoSuitableHeroFoundException(string message) : base(message)
    {
    }
}