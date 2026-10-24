namespace SpaceTrader.Core;

// Tables and price rules from Space Trader for Windows 2.01 (GPL-2.0-or-later),
// itself a port of Pieter Spronck's Palm game.

public enum Difficulty { Beginner, Easy, Normal, Hard, Impossible }

public enum TechLevel
{
    PreAgricultural, Agricultural, Medieval, Renaissance,
    EarlyIndustrial, Industrial, PostIndustrial, HiTech, Unavailable
}

public enum Size { Tiny, Small, Medium, Large, Huge }

public enum Activity
{
    Absent, Minimal, Few, Some, Moderate, Many, Abundant, Swarms, NA = 100
}

public enum Pressure { None, War, Plague, Drought, Boredom, Cold, CropFailure, Employment }

public enum Resource
{
    NA = -1, Nothing, MineralRich, MineralPoor, Desert, SweetOceans, RichSoil, PoorSoil,
    RichFauna, Lifeless, WeirdMushrooms, SpecialHerbs, Artistic, Warlike
}

public enum Good
{
    Water, Furs, Food, Ore, Games, Firearms, Medicine, Machines, Narcotics, Robots
}

public enum GadgetKind { ExtraCargo, AutoRepair, Navigating, Targeting, Cloaking }

public enum PortEvent
{
    None, Lottery, Moon, EraseRecord, TrainSkill, Tribble, TribbleBuyer,
    Japori, JaporiDelivery, Artifact, ArtifactDelivery, Jarek, JarekDrop,
    Monster, SealedCanisters
}

public enum EncounterKind { Pirate, Police, Trader, Monster }

public enum GameEnd { Playing, Retired, Killed }

public enum CrewRole { Merc, Zeethibal, Jarek }

public sealed record TradeGood(
    string Name, TechLevel Produces, TechLevel Uses, TechLevel Top,
    int PriceLow, int PriceInc, int Variance, Pressure Pressure,
    Resource Cheap, Resource Dear, bool Illegal);

public sealed record Politics(
    string Name, int Reaction, Activity Police, Activity Pirates, Activity Traders,
    TechLevel MinTech, TechLevel MaxTech, int Bribe, bool Drugs, bool Guns, int Wanted);

public sealed record ShipSpec(
    string Name, Size Size, int Bays, int Weapons, int Shields, int Gadgets, int Crew,
    int Fuel, int FuelCost, int Hull, int Repair, int Price, int Occurrence,
    Activity Police, Activity Pirates, Activity Traders, TechLevel MinTech);

public sealed record WeaponSpec(string Name, int Power, bool Disabling, int Price, TechLevel Tech);

public sealed record ShieldSpec(string Name, int Power, int Price, TechLevel Tech);

public sealed record GadgetSpec(string Name, string Blurb, int Price, TechLevel Tech, GadgetKind Kind);

public static partial class Catalog
{
    public const int GalaxyWidth = 154;
    public const int GalaxyHeight = 110;
    public const int MinDistance = 7;
    public const int CloseDistance = 13;
    public const int WormDistance = 25;
    public const int SkillBonus = 3;
    public const int MaxSkill = 10;
    public const int StartCash = 1000;
    public const int PodPrice = 2000;
    public const int PodTransfer = 200;
    public const int FleaCost = 500;
    public const int DebtWarning = 75_000;
    public const int DebtTooLarge = 100_000;
    public const int MoonPrice = 500_000;
    public const double Interest = 0.10;
    public const double InsuranceRate = 0.0025;
    public const int MaxNoClaim = 90;
    public const int TravelClicks = 20;

    public static readonly string[] TechNames =
    [
        "Pre-agricultural", "Agricultural", "Medieval", "Renaissance",
        "Early industrial", "Industrial", "Post-industrial", "Hi-tech"
    ];

    public static readonly string[] SizeNames = ["Tiny", "Small", "Medium", "Large", "Huge"];

    public static readonly string[] PressureText =
    [
        "under no particular pressure",
        "at war",
        "ravaged by a plague",
        "suffering from a drought",
        "suffering from extreme boredom",
        "suffering from a cold spell",
        "suffering from a crop failure",
        "lacking enough workers"
    ];

    public static readonly string[] ResourceNames =
    [
        "nothing special", "mineral rich", "mineral poor", "desert", "sweetwater oceans",
        "rich soil", "poor soil", "rich fauna", "lifeless", "weird mushrooms",
        "special herbs", "artistic", "warlike"
    ];

