using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceTrader.Core;

public sealed class CrewMember
{
    public string Name { get; set; } = "";
    public int Pilot { get; set; }
    public int Fighter { get; set; }
    public int Trader { get; set; }
    public int Engineer { get; set; }
    public int System { get; set; }
    public int Home { get; set; }
    public bool Hired { get; set; }
    public CrewRole Role { get; set; }
    public int Wage => (Pilot + Fighter + Trader + Engineer) * 3;
}

public readonly record struct TradeQuote(int Average, int? Difference, int? Percent, bool WorthBuying);

public sealed class Encounter
{
    public EncounterKind Kind { get; set; }
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public int Ship { get; set; }
    public int Hull { get; set; }
    public int MaxHull { get; set; }
    public int Shield { get; set; }
    public int MaxShield { get; set; }
    public int Power { get; set; }
    public int Pilot { get; set; }
    public int Fighter { get; set; }
    public int[] Cargo { get; set; } = new int[Catalog.Goods.Length];
    public int TradeGood { get; set; } = -1;
    public int TradeQty { get; set; }
    public int TradePrice { get; set; }
    public int Bribe { get; set; }
    public int Fine { get; set; }
    public bool Hostile { get; set; }
    public bool Fleeing { get; set; }
    public bool Disabled { get; set; }
    public bool Destroyed { get; set; }
    public bool Scored { get; set; }
    public List<string> Notes { get; set; } = [];
}

public sealed partial class GameState
{
    static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [JsonIgnore] public Random Rng { get; set; } = new();
    [JsonIgnore] public bool EncountersEnabled { get; set; } = true;

    public string CommanderName { get; set; } = "";
    public Difficulty Difficulty { get; set; }
    public int Pilot { get; set; }
    public int Fighter { get; set; }
    public int Trader { get; set; }
    public int Engineer { get; set; }
    public int Cash { get; set; }
    public int Debt { get; set; }
    public int Days { get; set; }
    public int PoliceScore { get; set; }
    public int Reputation { get; set; }
    public bool Insurance { get; set; }
    public int NoClaim { get; set; }
    public int Ship { get; set; } = 1;
    public int Hull { get; set; }
    public int Fuel { get; set; }
    public int ShieldCharge { get; set; }
    public bool EscapePod { get; set; }
    public int[] Weapons { get; set; } = [-1, -1, -1];
    public int[] Shields { get; set; } = [-1, -1, -1];
    public int[] Gadgets { get; set; } = [-1, -1, -1];
    public int[] Cargo { get; set; } = new int[Catalog.Goods.Length];
    public int[] Paid { get; set; } = new int[Catalog.Goods.Length];
    public int[] Buy { get; set; } = new int[Catalog.Goods.Length];
    public int[] Sell { get; set; } = new int[Catalog.Goods.Length];
    public int Tribbles { get; set; }
    public bool Artifact { get; set; }
    public bool MonsterAlive { get; set; } = true;
    public int JaporiQuest { get; set; }
    public int JaporiDest { get; set; } = -1;
    public int ArtifactQuest { get; set; }
    public int ArtifactDest { get; set; } = -1;
    public int JarekQuest { get; set; }
    public int JarekDest { get; set; } = -1;
    public int Current { get; set; }
    public int Destination { get; set; } = -1;
    public bool Traveling { get; set; }
    public int Clicks { get; set; }
    public bool Raided { get; set; }
    public bool Inspected { get; set; }
    public bool PodEscape { get; set; }
    public GameEnd End { get; set; }
    public string Banner { get; set; } = "";
    public List<string> LogLines { get; set; } = [];
    public List<StarSystem> Systems { get; set; } = [];
    public int[] Wormholes { get; set; } = [];
    public List<CrewMember> Crew { get; set; } = [];
    public Encounter? Encounter { get; set; }

    public ShipSpec Spec => Catalog.Ships[Ship];
    public StarSystem Here => Systems[Current];
    public int CargoBays => Spec.Bays + 5 * CountGadgets(GadgetKind.ExtraCargo);
    public int UsedBays => Cargo.Sum();
    public int FreeBays => Math.Max(0, CargoBays - UsedBays);
    public int CommanderQuarters => Crew.Count(member => member.Hired) + 1;
    public int Worth => Spec.Price + Cash - Debt;
    public int MaxLoan => PoliceScore >= 0 ? Math.Min(25_000, Math.Max(1_000, Worth / 5_000 * 500)) : 500;
    public int InterestDue => Debt <= 0 ? 0 : Math.Max(1, (int)(Debt * Catalog.Interest));
    public int Wages => Crew.Where(member => member.Hired && member.Role == CrewRole.Merc).Sum(member => member.Wage);
    public int WormholeTax => Spec.FuelCost * Catalog.WormDistance;

