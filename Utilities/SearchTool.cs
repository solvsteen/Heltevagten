namespace Heltevagten.Utilities;

/// <summary>Genbrugelig søgning, der hverken kender Hero eller Incident.</summary>
public static class SearchTool
{
    /// <summary>
    /// Returnerer det første match eller null. T erstattes af den anvendte type.
    /// where T : class begrænser søgningen til referencetyper, så null er entydigt.
    /// </summary>
    public static T? FindFirst<T>(IEnumerable<T> items, Func<T, bool> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (T item in items)
        {
            // Predicate er en funktion modtaget udefra: ét element ind, true/false ud.
            if (predicate(item))
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>Returnerer alle matches som en ny liste via IEnumerable.</summary>
    public static IEnumerable<T> Filter<T>(IEnumerable<T> items, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(predicate);

        List<T> matches = new List<T>();
        foreach (T item in items)
        {
            if (predicate(item))
            {
                matches.Add(item);
            }
        }

        return matches;
    }
}