    public static readonly string[] RecordNames =
    [
        "Psychopath", "Villain", "Criminal", "Crook", "Dubious",
        "Clean", "Lawful", "Trusted", "Liked", "Hero"
    ];

    public static readonly int[] RecordScores = [-100, -70, -30, -10, -5, 0, 5, 10, 25, 75];

    public static readonly string[] RepNames =
    [
        "Harmless", "Mostly harmless", "Poor", "Average", "Above average",
        "Competent", "Dangerous", "Deadly", "Elite"
    ];

    public static readonly int[] RepScores = [0, 10, 20, 40, 80, 150, 300, 600, 1500];

    public static readonly TradeGood[] Goods =
    [
        new("Water", TechLevel.PreAgricultural, TechLevel.PreAgricultural, TechLevel.Medieval, 30, 3, 4, Pressure.Drought, Resource.SweetOceans, Resource.Desert, false),
        new("Furs", TechLevel.PreAgricultural, TechLevel.PreAgricultural, TechLevel.PreAgricultural, 250, 10, 10, Pressure.Cold, Resource.RichFauna, Resource.Lifeless, false),
        new("Food", TechLevel.Agricultural, TechLevel.PreAgricultural, TechLevel.Agricultural, 100, 5, 5, Pressure.CropFailure, Resource.RichSoil, Resource.PoorSoil, false),
        new("Ore", TechLevel.Medieval, TechLevel.Medieval, TechLevel.Renaissance, 350, 20, 10, Pressure.War, Resource.MineralRich, Resource.MineralPoor, false),
        new("Games", TechLevel.Renaissance, TechLevel.Agricultural, TechLevel.PostIndustrial, 250, -10, 5, Pressure.Boredom, Resource.Artistic, Resource.NA, false),
        new("Firearms", TechLevel.Renaissance, TechLevel.Agricultural, TechLevel.Industrial, 1250, -75, 100, Pressure.War, Resource.Warlike, Resource.NA, true),
        new("Medicine", TechLevel.EarlyIndustrial, TechLevel.Agricultural, TechLevel.PostIndustrial, 650, -20, 10, Pressure.Plague, Resource.SpecialHerbs, Resource.NA, false),
        new("Machines", TechLevel.EarlyIndustrial, TechLevel.Renaissance, TechLevel.Industrial, 900, -30, 5, Pressure.Employment, Resource.NA, Resource.NA, false),
        new("Narcotics", TechLevel.Industrial, TechLevel.PreAgricultural, TechLevel.Industrial, 3500, -125, 150, Pressure.Boredom, Resource.WeirdMushrooms, Resource.NA, true),
        new("Robots", TechLevel.PostIndustrial, TechLevel.EarlyIndustrial, TechLevel.HiTech, 5000, -150, 100, Pressure.Employment, Resource.NA, Resource.NA, false)
    ];

