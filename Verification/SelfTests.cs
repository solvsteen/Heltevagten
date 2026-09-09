using Heltevagten.Heroes;
using Heltevagten.Incidents;
using Heltevagten.Dispatching;
using Heltevagten.Execeptions;
using Heltevagten.Utilities;
using Heltevagten.Abilities;

namespace Heltevagten.Verification;

/// <summary>
/// Små automatiske adfærdskontroller uden eksterne testpakker.
/// Køres med --self-test og afslutter med fejlkode 1, hvis en kontrol fejler.
/// </summary>
internal static class SelfTests
{
    public static int Run()
    {
        List<(string Name, Action Test)> tests = new List<(string, Action)>()
        {
            ("Startenergi og tilgængelighed", () =>
            {
                Hero hero = new FlyingHero("Sky", 75);
                Check(hero.EnergyLevel == 75 && hero.IsAvailable, "Forkert starttilstand.");
            }),
            ("Ugyldigt navn og startenergi", () =>
            {
                Expect<ArgumentException>(() => new FlyingHero(" ", 10));
                Expect<ArgumentOutOfRangeException>(() => new FlyingHero("Sky", -1));
                Expect<ArgumentOutOfRangeException>(() => new FlyingHero("Sky", 101));
            }),
            ("Energiforbrug og afvisning uden ændring", () =>
            {
                Hero hero = new FlyingHero("Sky", 20);
                hero.UseEnergy(5);
                Expect<InvalidOperationException>(() => hero.UseEnergy(16));
                Expect<ArgumentOutOfRangeException>(() => hero.UseEnergy(0));
                Expect<ArgumentOutOfRangeException>(() => hero.UseEnergy(-1));
                Check(hero.EnergyLevel == 15, "Fejl ændrede energien.");
                hero.UseEnergy(15);
                Check(hero.EnergyLevel == 0, "Nul energi skal være tilladt.");
            }),
            ("Genopladning og overflow-grænse", () =>
            {
                Hero hero = new FlyingHero("Sky", 60);
                hero.RestoreEnergy(20);
                Check(hero.EnergyLevel == 80, "Almindelig genopladning fejlede.");
                hero.RestoreEnergy(30);
                Check(hero.EnergyLevel == 100, "Energi blev ikke begrænset.");
                hero.RestoreEnergy(int.MaxValue);
                Check(hero.EnergyLevel == 100, "Stor genopladning gav overflow.");
                Expect<ArgumentOutOfRangeException>(() => hero.RestoreEnergy(-1));
                Expect<ArgumentOutOfRangeException>(() => hero.RestoreEnergy(0));
            }),
            ("Tre forskellige polymorfe handlinger", () =>
            {
                Hero[] heroes = { new FlyingHero("A", 60), new StrengthHero("A", 60), new HealingHero("A", 60) };
                Check(heroes.Select(hero => hero.UseSignatureMove()).Distinct().Count() == 3,
                    "Handlingerne skal være forskellige.");
                Check(heroes.All(hero => hero.EnergyLevel == 60), "Beskrivelser må ikke bruge energi.");
            }),
            ("Evner og helbredelse", () =>
            {
                Hero target = new FlyingHero("Sky", 90);
                IHealer healer = new HealingHero("Pulse", 80);
                healer.Heal(target);
                Check(target.EnergyLevel == 100, "Helbredelse skal respektere energiloftet.");
                Check(target is IFlyable && target is not ISuperStrong, "Forkert evnefordeling.");
                Expect<ArgumentNullException>(() => healer.Heal(null!));
            }),
            ("Hændelsesvalidering og unik identitet", () =>
            {
                Expect<ArgumentException>(() => new Incident("", "Park", Severity.Low));
                Expect<ArgumentException>(() => new Incident("Kat", " ", Severity.Low));
                Expect<ArgumentOutOfRangeException>(() => new Incident("Kat", "Park", (Severity)99));
                Check(NewIncident().Id != NewIncident().Id, "Identiteter skal være forskellige.");
            }),
            ("Generisk søgning på begge domænetyper", () =>
            {
                Hero[] heroes = { new FlyingHero("Sky", 20), new StrengthHero("Titan", 50) };
                Incident[] incidents = { NewIncident(), new Incident("And", "Havn", Severity.High) };
                Check(SearchTool.FindFirst(heroes, hero => hero.EnergyLevel > 40) == heroes[1], "Forkert helt.");
                Check(SearchTool.FindFirst(incidents, incident => incident.Severity == Severity.High) == incidents[1],
                    "Forkert hændelse.");
                Check(SearchTool.FindFirst(heroes, hero => hero.EnergyLevel > 100) is null, "Intet match skal være null.");
                Check(SearchTool.Filter(incidents, incident => !incident.IsResolved).Count() == 2, "Filter fejlede.");
                Check(SearchTool.FindFirst(Array.Empty<Hero>(), _ => true) is null, "Tom samling fejlede.");
            }),
            ("Strategier vælger forskelligt og håndterer lighed", () =>
            {
                Hero[] heroes = { new FlyingHero("Sky", 60), new StrengthHero("Titan", 90), new HealingHero("Pulse", 90) };
                Incident incident = NewIncident();
                Check(new FirstAvailableStrategy().SelectHero(incident, heroes) == heroes[0], "Forkert første kandidat.");
                Check(new HighestEnergyStrategy().SelectHero(incident, heroes) == heroes[1], "Lighed skal vælge første.");
                heroes[1].SetAvailability(false);
                Check(new HighestEnergyStrategy().SelectHero(incident, heroes) == heroes[2], "Optaget kandidat blev valgt.");
                heroes[0].UseEnergy(60);
                Check(new FirstAvailableStrategy().SelectHero(incident, heroes) == heroes[2], "Nul energi blev valgt.");
            }),
            ("Dubletter og beskyttede samlinger", () =>
            {
                DispatchCenter center = NewCenter();
                Hero hero = new FlyingHero("Sky", 60);
                Incident incident = NewIncident();
                center.RegisterHero(hero);
                center.ReportIncident(incident);
                Expect<InvalidOperationException>(() => center.RegisterHero(hero));
                Expect<InvalidOperationException>(() => center.ReportIncident(incident));
                Check(center.Heroes is not List<Hero>, "Den private liste blev eksponeret.");
                Expect<NotSupportedException>(() => ((ICollection<Hero>)center.Heroes).Clear());
            }),
            ("Tildeling og dobbeltbooking", () =>
            {
                DispatchCenter center = NewCenter();
                Hero hero = new FlyingHero("Sky", 60);
                Incident first = NewIncident();
                Incident second = NewIncident();
                center.RegisterHero(hero);
                center.ReportIncident(first);
                center.ReportIncident(second);
                Check(center.DispatchHero(first) == hero, "Forkert returneret helt.");
                Check(!hero.IsAvailable && hero.EnergyLevel == 59 && first.AssignedHero == hero, "Forkert tildeling.");
                Expect<HeroUnavailableException>(() => center.AssignHero(hero, second));
                Expect<NoSuitableHeroFoundException>(() => center.DispatchHero(second));
                Check(second.AssignedHero is null && hero.EnergyLevel == 59, "Fejlet tildeling ændrede tilstanden.");
            }),
            ("Ukendte objekter og allerede tildelte hændelser", () =>
            {
                DispatchCenter center = NewCenter();
                Hero hero = new FlyingHero("Sky", 60);
                Hero other = new StrengthHero("Titan", 90);
                Incident incident = NewIncident();
                center.RegisterHero(hero);
                center.ReportIncident(incident);
                Expect<InvalidOperationException>(() => center.AssignHero(other, incident));
                Expect<InvalidOperationException>(() => center.DispatchHero(NewIncident()));
                center.DispatchHero(incident);
                Expect<InvalidOperationException>(() => center.DispatchHero(incident));
            }),
            ("Tom vagtcentral og helt uden energi", () =>
            {
                DispatchCenter center = NewCenter();
                Incident incident = NewIncident();
                center.ReportIncident(incident);
                Expect<NoSuitableHeroFoundException>(() => center.DispatchHero(incident));
                Hero hero = new FlyingHero("Sky", 0);
                center.RegisterHero(hero);
                Expect<HeroUnavailableException>(() => center.AssignHero(hero, incident));
                Expect<NoSuitableHeroFoundException>(() => center.DispatchHero(incident));
            }),
            ("Løsning, callback og genbrug af helt", () =>
            {
                DispatchCenter center = NewCenter();
                Hero hero = new FlyingHero("Sky", 60);
                Incident first = NewIncident();
                Incident second = NewIncident();
                center.RegisterHero(hero);
                center.ReportIncident(first);
                center.ReportIncident(second);
                Expect<InvalidOperationException>(() => center.ResolveIncident(first, _ => { }));
                center.DispatchHero(first);
                int calls = 0;
                center.ResolveIncident(first, resolved =>
                {
                    calls++;
                    Check(resolved == first && resolved.IsResolved && hero.IsAvailable, "Callback kom for tidligt.");
                });
                center.DispatchHero(second);
                Expect<InvalidOperationException>(() => center.ResolveIncident(first, _ => calls++));
                Check(calls == 1 && !hero.IsAvailable, "Gammel løsning frigav en ny aktiv tildeling.");
                Check(first.AssignedHero == hero, "Historisk tildeling forsvandt.");
            }),
            ("Callbackfejl efterlader konsistent status", () =>
            {
                DispatchCenter center = NewCenter();
                Hero hero = new FlyingHero("Sky", 60);
                Incident incident = NewIncident();
                center.RegisterHero(hero);
                center.ReportIncident(incident);
                center.DispatchHero(incident);
                Expect<ArgumentNullException>(() => center.ResolveIncident(incident, null!));
                Check(!incident.IsResolved && !hero.IsAvailable, "Null-callback ændrede tilstanden.");
                Expect<InvalidOperationException>(() => center.ResolveIncident(incident,
                    _ => throw new InvalidOperationException("Logfejl")));
                Check(incident.IsResolved && hero.IsAvailable, "Callbackfejl ødelagde domænet.");
            })
        };

        int failures = 0;
        foreach ((string name, Action test) in tests)
        {
            try
            {
                test();
                Console.WriteLine($"OK: {name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.WriteLine($"FEJL: {name}: {exception.Message}");
            }
        }

        Console.WriteLine($"\n{tests.Count - failures}/{tests.Count} kontroller bestået.");
        return failures == 0 ? 0 : 1;
    }

    private static DispatchCenter NewCenter() => new DispatchCenter(new FirstAvailableStrategy());

    private static Incident NewIncident() => new Incident("Kat i træ", "Parken", Severity.Low);

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Expect<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Forventede {typeof(TException).Name}.");
    }
}

