using Spectre.Console;
using Heltevagten.Dispatching;
using Heltevagten.Heroes;
using Heltevagten.Incidents;

namespace Heltevagten.UI;

/// <summary>
/// Konsollens visning og input. Spectre bruges kun her, så farver og menuer
/// kan ændres uden at ændre reglerne i Hero eller DispatchCenter.
/// </summary>
internal static class ConsoleUi
{
    // Ved eksempelvis pipet input kan piletaster ikke bruges.
    // Her tilbydes en nummermenu, så programmet også kan afprøves automatisk.
    private static bool IsInteractive =>
        !Console.IsInputRedirected && !Console.IsOutputRedirected &&
        AnsiConsole.Profile.Capabilities.Interactive;

    /// <summary>Viser programmets titel og sessionens strategi.</summary>
    public static void ShowHeader(string subtitle)
    {
        if (IsInteractive)
        {
            AnsiConsole.Clear();
        }

        AnsiConsole.Write(new Panel(
            new Markup("[bold cyan]HELTEVAGTEN[/]\n[grey]Byens digitale vagtcentral[/]"))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .Padding(2, 1)
            .Expand());
        Message(subtitle);
        AnsiConsole.WriteLine();
    }

    /// <summary>Viser sessionens aktuelle data før brugerens næste handling.</summary>
    public static void ShowDashboard(DispatchCenter center, string strategyName)
    {
        ShowHeader($"Tildelingsstrategi: {strategyName}");
        int available = center.Heroes.Count(hero => hero.IsAvailable && hero.EnergyLevel > 0);
        int active = center.Incidents.Count(incident => !incident.IsResolved && incident.AssignedHero is not null);
        int waiting = center.Incidents.Count(incident => !incident.IsResolved && incident.AssignedHero is null);
        int resolved = center.Incidents.Count(incident => incident.IsResolved);

        AnsiConsole.MarkupLine($"[green]{available} klar[/]  ·  [yellow]{active} aktive[/]  ·  " +
            $"[cyan]{waiting} afventer[/]  ·  [grey]{resolved} løst[/]");
        ShowStatus(center);
    }

    /// <summary>Viser hele menuen. Returværdierne svarer til arbejdsgangens switch.</summary>
    public static string MainMenu()
    {
        return Choice("Hvad vil du gøre?", new (int, string)[]
        {
            (1, "Vis oversigt"),
            (2, "Registrer helt"),
            (3, "Indmeld hændelse"),
            (4, "Tildel automatisk"),
            (5, "Tildel bestemt helt"),
            (6, "Løs hændelse"),
            (7, "Genoplad helt"),
            (8, "Søg efter helte og hændelser"),
            (9, "Brug en helts evner"),
            (10, "Løs alle aktive hændelser"),
            (0, "Afslut")
        }).ToString();
    }

    /// <summary>Returnerer nøglen til det valgte punkt; teksten er kun præsentation.</summary>
    public static int Choice(string title, params (int Value, string Label)[] choices)
    {
        if (choices.Length == 0)
        {
            throw new InvalidOperationException("Der er ingen valgmuligheder.");
        }

        if (IsInteractive)
        {
            // SelectionPrompt returnerer et valgt tal. UseConverter bestemmer
            // den tekst, brugeren ser. Escape behandler fx [Titan] som almindelig tekst.
            return AnsiConsole.Prompt(new SelectionPrompt<int> { DefaultValue = choices[0].Value }
                .Title($"[bold]{Markup.Escape(title)}[/]")
                .PageSize(Math.Clamp(AnsiConsole.Profile.Height - 23, 3, 10))
                .HighlightStyle(new Style(Color.Cyan1, decoration: Decoration.Bold))
                .MoreChoicesText("[grey]Brug piletasterne for at se flere valg[/]")
                .UseConverter(value => Markup.Escape(
                    choices.First(choice => choice.Value == value).Label))
                .AddChoices(choices.Select(choice => choice.Value)));
        }

        Message(title);
        foreach ((int value, string label) in choices)
        {
            Message($"{value}: {label}");
        }

        while (true)
        {
            string input = ReadLine("Valg: ");
            if (int.TryParse(input, out int value) && choices.Any(choice => choice.Value == value))
            {
                return value;
            }

            Error("Vælg et af de viste numre.");
        }
    }

    /// <summary>Vælger et objekt gennem et indeks, også hvis to objekter har samme navn.</summary>
    public static T Choose<T>(string title, IEnumerable<T> items, Func<T, string> label)
    {
        List<T> options = items.ToList();
        if (options.Count == 0)
        {
            throw new InvalidOperationException("Der er ingen elementer at vælge imellem.");
        }

        var choices = options.Select((item, index) => (index + 1, label(item))).ToArray();
        int selected = Choice(title, choices);
        return options[selected - 1];
    }

