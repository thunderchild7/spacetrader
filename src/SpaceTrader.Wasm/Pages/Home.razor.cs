using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SpaceTrader.Core;

namespace SpaceTrader.Wasm.Pages;

public partial class Home
{
    [Inject] IJSRuntime Js { get; set; } = default!;

    static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    GameState? game;
    bool ready;
    bool hasSave;
    bool scoreRecorded;
    string? loadError;
    string name = "Jameson";
    Difficulty difficulty = Difficulty.Normal;
    int pilot = 5;
    int fighter = 5;
    int trader = 5;
    int engineer = 5;
    string tab = "chart";
    int selected = -1;
    bool showAll;
    List<ScoreEntry> scores = [];

    int PointsLeft => 20 - (pilot + fighter + trader + engineer);
    int Focused => game is null || selected < 0 || selected >= game.Systems.Count ? game?.Current ?? 0 : selected;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            var saved = await Js.InvokeAsync<string?>("stSave.load");
            hasSave = !string.IsNullOrWhiteSpace(saved);
            var raw = await Js.InvokeAsync<string?>("stSave.scores");
            if (!string.IsNullOrWhiteSpace(raw))
                scores = JsonSerializer.Deserialize<List<ScoreEntry>>(raw, Json) ?? [];
        }
        catch (Exception ex)
        {
            loadError = ex.Message;
        }

        ready = true;
        StateHasChanged();
    }

    void Bump(int slot, int delta)
    {
        var current = Skill(slot);
        var next = Math.Clamp(current + delta, 1, 10);
        var others = pilot + fighter + trader + engineer - current;
        if (others + next > 20)
            next = 20 - others;
        if (next < 1)
            return;
        SetSkill(slot, next);
    }

    void Preset(int p, int f, int t, int e)
    {
        pilot = p;
        fighter = f;
        trader = t;
        engineer = e;
    }

    async Task Start()
    {
        loadError = null;
        try
        {
            game = GameState.New(name, difficulty, pilot, fighter, trader, engineer);
            selected = game.Current;
            tab = "chart";
            scoreRecorded = false;
            await Persist();
        }
        catch (Exception ex)
        {
            loadError = ex.Message;
        }
    }

    async Task Continue()
    {
        try
        {
            var saved = await Js.InvokeAsync<string?>("stSave.load");
            if (string.IsNullOrWhiteSpace(saved))
            {
                hasSave = false;
                return;
            }

            game = GameState.Load(saved);
            selected = game.Current;
            tab = game.Encounter is null ? "chart" : "chart";
            scoreRecorded = false;
            loadError = null;
        }
        catch (Exception ex)
        {
            loadError = "That save could not be read. " + ex.Message;
        }
    }

    async Task Abandon()
    {
        game = null;
        await Js.InvokeVoidAsync("stSave.clear");
        hasSave = false;
    }

    async Task Mutate(Action action)
    {
        if (game is null)
            return;
        action();
        await Persist();
    }

    async Task Persist()
    {
        if (game is null)
            return;
        if (game.End != GameEnd.Playing)
        {
            await RecordScore();
            await Js.InvokeVoidAsync("stSave.clear");
            hasSave = false;
            return;
        }

        await Js.InvokeVoidAsync("stSave.save", game.Save());
        hasSave = true;
    }

    async Task RecordScore()
    {
        if (game is null || scoreRecorded || game.End == GameEnd.Playing)
            return;
        scoreRecorded = true;
        scores.Insert(0, new ScoreEntry(game.CommanderName, game.Days, game.Worth, game.End.ToString(), game.Difficulty.ToString()));
        scores = scores.Take(8).ToList();
        await Js.InvokeVoidAsync("stSave.saveScores", JsonSerializer.Serialize(scores, Json));
    }

    string? Block(int dest)
    {
        if (game is null)
            return "No game";
        if (game.Debt > Catalog.DebtTooLarge)
            return "Grounded by debt";
        var wormhole = game.IsWormhole(game.Current, dest);
        var distance = game.DistanceTo(dest);
        if (!wormhole && game.Fuel < distance)
            return $"{distance} parsecs, tank has {game.Fuel}";
        var cost = game.DepartureCost(dest);
        if (game.Cash < cost)
            return $"Need {Money(cost)} to depart";
        return null;
    }

    IEnumerable<StarSystem> Routes()
    {
        if (game is null)
            yield break;
        var hole = game.WormholeTarget(game.Current);
        foreach (var system in game.Systems.OrderBy(item => game.DistanceTo(item.Id)))
        {
            if (system.Id == game.Current)
                continue;
            var wormhole = system.Id == hole;
            var inRange = game.DistanceTo(system.Id) <= game.Fuel;
            if (showAll || wormhole || inRange)
                yield return system;
        }
    }

    IEnumerable<string> QuestLines()
    {
        if (game is null)
            yield break;
        if (game.JaporiQuest == 1 && game.JaporiDest >= 0)
            yield return $"Deliver the antidote to {game.Systems[game.JaporiDest].Name}.";
        if (game.Artifact && game.ArtifactDest >= 0)
            yield return $"The artifact buyer is at {game.Systems[game.ArtifactDest].Name}.";
        if (game.JarekQuest == 1 && game.JarekDest >= 0)
            yield return $"Jarek wants passage to {game.Systems[game.JarekDest].Name}.";
        if (game.MonsterAlive)
            yield return "A space monster has been sighted at Acamar.";
        if (game.Tribbles > 0)
            yield return $"{game.Tribbles:N0} tribbles are loose in the hold.";
        if (!QuestLinesExist())
            yield return "No open errands. The ports still gossip.";
    }

    bool QuestLinesExist() => game is not null && (game.JaporiQuest == 1 || game.Artifact || game.JarekQuest == 1 || game.MonsterAlive || game.Tribbles > 0);

    static string Money(int value) => value.ToString("N0") + " cr.";

    static string TechColor(TechLevel tech) => tech switch
    {
        TechLevel.PreAgricultural => "#7d8794",
        TechLevel.Agricultural => "#8eae62",
        TechLevel.Medieval => "#d2a15a",
        TechLevel.Renaissance => "#d4844e",
        TechLevel.EarlyIndustrial => "#d46c6c",
        TechLevel.Industrial => "#79b4d8",
        TechLevel.PostIndustrial => "#7ed0c4",
        _ => "#f0d48a"
    };

    int Skill(int slot) => slot switch { 0 => pilot, 1 => fighter, 2 => trader, _ => engineer };

    void SetSkill(int slot, int value)
    {
        switch (slot)
        {
            case 0: pilot = value; break;
            case 1: fighter = value; break;
            case 2: trader = value; break;
            default: engineer = value; break;
        }
    }

    static int Percent(int value, int max) => max <= 0 ? 0 : Math.Clamp(value * 100 / max, 0, 100);

    sealed record ScoreEntry(string Name, int Days, int Worth, string End, string Difficulty);
}
