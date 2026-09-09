using System.Text;
using Heltevagten.Dispatching;
using Heltevagten.Heroes;
using Heltevagten.Incidents;
using Heltevagten.Verification;
using Heltevagten.Execeptions;
using Heltevagten.Utilities;
using Heltevagten.Abilities;


namespace Heltevagten.UI;

/// <summary>Styrer arbejdsgangen. ConsoleUi står for visning og input; domæneklasserne håndhæver reglerne.</summary>
internal static class ConsoleApplication
{
    /// <summary>Starter menuen eller testkontrollerne og returnerer programmets exitkode.</summary>
    public static int Run(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Contains("--self-test"))
        {
            return SelfTests.Run();
        }

        try
        {
            RunMenu();
            return 0;
        }
        catch (EndOfStreamException)
        {
            // Lukket input er en normal afslutning, også midt i en menu.
            return 0;
        }
        catch (Exception exception)
        {
            // Sidste værn ved konsolgrænsen. Forventede menufejl håndteres nedenfor.
            ConsoleUi.Error($"Programmet kunne ikke fortsætte: {exception.Message}");
            return 1;
        }
    }

    private static void RunMenu()
    {
        ConsoleUi.ShowHeader("Vælg tildelingsstrategi for denne session.");
        int strategyChoice = ConsoleUi.Choice("Hvordan skal helte vælges?",
            (1, "Første ledige"), (2, "Højeste energi"));

        // Begge konkrete strategier kan gives til samme constructor.
        // Valget gælder hele sessionen; vi opretter ikke nye centrale data undervejs.
        IDispatchStrategy strategy = strategyChoice == 1
            ? new FirstAvailableStrategy()
            : new HighestEnergyStrategy();
        DispatchCenter center = new DispatchCenter(strategy);
        string strategyName = strategyChoice == 1 ? "første ledige" : "højeste energi";
        center.RegisterHero(new FlyingHero("Skybolt", 60));
        center.RegisterHero(new StrengthHero("Titan", 90));
        center.RegisterHero(new HealingHero("Pulse", 70));

        while (true)
        {
            try
            {
                ConsoleUi.ShowDashboard(center, strategyName);
                string choice = ConsoleUi.MainMenu();
                if (choice == "0")
                {
                    return;
                }

                switch (choice)
                {
                    case "1":
                        ConsoleUi.ShowStatus(center);
                        break;
                    case "2":
                        RegisterHero(center);
                        break;
                    case "3":
                        string description = ConsoleUi.ReadText("Beskrivelse: ");
                        string location = ConsoleUi.ReadText("Placering: ");
                        int severity = ConsoleUi.Choice("Alvorlighed", (0, "Lav"), (1, "Mellem"), (2, "Høj"));
                        center.ReportIncident(new Incident(description, location, (Severity)severity));
                        ConsoleUi.Message("Hændelsen blev indmeldt.");
                        break;
                    case "4":
                        Hero dispatchedHero = center.DispatchHero(ChooseIncident(center));
                        ConsoleUi.Message($"{dispatchedHero.Name} blev sendt ud.");
                        ConsoleUi.Message(dispatchedHero.UseSignatureMove());
                        break;
                    case "5":
                        Incident incident = ChooseIncident(center);
                        Hero hero = ChooseHero(center);
                        center.AssignHero(hero, incident);
                        ConsoleUi.Message($"{hero.Name} blev sendt ud.");
                        ConsoleUi.Message(hero.UseSignatureMove());
                        break;
                    case "6":
                        center.ResolveIncident(ChooseIncident(center), LogResolvedIncident);
                        break;
                    case "7":
                        Hero restingHero = ChooseHero(center);
                        int amount = ConsoleUi.ReadNumber("Energi der skal tilføjes: ", 1, int.MaxValue);
                        restingHero.RestoreEnergy(amount);
                        ConsoleUi.Message($"{restingHero.Name} har nu {restingHero.EnergyLevel} energi.");
                        break;
                    case "8":
                        Search(center);
                        break;
                    case "9":
                        UseAbility(center);
                        break;
                    case "10":
                        ResolveAll(center);
                        break;
                    default:
                        ConsoleUi.Message("Vælg et tal fra 0 til 10.");
                        break;
                }
            }
            catch (HeroUnavailableException exception)
            {
                ConsoleUi.Error($"Helten kan ikke tildeles: {exception.Message}");
            }
            catch (NoSuitableHeroFoundException exception)
            {
                ConsoleUi.Error($"Tildeling mislykkedes: {exception.Message}");
            }
            catch (ArgumentException exception)
            {
                ConsoleUi.Error($"Ugyldig indtastning: {exception.Message}");
            }
            catch (InvalidOperationException exception)
            {
                ConsoleUi.Error($"Handlingen kunne ikke udføres: {exception.Message}");
            }
            catch (EndOfStreamException)
            {
                return;
            }
            catch (Exception exception)
            {
                ConsoleUi.Error($"Uventet fejl: {exception.Message}");
            }

            ConsoleUi.Pause();
        }
    }

    /// <summary>Søger i de faktiske registreringer med samme generiske metode.</summary>
    private static void Search(DispatchCenter center)
    {
        int kind = ConsoleUi.Choice("Søgning", (1, "Find helt med evne"), (2, "Find uløste hændelser"));
        if (kind == 1)
        {
            int ability = ConsoleUi.Choice("Evne", (1, "Flyvning"), (2, "Superstyrke"), (3, "Helbredelse"));
            Hero? match = SearchTool.FindFirst(center.Heroes, hero =>
                hero.IsAvailable && hero.EnergyLevel > 0 &&
                ((ability == 1 && hero is IFlyable) ||
                 (ability == 2 && hero is ISuperStrong) ||
                 (ability == 3 && hero is IHealer)));
            ConsoleUi.Message(match is null
                ? "Ingen ledig helt med energi og den valgte evne."
                : $"Første egnede helt: {match.Name} ({match.EnergyLevel} energi).");
        }
        else
        {
            Severity severity = (Severity)ConsoleUi.Choice("Alvorlighed", (0, "Lav"), (1, "Mellem"), (2, "Høj"));
            // Func beskriver søgereglen; T bliver her Incident i stedet for Hero.
            Func<Incident, bool> predicate = incident => !incident.IsResolved && incident.Severity == severity;
            Incident? first = SearchTool.FindFirst(center.Incidents, predicate);
            if (first is null)
            {
                ConsoleUi.Message("Ingen uløste hændelser med den alvorlighed.");
                return;
            }

            ConsoleUi.Message($"Første match: {first.Description}");
            ConsoleUi.ShowIncidents(SearchTool.Filter(center.Incidents, predicate));
        }
    }

    /// <summary>Bruger evner på den valgte helt fra vagtcentralens rigtige samling.</summary>
    private static void UseAbility(DispatchCenter center)
    {
        Hero hero = ChooseHero(center);
        int action = ConsoleUi.Choice("Handling", (1, "Signaturhandling"), (2, "Særlig evne"));
        if (action == 1)
        {
            // Polymorfi: variablen er Hero, men den konkrete override kaldes.
            ConsoleUi.Message(hero.UseSignatureMove());
            return;
        }

        // Typekontrollerne spørger efter evner frem for konkrete heltetyper.
        // En kommende helt med flere interfaces kan bruge flere af disse evner.
        if (hero is IFlyable flyer)
        {
            ConsoleUi.Message(flyer.Fly());
        }
        if (hero is ISuperStrong strongHero)
        {
            ConsoleUi.Message(strongHero.LiftHeavyObject());
        }
        if (hero is IHealer healer)
        {
            ConsoleUi.Message("Vælg helten, der skal modtage helbredelse:");
            ConsoleUi.Message(healer.Heal(ChooseHero(center)));
        }
    }

    /// <summary>Løser alle aktive opgaver efter brugerens bekræftelse.</summary>
    private static void ResolveAll(DispatchCenter center)
    {
        List<Incident> activeIncidents = SearchTool.Filter(center.Incidents,
            incident => !incident.IsResolved && incident.AssignedHero is not null).ToList();
        if (activeIncidents.Count == 0)
        {
            ConsoleUi.Message("Der er ingen aktive, tildelte hændelser.");
            return;
        }

        ConsoleUi.Message($"{activeIncidents.Count} aktive hændelser kan afsluttes.");
        if (ConsoleUi.Choice("Afslut alle aktive hændelser?", (0, "Annuller"), (1, "Ja, afslut dem")) == 0)
        {
            return;
        }

        int resolvedCount = 0;
        foreach (Incident incident in activeIncidents)
        {
            // Her er callbacket en lambda. Ved enkeltvis løsning bruges
            // den navngivne LogResolvedIncident-metode i menupunkt 6.
            center.ResolveIncident(incident, resolved =>
            {
                resolvedCount++;
                ConsoleUi.Message($"Afsluttet: {resolved.Description}. {resolved.AssignedHero?.Name} er ledig igen.");
            });
        }

        ConsoleUi.Message($"{resolvedCount} hændelser er afsluttet.");
    }

    private static void RegisterHero(DispatchCenter center)
    {
        int type = ConsoleUi.Choice("Heltetype", (1, "Flyvende"), (2, "Stærk"), (3, "Helbredende"));
        string name = ConsoleUi.ReadText("Navn: ");
        int energy = ConsoleUi.ReadNumber("Startenergi (0–100): ", 0, 100);

        Hero hero;
        switch (type)
        {
            case 1:
                hero = new FlyingHero(name, energy);
                break;
            case 2:
                hero = new StrengthHero(name, energy);
                break;
            default:
                hero = new HealingHero(name, energy);
                break;
        }

        center.RegisterHero(hero);
        ConsoleUi.Message($"{hero.Name} er registreret.");
    }

    private static Hero ChooseHero(DispatchCenter center)
    {
        return ConsoleUi.Choose("Vælg helt", center.Heroes,
            hero => $"{hero.Name} · {hero.EnergyLevel} energi · {(hero.IsAvailable ? "ledig" : "optaget")}");
    }

    private static Incident ChooseIncident(DispatchCenter center)
    {
        return ConsoleUi.Choose("Vælg hændelse", center.Incidents,
            incident => $"{incident.Description} · {incident.Location} · {(incident.IsResolved ? "løst" : "uløst")}");
    }

    private static void LogResolvedIncident(Incident incident)
    {
        ConsoleUi.Message($"Log: '{incident.Description}' er løst. Helten er ledig igen.");
    }
}