    public int InsuranceDue => !Insurance
        ? 0
        : Math.Max(1, (int)(BaseWorth(true) * Catalog.InsuranceRate * (100 - NoClaim) / 100));

    public string RecordName => Catalog.RecordName(PoliceScore);
    public string RepName => Catalog.RepName(Reputation);

    public static GameState New(string name, Difficulty difficulty, int pilot, int fighter, int trader, int engineer, int? seed = null)
    {
        var trimmed = name.Trim();
        if (trimmed.Length is < 1 or > 20)
            throw new ArgumentException("Commander name must be 1 to 20 characters.");
        if (new[] { pilot, fighter, trader, engineer }.Any(skill => skill is < 1 or > 10) || pilot + fighter + trader + engineer != 20)
            throw new ArgumentException("Assign each skill from 1 to 10, using exactly 20 points.");

        var rng = seed is null ? new Random() : new Random(seed.Value);
        var gnat = Catalog.Ships[1];
        var game = new GameState
        {
            Rng = rng,
            CommanderName = trimmed,
            Difficulty = difficulty,
            Pilot = pilot,
            Fighter = fighter,
            Trader = trader,
            Engineer = engineer,
            Cash = Catalog.StartCash,
            Ship = 1,
            Hull = gnat.Hull,
            Fuel = gnat.Fuel,
            Weapons = [0, -1, -1],
            MonsterAlive = true
        };

        game.Systems = Galaxy.Generate(rng, out var wormholes);
        game.Wormholes = wormholes;
        game.PlaceCrew();
        game.PlaceEvents();
        game.Current = game.PickStart();
        if (difficulty < Difficulty.Normal)
            game.Here.Event = PortEvent.Lottery;
        foreach (var system in game.Systems)
            game.Restock(system);
        game.Here.Visited = true;
        game.RecalculatePrices();
        game.Say($"Welcome aboard, Commander {game.CommanderName}. Your Gnat is docked at {game.Here.Name} with {Catalog.StartCash:N0} credits.");
        return game;
    }

    public string Save() => JsonSerializer.Serialize(this, Json);

    public static GameState Load(string json)
    {
        var game = JsonSerializer.Deserialize<GameState>(json, Json) ?? throw new InvalidOperationException("Save was empty.");
        game.Rng ??= new Random();
        game.EncountersEnabled = true;
        return game;
    }

    public int Skill(int slot)
    {
        var best = slot switch
        {
            0 => Pilot,
            1 => Fighter,
            2 => Trader,
            _ => Engineer
        };
        foreach (var member in Crew)
        {
            if (!member.Hired)
                continue;
            var value = slot switch
            {
                0 => member.Pilot,
                1 => member.Fighter,
                2 => member.Trader,
                _ => member.Engineer
            };
            if (value > best)
                best = value;
        }

        best += Difficulty switch
        {
            Difficulty.Beginner or Difficulty.Easy => 1,
            Difficulty.Hard or Difficulty.Impossible => -1,
            _ => 0
        };
        best = Math.Max(1, best);
        for (var i = 0; i < Spec.Gadgets; i++)
        {
            if (Gadgets[i] < 0)
                continue;
            var kind = Catalog.Gadgets[Gadgets[i]].Kind;
            var matches = slot switch
            {
                0 => kind is GadgetKind.Navigating or GadgetKind.Cloaking,
                1 => kind == GadgetKind.Targeting,
                3 => kind == GadgetKind.AutoRepair,
                _ => false
            };
            if (matches)
                best += Catalog.SkillBonus;
        }

        return best;
    }

    public int DistanceTo(int system) => Catalog.Distance(Here.X, Here.Y, Systems[system].X, Systems[system].Y);

    public TradeQuote Quote(int systemId, int good)
    {
        var system = Systems[systemId];
        var resource = system.Visited ? system.Resource : Resource.Nothing;
        var average = Catalog.StandardPrice(Catalog.Goods[good], good, system.Tech, system.Size, system.PoliticsInfo, resource);
        if (average <= 0 || Buy[good] <= 0)
            return new TradeQuote(average, null, null, false);

        var difference = average - Buy[good];
        return new TradeQuote(average, difference, 100 * difference / Buy[good], difference > 0 && Here.Stock[good] > 0);
    }

    public int WormholeTarget(int system)
    {
        var index = Array.IndexOf(Wormholes, system);
        return index < 0 ? -1 : Wormholes[(index + 1) % Wormholes.Length];
    }

    public bool IsWormhole(int from, int to) => WormholeTarget(from) == to;

    public int DepartureCost(int dest) => Wages + InsuranceDue + (IsWormhole(Current, dest) ? WormholeTax : 0);