    public static readonly Politics[] Politics =
    [
        new("Anarchy", 0, Activity.Absent, Activity.Swarms, Activity.Minimal, TechLevel.PreAgricultural, TechLevel.Industrial, 7, true, true, (int)Good.Food),
        new("Capitalist State", 2, Activity.Some, Activity.Few, Activity.Swarms, TechLevel.EarlyIndustrial, TechLevel.HiTech, 1, true, true, (int)Good.Ore),
        new("Communist State", 6, Activity.Abundant, Activity.Moderate, Activity.Moderate, TechLevel.Agricultural, TechLevel.Industrial, 5, true, true, -1),
        new("Confederacy", 5, Activity.Moderate, Activity.Some, Activity.Many, TechLevel.Agricultural, TechLevel.PostIndustrial, 3, true, true, (int)Good.Games),
        new("Corporate State", 2, Activity.Abundant, Activity.Few, Activity.Swarms, TechLevel.EarlyIndustrial, TechLevel.HiTech, 2, true, true, (int)Good.Robots),
        new("Cybernetic State", 0, Activity.Swarms, Activity.Swarms, Activity.Many, TechLevel.PostIndustrial, TechLevel.HiTech, 0, false, false, (int)Good.Ore),
        new("Democracy", 4, Activity.Some, Activity.Few, Activity.Many, TechLevel.Renaissance, TechLevel.HiTech, 2, true, true, (int)Good.Games),
        new("Dictatorship", 3, Activity.Moderate, Activity.Many, Activity.Some, TechLevel.PreAgricultural, TechLevel.HiTech, 2, true, true, -1),
        new("Fascist State", 7, Activity.Swarms, Activity.Swarms, Activity.Minimal, TechLevel.EarlyIndustrial, TechLevel.HiTech, 0, false, true, (int)Good.Machines),
        new("Feudal State", 1, Activity.Minimal, Activity.Abundant, Activity.Few, TechLevel.PreAgricultural, TechLevel.Renaissance, 6, true, true, (int)Good.Firearms),
        new("Military State", 7, Activity.Swarms, Activity.Absent, Activity.Abundant, TechLevel.Medieval, TechLevel.HiTech, 0, false, true, (int)Good.Robots),
        new("Monarchy", 3, Activity.Moderate, Activity.Some, Activity.Moderate, TechLevel.PreAgricultural, TechLevel.Industrial, 4, true, true, (int)Good.Medicine),
        new("Pacifist State", 7, Activity.Few, Activity.Minimal, Activity.Many, TechLevel.PreAgricultural, TechLevel.Renaissance, 1, true, false, -1),
        new("Socialist State", 4, Activity.Few, Activity.Many, Activity.Some, TechLevel.PreAgricultural, TechLevel.Industrial, 6, true, true, -1),
        new("State of Satori", 0, Activity.Minimal, Activity.Minimal, Activity.Minimal, TechLevel.PreAgricultural, TechLevel.Agricultural, 0, false, false, -1),
        new("Technocracy", 1, Activity.Abundant, Activity.Some, Activity.Abundant, TechLevel.EarlyIndustrial, TechLevel.HiTech, 2, true, true, (int)Good.Water),
        new("Theocracy", 5, Activity.Abundant, Activity.Minimal, Activity.Moderate, TechLevel.PreAgricultural, TechLevel.EarlyIndustrial, 0, true, true, (int)Good.Narcotics)
    ];

    public static readonly ShipSpec[] Ships =
    [
        new("Flea", Size.Tiny, 10, 0, 0, 0, 1, 20, 1, 25, 1, 2000, 2, Activity.NA, Activity.NA, Activity.Absent, TechLevel.EarlyIndustrial),
        new("Gnat", Size.Small, 15, 1, 0, 1, 1, 14, 1, 100, 2, 10000, 28, Activity.Absent, Activity.Absent, Activity.Absent, TechLevel.Industrial),
        new("Firefly", Size.Small, 20, 1, 1, 1, 1, 17, 1, 100, 3, 25000, 20, Activity.Absent, Activity.Absent, Activity.Absent, TechLevel.Industrial),
        new("Mosquito", Size.Small, 15, 2, 1, 1, 1, 13, 1, 100, 5, 30000, 20, Activity.Absent, Activity.Minimal, Activity.Absent, TechLevel.Industrial),
        new("Bumblebee", Size.Medium, 25, 1, 2, 2, 2, 15, 1, 100, 7, 60000, 15, Activity.Minimal, Activity.Minimal, Activity.Absent, TechLevel.Industrial),
        new("Beetle", Size.Medium, 50, 0, 1, 1, 3, 14, 1, 50, 10, 80000, 3, Activity.NA, Activity.NA, Activity.Absent, TechLevel.Industrial),
        new("Hornet", Size.Large, 20, 3, 2, 1, 2, 16, 2, 150, 15, 100000, 6, Activity.Few, Activity.Some, Activity.Minimal, TechLevel.PostIndustrial),
        new("Grasshopper", Size.Large, 30, 2, 2, 3, 3, 15, 3, 150, 15, 150000, 2, Activity.Some, Activity.Moderate, Activity.Few, TechLevel.PostIndustrial),
        new("Termite", Size.Huge, 60, 1, 3, 2, 3, 13, 4, 200, 20, 225000, 2, Activity.Moderate, Activity.Many, Activity.Some, TechLevel.HiTech),
        new("Wasp", Size.Huge, 35, 3, 2, 2, 3, 14, 5, 200, 20, 300000, 2, Activity.Many, Activity.Abundant, Activity.Moderate, TechLevel.HiTech),
        new("Space monster", Size.Huge, 0, 3, 0, 0, 1, 1, 1, 500, 1, 500000, 0, Activity.NA, Activity.NA, Activity.NA, TechLevel.Unavailable)
    ];

