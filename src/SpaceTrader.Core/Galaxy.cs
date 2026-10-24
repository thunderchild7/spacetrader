namespace SpaceTrader.Core;

public static class Galaxy
{
    public static List<StarSystem> Generate(Random rng, out int[] wormholes)
    {
        var count = Catalog.SystemNames.Length;
        var systems = new StarSystem[count];
        wormholes = new int[6];

        for (var i = 0; i < count; i++)
        {
            var politics = rng.Next(Catalog.Politics.Length);
            var size = (Size)rng.Next((int)Size.Huge + 1);
            var tech = (TechLevel)rng.Next((int)Catalog.Politics[politics].MinTech, (int)Catalog.Politics[politics].MaxTech + 1);
            if (i == Catalog.Galvon)
            {
                size = Size.Large;
                politics = 11;
                tech = TechLevel.HiTech;
            }

            var pressure = Pressure.None;
            if (rng.Next(100) < 15)
                pressure = (Pressure)rng.Next((int)Pressure.War, (int)Pressure.Employment + 1);

            var resource = Resource.Nothing;
            if (rng.Next(5) >= 3)
                resource = (Resource)rng.Next((int)Resource.MineralRich, (int)Resource.Warlike + 1);

            int x;
            int y;
            if (i < wormholes.Length)
            {
                x = Catalog.GalaxyWidth * (1 + 2 * (i % 3)) / 6 - rng.Next(-Catalog.CloseDistance + 1, Catalog.CloseDistance);
                y = Catalog.GalaxyHeight * (i < 3 ? 1 : 3) / 4 - rng.Next(-Catalog.CloseDistance + 1, Catalog.CloseDistance);
                wormholes[i] = i;
            }
            else
            {
                x = 1;
                y = 1;
                var placed = false;
                for (var attempt = 0; attempt < 200_000 && !placed; attempt++)
                {
                    x = rng.Next(1, Catalog.GalaxyWidth);
                    y = rng.Next(1, Catalog.GalaxyHeight);
                    var close = false;
                    var tooClose = false;
                    for (var j = 0; j < i && !tooClose; j++)
                    {
                        var distance = Catalog.Distance(systems[j].X, systems[j].Y, x, y);
                        if (distance < Catalog.MinDistance)
                            tooClose = true;
                        if (distance < Catalog.CloseDistance)
                            close = true;
                    }

                    placed = close && !tooClose;
                }

                if (!placed)
                    throw new InvalidOperationException($"Could not place {Catalog.SystemNames[i]}.");
            }

            systems[i] = new StarSystem
            {
                Id = i,
                Name = Catalog.SystemNames[i],
                X = x,
                Y = y,
                Size = size,
                Tech = tech,
                Politics = politics,
                Pressure = pressure,
                Resource = resource
            };
        }

        for (var i = 0; i < systems.Length; i++)
        {
            var j = rng.Next(systems.Length);
            if (Array.IndexOf(wormholes, j) >= 0)
                continue;

            (systems[i].X, systems[j].X) = (systems[j].X, systems[i].X);
            (systems[i].Y, systems[j].Y) = (systems[j].Y, systems[i].Y);
            var wormhole = Array.IndexOf(wormholes, i);
            if (wormhole >= 0)
                wormholes[wormhole] = j;
        }

        for (var i = 0; i < wormholes.Length; i++)
        {
            var j = rng.Next(wormholes.Length);
            (wormholes[i], wormholes[j]) = (wormholes[j], wormholes[i]);
        }

        return systems.ToList();
    }
}

public sealed class StarSystem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public Size Size { get; set; }
    public TechLevel Tech { get; set; }
    public int Politics { get; set; }
    public Pressure Pressure { get; set; }
    public Resource Resource { get; set; }
    public PortEvent Event { get; set; }
    public int[] Stock { get; set; } = new int[Catalog.Goods.Length];
    public int Countdown { get; set; }
    public bool Visited { get; set; }

    public Politics PoliticsInfo => Catalog.Politics[Politics];
}