    public string Describe(StarSystem system)
    {
        var resource = system.Resource == Resource.Nothing
            ? ""
            : $" Special resource: {Catalog.ResourceNames[(int)system.Resource]}.";
        return $"{system.Name} is a {Catalog.SizeNames[(int)system.Size].ToLowerInvariant()} {system.PoliticsInfo.Name.ToLowerInvariant()}. Tech is {Catalog.TechNames[(int)system.Tech]}. The system is {Catalog.PressureText[(int)system.Pressure]}.{resource}";
    }

    public int WeaponPower()
    {
        var total = 0;
        for (var i = 0; i < Spec.Weapons; i++)
            if (Weapons[i] >= 0)
                total += Catalog.Weapons[Weapons[i]].Power;
        return total;
    }

    public int ShieldMax()
    {
        var total = 0;
        for (var i = 0; i < Spec.Shields; i++)
            if (Shields[i] >= 0)
                total += Catalog.Shields[Shields[i]].Power;
        return total;
    }

    public int BaseWorth(bool forInsurance)
    {
        var spec = Spec;
        var price = spec.Price * (Tribbles > 0 && !forInsurance ? 1 : 3) / 4;
        price -= (spec.Hull - Hull) * spec.Repair;
        price -= (spec.Fuel - Fuel) * spec.FuelCost;
        for (var i = 0; i < spec.Weapons; i++)
            if (Weapons[i] >= 0)
                price += Catalog.Weapons[Weapons[i]].Price * 3 / 4;
        for (var i = 0; i < spec.Shields; i++)
            if (Shields[i] >= 0)
                price += Catalog.Shields[Shields[i]].Price * 3 / 4;
        for (var i = 0; i < spec.Gadgets; i++)
            if (Gadgets[i] >= 0)
                price += Catalog.Gadgets[Gadgets[i]].Price * 3 / 4;
        return price;
    }

    public int ShipWorth => BaseWorth(false) + Paid.Sum();

    public int GearPrice(int basePrice) => Math.Max(1, basePrice * (100 - Math.Min(Skill(2), 90)) / 100);

    void Say(string message)
    {
        Banner = message;
        LogLines.Add(message);
        if (LogLines.Count > 60)
            LogLines.RemoveAt(0);
    }

    void Note(string message)
    {
        Encounter?.Notes.Add(message);
        Say(message);
    }

    int CountGadgets(GadgetKind kind)
    {
        var count = 0;
        for (var i = 0; i < Spec.Gadgets; i++)
            if (Gadgets[i] >= 0 && Catalog.Gadgets[Gadgets[i]].Kind == kind)
                count++;
        return count;
    }

    bool Has(GadgetKind kind) => CountGadgets(kind) > 0;

    bool HasDisabler()
    {
        for (var i = 0; i < Spec.Weapons; i++)
            if (Weapons[i] >= 0 && Catalog.Weapons[Weapons[i]].Disabling)
                return true;
        return false;
    }

    void Restock(StarSystem system)
    {
        var politics = system.PoliticsInfo;
        for (var i = 0; i < Catalog.Goods.Length; i++)
        {
            var good = Catalog.Goods[i];
            if (!Catalog.Traded(good, i, system.Tech, politics))
            {
                system.Stock[i] = 0;
                continue;
            }

            var qty = ((int)system.Size + 1) * (Rng.Next(9, 14) - Math.Abs((int)good.Top - (int)system.Tech));
            if (i >= (int)Good.Narcotics)
                qty = qty * (5 - (int)Difficulty) / (6 - (int)Difficulty) + 1;
            if (system.Resource == good.Cheap)
                qty = qty * 4 / 3;
            if (system.Resource == good.Dear)
                qty = qty * 3 / 4;
            if (system.Pressure == good.Pressure)
                qty /= 5;
            qty += Rng.Next(10) - Rng.Next(10);
            system.Stock[i] = Math.Max(0, qty);
        }
    }

    void RecalculatePrices()
    {
        var system = Here;
        var politics = system.PoliticsInfo;
        for (var i = 0; i < Catalog.Goods.Length; i++)
        {
            var good = Catalog.Goods[i];
            var price = Catalog.StandardPrice(good, i, system.Tech, system.Size, politics, system.Resource);
            if (price > 0)
            {
                if (good.Pressure == system.Pressure)
                    price = price * 3 / 2;
                var variance = Math.Min(good.Variance, Math.Max(0, price - 1));
                if (variance > 0)
                    price += Rng.Next(-variance, variance + 1);
                price = Math.Max(1, price);
                if (PoliceScore < -5)
                    price = price * 90 / 100;
            }

            Sell[i] = price;
            if (price <= 0 || !Catalog.Traded(good, i, system.Tech, politics))
            {
                Buy[i] = 0;
                continue;
            }

            var buy = price;
            if (PoliceScore < -5)
                buy = buy * 100 / 90;
            buy = buy * (103 + Catalog.MaxSkill - Skill(2)) / 100;
            Buy[i] = buy <= price ? price + 1 : buy;
        }
    }