    public static readonly WeaponSpec[] Weapons =
    [
        new("Pulse laser", 15, false, 2000, TechLevel.Industrial),
        new("Beam laser", 25, false, 12500, TechLevel.PostIndustrial),
        new("Military laser", 35, false, 35000, TechLevel.HiTech),
        new("Photon disruptor", 20, true, 15000, TechLevel.PostIndustrial)
    ];

    public static readonly ShieldSpec[] Shields =
    [
        new("Energy shield", 100, 5000, TechLevel.Industrial),
        new("Reflective shield", 200, 20000, TechLevel.PostIndustrial)
    ];

    public static readonly GadgetSpec[] Gadgets =
    [
        new("5 extra cargo bays", "Five more holds.", 2500, TechLevel.EarlyIndustrial, GadgetKind.ExtraCargo),
        new("Auto-repair system", "Your engineer patches the hull, and is more effective.", 7500, TechLevel.Industrial, GadgetKind.AutoRepair),
        new("Navigating system", "A better pilot when you run.", 15000, TechLevel.PostIndustrial, GadgetKind.Navigating),
        new("Targeting system", "A better gunner.", 25000, TechLevel.PostIndustrial, GadgetKind.Targeting),
        new("Cloaking device", "Patrols and pirates miss you more often.", 100000, TechLevel.HiTech, GadgetKind.Cloaking)
    ];

    public static readonly string[] MercNames =
    [
        "Alyssa", "Armatur", "Bentos", "C2U2", "Chi'Ti", "Crystal", "Dane", "Deirdre", "Doc", "Draco",
        "Iranda", "Jeremiah", "Jujubal", "Krydon", "Luis", "Mercedez", "Milete", "Muri-L", "Mystyc", "Nandi",
        "Orestes", "Pancho", "PS37", "Quarck", "Sosumi", "Uma", "Wesley", "Wonton", "Yorvick",
        "Aragorn", "Brady", "Eight of Nine", "Fangorn", "Gagarin", "Hoshi", "Jackson", "Kaylee", "Marcus",
        "O'Neill", "Ripley", "Stilgar", "Taggart", "Vansen", "Xizor", "Ziyal"
    ];

    public static int Acamar => Array.IndexOf(SystemNames, "Acamar");
    public static int Galvon => Array.IndexOf(SystemNames, "Galvon");
    public static int Kravat => Array.IndexOf(SystemNames, "Kravat");

    public static int IndexOf(int score, int[] thresholds)
    {
        var index = 0;
        for (var i = 0; i < thresholds.Length; i++)
            if (score >= thresholds[i])
                index = i;
        return index;
    }

    public static string RecordName(int score) => RecordNames[IndexOf(score, RecordScores)];
    public static string RepName(int score) => RepNames[IndexOf(score, RepScores)];

    public static int Distance(int x1, int y1, int x2, int y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return (int)Math.Floor(Math.Sqrt(dx * dx + dy * dy));
    }

    public static bool Traded(TradeGood good, int goodIndex, TechLevel tech, Politics politics) =>
        tech >= good.Produces && Legal(good, goodIndex, politics);

    public static bool Used(TradeGood good, int goodIndex, TechLevel tech, Politics politics) =>
        tech >= good.Uses && Legal(good, goodIndex, politics);

    public static bool Legal(TradeGood good, int goodIndex, Politics politics)
    {
        if (goodIndex == (int)Good.Narcotics && !politics.Drugs)
            return false;
        if (goodIndex == (int)Good.Firearms && !politics.Guns)
            return false;
        return true;
    }

    public static int StandardPrice(TradeGood good, int goodIndex, TechLevel tech, Size size, Politics politics, Resource resource)
    {
        if (!Used(good, goodIndex, tech, politics))
            return 0;

        var price = good.PriceLow + (int)tech * good.PriceInc;
        if (politics.Wanted == goodIndex)
            price = price * 4 / 3;
        price = price * (100 - 2 * (int)politics.Traders) / 100;
        price = price * (100 - (int)size) / 100;
        if (resource == good.Cheap)
            price = price * 3 / 4;
        else if (resource == good.Dear)
            price = price * 4 / 3;
        return Math.Max(0, price);
    }
}