    /// <summary>Indlæser tekst. Domæneklasserne validerer stadig deres egne argumenter.</summary>
    public static string ReadText(string prompt)
    {
        if (IsInteractive)
        {
            return AnsiConsole.Prompt(new TextPrompt<string>(Markup.Escape(prompt))
                .Validate(text => !string.IsNullOrWhiteSpace(text)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]Teksten må ikke være tom.[/]")));
        }

        return ReadLine(prompt);
    }

    /// <summary>Indlæser et heltal og hjælper brugeren med at rette ugyldigt input.</summary>
    public static int ReadNumber(string prompt, int minimum, int maximum)
    {
        if (IsInteractive)
        {
            return AnsiConsole.Prompt(new TextPrompt<int>(Markup.Escape(prompt))
                .ValidationErrorMessage("[red]Indtast et gyldigt heltal.[/]")
                .Validate(number => number >= minimum && number <= maximum
                    ? ValidationResult.Success()
                    : ValidationResult.Error($"[red]Vælg fra {minimum} til {maximum}.[/]")));
        }

        while (true)
        {
            if (int.TryParse(ReadLine(prompt), out int number) && number >= minimum && number <= maximum)
            {
                return number;
            }

            Error($"Indtast et heltal fra {minimum} til {maximum}.");
        }
    }

    /// <summary>Viser farvede tabeller over samlingerne uden at ændre deres indhold.</summary>
    public static void ShowStatus(DispatchCenter center)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[cyan]HELTE[/]").LeftJustified());
        Table heroes = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        heroes.AddColumn("Navn");
        heroes.AddColumn("Energi");
        heroes.AddColumn("Status");

        foreach (Hero hero in center.Heroes)
        {
            string energyColor = hero.EnergyLevel > 50 ? "green" : hero.EnergyLevel > 0 ? "yellow" : "red";
            string bar = new string('■', hero.EnergyLevel / 10).PadRight(10, '·');
            string status = !hero.IsAvailable ? "[yellow]Optaget[/]"
                : hero.EnergyLevel == 0 ? "[red]Mangler energi[/]" : "[green]Ledig[/]";
            heroes.AddRow(Markup.Escape(hero.Name), $"[{energyColor}]{bar} {hero.EnergyLevel}/100[/]", status);
        }

        AnsiConsole.Write(heroes);
        AnsiConsole.Write(new Rule("[cyan]HÆNDELSER[/]").LeftJustified());
        ShowIncidents(center.Incidents);
    }

    /// <summary>Viser alle hændelser fra en samling, eksempelvis søgeresultater.</summary>
    public static void ShowIncidents(IEnumerable<Incident> incidents)
    {
        List<Incident> rows = incidents.ToList();
        if (rows.Count == 0)
        {
            Message("Ingen hændelser at vise.");
            return;
        }

        Table table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        table.AddColumn("Hændelse");
        table.AddColumn("Sted");
        table.AddColumn("Alvorlighed");
        table.AddColumn("Status / helt");
        foreach (Incident incident in rows)
        {
            string severity = incident.Severity switch
            {
                Severity.High => "[red]Høj[/]",
                Severity.Medium => "[yellow]Mellem[/]",
                _ => "[green]Lav[/]"
            };
            string status = incident.IsResolved ? "[green]Løst[/]"
                : incident.AssignedHero is null ? "[cyan]Afventer[/]" : "[yellow]Aktiv[/]";
            table.AddRow(Markup.Escape(incident.Description), Markup.Escape(incident.Location),
                severity, $"{status}\n{Markup.Escape(incident.AssignedHero?.Name ?? "Ingen helt")}");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Almindelige beskeder bruger Text frem for Markup, så brugerens parenteser
    /// og eventuelle markup-tegn ikke fortolkes som farvekoder.
    /// </summary>
    public static void Message(string message)
    {
        AnsiConsole.Write(new Text(message + Environment.NewLine));
    }

    /// <summary>Fremhæver en fejl uden at udskrive stacktrace til brugeren.</summary>
    public static void Error(string message)
    {
        AnsiConsole.Write(new Panel(new Text(message))
            .Header("[red]Handlingen kunne ikke udføres[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Red));
    }

    /// <summary>Lader brugeren læse resultatet, før næste dashboard tegnes.</summary>
    public static void Pause()
    {
        if (IsInteractive)
        {
            AnsiConsole.MarkupLine("[grey]Tryk på en tast for at fortsætte …[/]");
            Console.ReadKey(true);
        }
    }

    private static string ReadLine(string prompt)
    {
        AnsiConsole.Write(new Text(prompt));
        return Console.ReadLine() ?? throw new EndOfStreamException();
    }
}
