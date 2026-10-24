using SpaceTrader.Core;

namespace SpaceTrader.Core.Tests;

public class RulesTests
{
    [Fact]
    public void GalaxyHasTheClassicNamesAndAMonarchyAtGalvon()
    {
        Assert.Equal(128, Catalog.SystemNames.Length);
        Assert.Equal("Acamar", Catalog.SystemNames[0]);
        Assert.Equal("Zonama", Catalog.SystemNames[^1]);

        var game = NewGame();
        Assert.Equal(128, game.Systems.Count);
        Assert.Equal(6, game.Wormholes.Distinct().Count());
        var galvon = game.Systems[Catalog.Galvon];
        Assert.Equal(TechLevel.HiTech, galvon.Tech);
        Assert.Equal("Monarchy", galvon.PoliticsInfo.Name);
        Assert.Contains(game.Systems, system => system.X is > 0 and < Catalog.GalaxyWidth);
    }

    [Fact]
    public void PricesFollowTechTheWayThePalmGameDid()
    {
        var rich = Catalog.Politics[1];
        var waterLow = Catalog.StandardPrice(Catalog.Goods[0], 0, TechLevel.Agricultural, Size.Medium, rich, Resource.Nothing);
        var waterHigh = Catalog.StandardPrice(Catalog.Goods[0], 0, TechLevel.HiTech, Size.Medium, rich, Resource.Nothing);
        var robotsLow = Catalog.StandardPrice(Catalog.Goods[9], 9, TechLevel.EarlyIndustrial, Size.Medium, rich, Resource.Nothing);
        var robotsHigh = Catalog.StandardPrice(Catalog.Goods[9], 9, TechLevel.HiTech, Size.Medium, rich, Resource.Nothing);

        Assert.True(waterHigh > waterLow);
        Assert.True(robotsHigh < robotsLow);
        Assert.Equal(0, Catalog.StandardPrice(Catalog.Goods[9], 9, TechLevel.PreAgricultural, Size.Tiny, rich, Resource.Nothing));
    }

    [Fact]
    public void NewCommanderStartsInAGnatWithAPulseLaser()
    {
        var game = NewGame();
        Assert.Equal(1000, game.Cash);
        Assert.Equal(0, game.Debt);
        Assert.Equal("Gnat", game.Spec.Name);
        Assert.Equal(0, game.Weapons[0]);
        Assert.Equal(game.Spec.Fuel, game.Fuel);
        Assert.Equal(game.Spec.Hull, game.Hull);
        Assert.True(game.Here.Visited);
        Assert.Contains(game.Buy, price => price > 0);
    }

    [Fact]
    public void BuyingCostsMoreThanTheLocalSellPrice()
    {
        var game = NewGame();
        for (var i = 0; i < game.Buy.Length; i++)
        {
            if (game.Buy[i] <= 0)
                continue;
            Assert.True(game.Buy[i] > game.Sell[i]);
        }
    }

    [Fact]
    public void WarpBurnsFuelAndALoanChargesTenPercent()
    {
        var game = NewGame();
        game.EncountersEnabled = false;
        var dest = Neighbor(game);
        var distance = game.DistanceTo(dest);
        game.Borrow(1000);
        Assert.Equal(2000, game.Cash);
        Assert.Equal(1000, game.Debt);

        Assert.True(game.BeginWarp(dest));
        Assert.False(game.Traveling);
        Assert.Equal(dest, game.Current);
        Assert.Equal(game.Spec.Fuel - distance, game.Fuel);
        Assert.Equal(1900, game.Cash);
        Assert.Equal(1000, game.Debt);
        Assert.Equal(1, game.Days);
    }

    [Fact]
    public void WormholeTravelChargesTaxInsteadOfFuel()
    {
        var game = NewGame();
        game.EncountersEnabled = false;
        game.TestDock(game.Wormholes[0]);
        var target = game.WormholeTarget(game.Current);
        var fuel = game.Fuel;
        var cash = game.Cash;
        Assert.True(game.BeginWarp(target));
        Assert.Equal(target, game.Current);
        Assert.Equal(fuel, game.Fuel);
        Assert.Equal(cash - game.Spec.FuelCost * Catalog.WormDistance, game.Cash);
    }

    [Fact]
    public void TradeAndSaveRoundTrip()
    {
        var game = NewGame();
        var good = Enumerable.Range(0, 10).First(index => game.Buy[index] > 0 && game.Here.Stock[index] > 0 && game.Cash >= game.Buy[index]);
        var before = game.Cash;
        game.BuyGood(good, 1);
        Assert.Equal(1, game.Cargo[good]);
        Assert.Equal(before - game.Buy[good], game.Cash);

        var loaded = GameState.Load(game.Save());
        Assert.Equal(game.CommanderName, loaded.CommanderName);
        Assert.Equal(game.Current, loaded.Current);
        Assert.Equal(1, loaded.Cargo[good]);
        Assert.Equal(game.Cash, loaded.Cash);
        Assert.Equal(game.Systems[3].X, loaded.Systems[3].X);
    }

    [Fact]
    public void MoonEndsTheGameAndAPodSurvivesDestruction()
    {
        var game = NewGame();
        var moon = game.Systems.First(system => system.Event == PortEvent.Moon);
        game.TestDock(moon.Id);
        game.TestCredit(Catalog.MoonPrice);
        game.PortAction();
        Assert.Equal(GameEnd.Retired, game.End);
        Assert.Equal(0, game.Cash);

        var survivor = NewGame();
        survivor.BuyPod();
        Assert.False(survivor.EscapePod);
        var yard = survivor.Systems.First(system => system.Tech >= TechLevel.Industrial);
        survivor.TestDock(yard.Id);
        survivor.TestCredit(Catalog.PodPrice);
        survivor.BuyPod();
        Assert.True(survivor.EscapePod);
        survivor.TestDestroy();
        Assert.Equal(GameEnd.Playing, survivor.End);
        Assert.Equal("Flea", survivor.Spec.Name);
        Assert.False(survivor.EscapePod);
    }

    [Fact]
    public void SkillPointsMustTotalTwenty()
    {
        Assert.Throws<ArgumentException>(() => GameState.New("Ada", Difficulty.Normal, 1, 1, 1, 1));
    }

    static GameState NewGame() => GameState.New("Jameson", Difficulty.Normal, 5, 5, 5, 5, seed: 42);

    static int Neighbor(GameState game) =>
        Enumerable.Range(0, game.Systems.Count).First(index =>
            index != game.Current && !game.IsWormhole(game.Current, index) && game.DistanceTo(index) > 0 && game.DistanceTo(index) <= game.Fuel);
}