    int PickStart()
    {
        var fuel = Spec.Fuel;
        for (var attempt = 0; attempt < 4_000; attempt++)
        {
            var system = Systems[Rng.Next(Systems.Count)];
            if (system.Event != PortEvent.None)
                continue;
            if (system.Tech <= TechLevel.PreAgricultural || system.Tech >= TechLevel.HiTech)
                continue;
            var close = Systems.Count(other => other.Id != system.Id && Catalog.Distance(system.X, system.Y, other.X, other.Y) <= fuel);
            if (close >= 3)
                return system.Id;
        }

        return Systems.First(system => system.Event == PortEvent.None).Id;
    }

    void PlaceCrew()
    {
        var used = new int[Systems.Count];
        foreach (var name in Catalog.MercNames)
        {
            int system;
            var guard = 0;
            do
            {
                system = Rng.Next(Systems.Count);
            }
            while (used[system] >= 3 && ++guard < 1_000);

            used[system]++;
            Crew.Add(new CrewMember
            {
                Name = name,
                Pilot = RandomSkill(),
                Fighter = RandomSkill(),
                Trader = RandomSkill(),
                Engineer = RandomSkill(),
                System = system,
                Home = system
            });
        }

        var kravat = Catalog.Kravat;
        Crew.Add(new CrewMember
        {
            Name = "Zeethibal",
            Pilot = 10,
            Fighter = 10,
            Trader = 10,
            Engineer = 10,
            System = kravat,
            Home = kravat,
            Role = CrewRole.Zeethibal
        });
    }

    int RandomSkill() => 1 + Rng.Next(5) + Rng.Next(6);

    void PlaceEvents()
    {
        var acamar = Systems[Catalog.Acamar];
        if (acamar.Event == PortEvent.None)
            acamar.Event = PortEvent.Monster;

        Assign(PortEvent.Moon, system => system.Tech == TechLevel.HiTech);
        Assign(PortEvent.EraseRecord);
        Assign(PortEvent.TrainSkill);
        Assign(PortEvent.SealedCanisters);

        var tribble = Assign(PortEvent.Tribble);
        if (tribble is int tribbleSystem && Distant(tribbleSystem) is int buyer)
        {
            Systems[buyer].Event = PortEvent.TribbleBuyer;
        }

        var japori = Assign(PortEvent.Japori);
        if (japori is int japoriSystem && Distant(japoriSystem) is int clinic)
        {
            JaporiDest = clinic;
            Systems[clinic].Event = PortEvent.JaporiDelivery;
        }

        var artifact = Assign(PortEvent.Artifact);
        if (artifact is int artifactSystem && Distant(artifactSystem) is int museum)
        {
            ArtifactDest = museum;
            Systems[museum].Event = PortEvent.ArtifactDelivery;
        }

        var jarek = Assign(PortEvent.Jarek);
        if (jarek is int jarekSystem && Distant(jarekSystem) is int drop)
        {
            JarekDest = drop;
            Systems[drop].Event = PortEvent.JarekDrop;
            Crew.Add(new CrewMember
            {
                Name = "Jarek",
                Pilot = 3,
                Fighter = 2,
                Trader = 10,
                Engineer = 4,
                System = jarekSystem,
                Home = jarekSystem,
                Role = CrewRole.Jarek
            });
        }
    }

    int? Assign(PortEvent kind, Func<StarSystem, bool>? match = null)
    {
        var options = Systems.Where(system => system.Event == PortEvent.None && system.Id != Catalog.Galvon && (match?.Invoke(system) ?? true)).ToList();
        if (options.Count == 0)
            return null;
        var picked = options[Rng.Next(options.Count)];
        picked.Event = kind;
        return picked.Id;
    }

    int? Distant(int from)
    {
        StarSystem? best = null;
        var bestDistance = int.MaxValue;
        foreach (var system in Systems)
        {
            if (system.Event != PortEvent.None || system.Id == from)
                continue;
            var distance = Catalog.Distance(Systems[from].X, Systems[from].Y, system.X, system.Y);
            if (distance >= 70 && distance < bestDistance)
            {
                best = system;
                bestDistance = distance;
            }
        }

        return best?.Id;
    }

    internal void TestCredit(int cash) => Cash = cash;
    internal void TestDebt(int debt) => Debt = debt;
    internal void TestDock(int system)
    {
        Current = system;
        Fuel = Spec.Fuel;
        Hull = Math.Min(Hull, Spec.Hull);
        RecalculatePrices();
    }
    internal void TestDestroy() => ResolveShipLoss("A test volley finishes the ship.");
}
