namespace SpaceTrader.Core;

public sealed partial class GameState
{
    public bool BeginWarp(int dest)
    {
        if (End != GameEnd.Playing || Encounter is not null || Traveling)
            return false;
        if (dest < 0 || dest >= Systems.Count || dest == Current)
            return false;
        if (Debt > Catalog.DebtTooLarge)
        {
            Say("Your debt is so large that the port authority grounds you.");
            return false;
        }

        var wormhole = IsWormhole(Current, dest);
        var distance = DistanceTo(dest);
        if (!wormhole && Fuel < distance)
        {
            Say($"Not enough fuel. {Systems[dest].Name} is {distance} parsecs away, and the tank holds {Fuel}.");
            return false;
        }

        var cost = DepartureCost(dest);
        if (Cash < cost)
        {
            Say($"You need {cost:N0} cr. on hand for crew, insurance, and wormhole tax.");
            return false;
        }

        Cash -= cost;
        if (!wormhole)
            Fuel -= distance;
        if (Debt > 0)
        {
            var interest = InterestDue;
            if (Cash > interest)
                Cash -= interest;
            else
            {
                Debt += interest - Cash;
                Cash = 0;
            }
        }

        Days++;
        Here.Countdown = (int)Difficulty + 3;
        Destination = dest;
        Traveling = true;
        Clicks = Catalog.TravelClicks;
        Raided = false;
        Inspected = false;
        Say(wormhole
            ? $"The wormhole opens toward {Systems[dest].Name}. Tax paid."
            : $"Warp set for {Systems[dest].Name}, {distance} parsecs out.");
        Step();
        return true;
    }

    public void Act(string action)
    {
        if (Encounter is null || End != GameEnd.Playing)
            return;

        switch (action)
        {
            case "attack":
                PlayerAttack();
                break;
            case "flee":
                TryFlee();
                break;
            case "ignore":
                if (Encounter.Hostile)
                {
                    Note("You hesitate, and they open fire.");
                    EnemyTurn();
                }
                else
                {
                    Note($"The {Encounter.Title} passes on.");
                    CloseEncounter();
                }
                break;
            case "surrender":
                Surrender();
                break;
            case "bribe":
                Bribe();
                break;
            case "submit":
                Submit();
                break;
            case "plunder":
                Plunder();
                break;
            case "leave":
                CloseEncounter();
                break;
            case "buy":
                TradeWithShip(buying: true);
                break;
            case "sell":
                TradeWithShip(buying: false);
                break;
        }
    }

    public void BuyGood(int good, int qty)
    {
        if (End != GameEnd.Playing || good < 0 || good >= Cargo.Length)
            return;
        if (Debt > Catalog.DebtTooLarge)
        {
            Say("Your debt is too large for the merchants to deal with you.");
            return;
        }
        if (qty <= 0 || Buy[good] <= 0)
        {
            Say($"{Catalog.Goods[good].Name} is not for sale here.");
            return;
        }

        qty = Math.Min(qty, Here.Stock[good]);
        qty = Math.Min(qty, FreeBays);
        qty = Math.Min(qty, Buy[good] <= 0 ? 0 : Cash / Buy[good]);
        if (qty <= 0)
        {
            Say("You cannot take any more of that aboard.");
            return;
        }

        var cost = qty * Buy[good];
        Cash -= cost;
        Cargo[good] += qty;
        Paid[good] += cost;
        Here.Stock[good] -= qty;
        Say($"Bought {qty} {Catalog.Goods[good].Name} for {cost:N0} cr.");
    }

    public void SellGood(int good, int qty)
    {
        if (good < 0 || good >= Cargo.Length || qty <= 0)
            return;
        if (Sell[good] <= 0)
        {
            Say($"No one here is buying {Catalog.Goods[good].Name}.");
            return;
        }

        qty = Math.Min(qty, Cargo[good]);
        if (qty <= 0)
            return;
        var revenue = qty * Sell[good];
        var left = Cargo[good] - qty;
        Paid[good] = left <= 0 ? 0 : Paid[good] * left / Cargo[good];
        Cargo[good] = left;
        Cash += revenue;
        Here.Stock[good] += qty;
        Say($"Sold {qty} {Catalog.Goods[good].Name} for {revenue:N0} cr.");
    }

    public void Jettison(int good)
    {
        if (good < 0 || Cargo[good] <= 0)
            return;
        var before = Cargo[good];
        Cargo[good]--;
        Paid[good] = Cargo[good] == 0 ? 0 : Paid[good] * Cargo[good] / before;
        if (Rng.Next(10) < (int)Difficulty + 1)
        {
            if (PoliceScore > -5)
                PoliceScore = -5;
            else
                PoliceScore--;
            Say($"Dumping {Catalog.Goods[good].Name} was witnessed. Your record slips.");
        }
        else
        {
            Say($"Dumped one unit of {Catalog.Goods[good].Name}.");
        }
    }

    public void FillFuel()
    {
        var missing = Spec.Fuel - Fuel;
        if (missing <= 0)
        {
            Say("The tank is already full.");
            return;
        }

        var unit = Math.Max(1, Spec.FuelCost);
        var affordable = Math.Min(missing, Cash / unit);
        if (affordable <= 0)
        {
            Say("You cannot afford fuel.");
            return;
        }

        Cash -= affordable * unit;
        Fuel += affordable;
        Say($"Fuel tank now holds {Fuel}/{Spec.Fuel} parsecs.");
    }

    public void RepairHull()
    {
        var missing = Spec.Hull - Hull;
        if (missing <= 0)
        {
            Say("The hull does not need repairs.");
            return;
        }

        var unit = Math.Max(1, Spec.Repair);
        var affordable = Math.Min(missing, Cash / unit);
        if (affordable <= 0)
        {
            Say("You cannot afford repairs.");
            return;
        }

        Cash -= affordable * unit;
        Hull += affordable;
        Say($"Hull strength is {Hull}/{Spec.Hull}.");
    }

    public void BuyShip(int type)
    {
        if (type < 0 || type >= 10 || type == Ship)
        {
            Say(type == Ship ? "You already fly that model." : "That ship is not for sale.");
            return;
        }

        var spec = Catalog.Ships[type];
        if (spec.MinTech > Here.Tech)
        {
            Say($"{spec.Name} is not built at this tech level.");
            return;
        }
        if (spec.Crew < CommanderQuarters)
        {
            Say("Dismiss crew until they fit the new quarters.");
            return;
        }

        var due = spec.Price - ShipWorth + (EscapePod ? Catalog.PodTransfer : 0);
        if (due > 0 && Debt > 0)
        {
            Say("The yard will not finance a ship while you are in debt.");
            return;
        }
        if (due > Cash)
        {
            Say($"A {spec.Name} costs {due:N0} cr. after trade-in.");
            return;
        }

        var keepPod = EscapePod;
        Cash -= due;
        Ship = type;
        Hull = spec.Hull;
        Fuel = spec.Fuel;
        Weapons = [-1, -1, -1];
        Shields = [-1, -1, -1];
        Gadgets = [-1, -1, -1];
        Cargo = new int[Catalog.Goods.Length];
        Paid = new int[Catalog.Goods.Length];
        Tribbles = 0;
        ShieldCharge = 0;
        EscapePod = keepPod;
        if (!keepPod)
        {
            Insurance = false;
            NoClaim = 0;
        }

        Say(due >= 0
            ? $"The yard hands over a {spec.Name}. You paid {due:N0} cr. after trade-in."
            : $"The yard hands over a {spec.Name} and pays you {-due:N0} cr.");
    }

    public void BuyPod()
    {
        if (EscapePod)
        {
            Say("An escape pod is already fitted.");
            return;
        }
        if (Here.Tech < TechLevel.Industrial)
        {
            Say("No escape pods are sold at this tech level.");
            return;
        }
        if (Debt > 0)
        {
            Say("Clear your debt before buying a pod.");
            return;
        }
        if (Cash < Catalog.PodPrice)
        {
            Say("An escape pod costs 2,000 cr.");
            return;
        }

        Cash -= Catalog.PodPrice;
        EscapePod = true;
        Say("Escape pod fitted. Insurance will now pay out if the hull fails.");
    }

    public void BuyWeapon(int id) => BuyInto(Weapons, Spec.Weapons, id, Catalog.Weapons[id].Name, Catalog.Weapons[id].Price, Catalog.Weapons[id].Tech, slot => Weapons[slot] = id);

    public void BuyShield(int id)
    {
        BuyInto(Shields, Spec.Shields, id, Catalog.Shields[id].Name, Catalog.Shields[id].Price, Catalog.Shields[id].Tech, slot =>
        {
            Shields[slot] = id;
            ShieldCharge = ShieldMax();
        });
    }

    public void BuyGadget(int id) => BuyInto(Gadgets, Spec.Gadgets, id, Catalog.Gadgets[id].Name, Catalog.Gadgets[id].Price, Catalog.Gadgets[id].Tech, slot => Gadgets[slot] = id);

    public void SellWeapon(int slot) => SellFrom(Weapons, Spec.Weapons, slot, slotIndex => Catalog.Weapons[slotIndex].Name, slotIndex => Catalog.Weapons[slotIndex].Price);

    public void SellShield(int slot)
    {
        SellFrom(Shields, Spec.Shields, slot, slotIndex => Catalog.Shields[slotIndex].Name, slotIndex => Catalog.Shields[slotIndex].Price);
        ShieldCharge = Math.Min(ShieldCharge, ShieldMax());
    }

    public void SellGadget(int slot) => SellFrom(Gadgets, Spec.Gadgets, slot, slotIndex => Catalog.Gadgets[slotIndex].Name, slotIndex => Catalog.Gadgets[slotIndex].Price);

    public void Hire(int index)
    {
        if (index < 0 || index >= Crew.Count)
            return;
        var member = Crew[index];
        if (member.Role == CrewRole.Jarek || member.Hired || member.System != Current)
        {
            Say($"{member.Name} is not taking a posting here.");
            return;
        }
        if (member.Role == CrewRole.Zeethibal && Reputation < 300)
        {
            Say("Zeethibal will not fly with a commander of your reputation.");
            return;
        }
        if (CommanderQuarters >= Spec.Crew)
        {
            Say("There are no free crew quarters.");
            return;
        }

        member.Hired = true;
        Say($"{member.Name} joins the ship. Daily wage {member.Wage:N0} cr.");
    }

    public void Dismiss(int index)
    {
        if (index < 0 || index >= Crew.Count || !Crew[index].Hired)
            return;
        if (Crew[index].Role == CrewRole.Jarek)
        {
            Say("Jarek is a passenger. Drop him at the system he named.");
            return;
        }

        Crew[index].Hired = false;
        Crew[index].System = Current;
        Say($"{Crew[index].Name} returns to the station.");
    }

    public void Borrow(int amount)
    {
        var room = MaxLoan - Debt;
        if (room <= 0 || amount <= 0)
        {
            Say("The bank will not extend another credit.");
            return;
        }

        amount = Math.Min(amount, room);
        Debt += amount;
        Cash += amount;
        Say($"Borrowed {amount:N0} cr. Interest is 10% each day you warp.");
    }

    public void Repay(int amount)
    {
        amount = Math.Min(Math.Max(0, amount), Math.Min(Cash, Debt));
        if (amount <= 0)
        {
            Say("Nothing was repaid.");
            return;
        }

        Cash -= amount;
        Debt -= amount;
        Say($"Repaid {amount:N0} cr. Debt is now {Debt:N0}.");
    }

    public void ToggleInsurance()
    {
        if (Insurance)
        {
            Insurance = false;
            Say("Insurance cancelled.");
            return;
        }
        if (!EscapePod)
        {
            Say("The bank will not insure a ship with no escape pod.");
            return;
        }

        Insurance = true;
        Say("The ship is insured. Premiums are due each time you warp.");
    }

    public void PortAction(int choice = 0)
    {
        if (End != GameEnd.Playing)
            return;
        switch (Here.Event)
        {
            case PortEvent.Lottery:
                Cash += 1_000;
                Here.Event = PortEvent.None;
                Say("The lottery pays 1,000 cr. The clerk looks surprised.");
                break;
            case PortEvent.Moon:
                if (Cash < Catalog.MoonPrice)
                {
                    Say("A retirement moon costs 500,000 cr.");
                    return;
                }
                Cash -= Catalog.MoonPrice;
                Here.Event = PortEvent.None;
                End = GameEnd.Retired;
                Say($"You buy a moon above {Here.Name} and retire from the spaceways.");
                break;
            case PortEvent.EraseRecord:
                if (Cash < 5_000)
                {
                    Say("Cleaning a police record costs 5,000 cr.");
                    return;
                }
                Cash -= 5_000;
                PoliceScore = 0;
                Here.Event = PortEvent.None;
                RecalculatePrices();
                Say("Your police record is wiped clean.");
                break;
            case PortEvent.TrainSkill:
                if (choice is < 0 or > 3)
                    return;
                if (Cash < 3_000)
                {
                    Say("The tutor wants 3,000 cr.");
                    return;
                }
                if (SkillBase(choice) >= Catalog.MaxSkill)
                {
                    Say("That skill is already mastered.");
                    return;
                }
                Cash -= 3_000;
                AddSkill(choice, 1);
                Here.Event = PortEvent.None;
                Say("The lesson sticks. One skill point gained.");
                break;
            case PortEvent.Tribble:
                if (Cash < 1_000)
                {
                    Say("The tribble costs 1,000 cr.");
                    return;
                }
                Cash -= 1_000;
                Tribbles = Math.Max(1, Tribbles);
                Here.Event = PortEvent.None;
                Say("You buy one small, purring tribble.");
                break;
            case PortEvent.TribbleBuyer:
                if (Tribbles <= 0)
                {
                    Say("You have no tribbles to sell.");
                    return;
                }
                var pay = Math.Min(Tribbles, 5_000) / 2;
                Cash += pay;
                Say($"A collector takes {Tribbles:N0} tribbles for {pay:N0} cr.");
                Tribbles = 0;
                Here.Event = PortEvent.None;
                break;
            case PortEvent.Japori:
                if (JaporiDest < 0)
                    return;
                if (FreeBays < 10)
                {
                    Say("The antidote needs 10 empty cargo bays.");
                    return;
                }
                Cargo[(int)Good.Medicine] += 10;
                JaporiQuest = 1;
                Here.Event = PortEvent.None;
                Say($"Antidote loaded. Deliver it to {Systems[JaporiDest].Name}.");
                break;
            case PortEvent.JaporiDelivery:
                if (JaporiQuest != 1)
                {
                    Say("You are not carrying the Japori antidote.");
                    return;
                }
                if (Cargo[(int)Good.Medicine] < 10)
                {
                    Say("You need 10 units of medicine to finish the delivery.");
                    return;
                }
                RemoveCargo((int)Good.Medicine, 10);
                AddSkill(Rng.Next(4), 1);
                AddSkill(Rng.Next(4), 1);
                JaporiQuest = 2;
                Here.Event = PortEvent.None;
                Say("The plague breaks. Two of your skills improve.");
                break;
            case PortEvent.Artifact:
                if (ArtifactDest < 0)
                    return;
                Artifact = true;
                ArtifactQuest = 1;
                Here.Event = PortEvent.None;
                Say($"The alien artifact is aboard. A buyer waits at {Systems[ArtifactDest].Name}.");
                break;
            case PortEvent.ArtifactDelivery:
                if (!Artifact)
                {
                    Say("You are not carrying the artifact.");
                    return;
                }
                Artifact = false;
                ArtifactQuest = 2;
                Cash += 20_000;
                Here.Event = PortEvent.None;
                Say("The museum pays 20,000 cr. for the artifact.");
                break;
            case PortEvent.Jarek:
                if (JarekDest < 0)
                    return;
                if (CommanderQuarters >= Spec.Crew)
                {
                    Say("Jarek needs a free crew quarter.");
                    return;
                }
                var jarek = Crew.FirstOrDefault(member => member.Role == CrewRole.Jarek);
                if (jarek is null)
                    return;
                jarek.Hired = true;
                JarekQuest = 1;
                Here.Event = PortEvent.None;
                Say($"Ambassador Jarek comes aboard. Take him to {Systems[JarekDest].Name}.");
                break;
            case PortEvent.JarekDrop:
                if (JarekQuest != 1)
                {
                    Say("Jarek is not aboard.");
                    return;
                }
                var passenger = Crew.First(member => member.Role == CrewRole.Jarek);
                passenger.Hired = false;
                passenger.System = Current;
                Cash += 5_000;
                AddSkill(2, 1);
                JarekQuest = 2;
                Here.Event = PortEvent.None;
                Say("Jarek disembarks and leaves 5,000 cr. Your trading improves.");
                break;
            case PortEvent.SealedCanisters:
                if (Cash < 1_000 || FreeBays < 3)
                {
                    Say("The canisters cost 1,000 cr. and three empty bays.");
                    return;
                }
                var good = Rng.Next(Catalog.Goods.Length);
                Cash -= 1_000;
                Cargo[good] += 3;
                Paid[good] += 1_000;
                Here.Event = PortEvent.None;
                Say($"The canisters are labeled {Catalog.Goods[good].Name}. You stow three.");
                break;
            case PortEvent.Monster:
                HuntMonster();
                break;
        }
    }

    public void HuntMonster()
    {
        if (!MonsterAlive || Here.Id != Catalog.Acamar || Encounter is not null || End != GameEnd.Playing)
        {
            Say("There is no monster to hunt here.");
            return;
        }

        Encounter = MakeMonster();
        Say("You clear the port and go looking for the space monster.");
    }

    void Step()
    {
        while (Clicks > 0 && Encounter is null && End == GameEnd.Playing && !PodEscape)
        {
            if (EncountersEnabled && RollEncounter())
                return;
            Clicks--;
        }

        if (Encounter is null && End == GameEnd.Playing && Traveling)
            Arrive();
    }

    void Arrive()
    {
        var dest = Destination;
        Traveling = false;
        Clicks = 0;
        for (var i = 0; i < Systems.Count; i++)
        {
            if (i == dest || Systems[i].Countdown <= 0)
                continue;
            Systems[i].Countdown--;
            if (Systems[i].Countdown == 0)
                Restock(Systems[i]);
        }

        Current = dest;
        Destination = -1;
        Here.Visited = true;
        if (Insurance)
            NoClaim = Math.Min(Catalog.MaxNoClaim, NoClaim + 1);
        ShieldCharge = ShieldMax();
        if (Has(GadgetKind.AutoRepair) && Hull < Spec.Hull)
            Hull = Math.Min(Spec.Hull, Hull + Math.Max(1, Skill(3) / 2));
        GrowTribbles();
        RecalculatePrices();
        Say($"Docked at {Here.Name}. {Describe(Here)}");
        if (Debt >= Catalog.DebtWarning)
            Say($"The bank reminds you of a {Debt:N0} cr. debt.");
    }

    void GrowTribbles()
    {
        if (Tribbles <= 0)
            return;
        var food = (int)Good.Food;
        if (Cargo[food] > 0)
        {
            RemoveCargo(food, 1);
            Tribbles = Math.Min(100_000, Tribbles * 2);
            Say("The tribbles eat a unit of food and double.");
        }
        else
        {
            Tribbles /= 2;
            Say(Tribbles == 0 ? "The tribbles starve out." : "Hungry tribbles die off.");
        }
    }

    bool RollEncounter()
    {
        var dest = Systems[Destination];
        var politics = dest.PoliticsInfo;
        if (Has(GadgetKind.Cloaking) && Rng.Next(100) < 35 + Skill(3) * 3)
            return false;
        if (MonsterAlive && dest.Id == Catalog.Acamar && Rng.Next(12) == 0)
        {
            Encounter = MakeMonster();
            return true;
        }

        var roll = Rng.Next(Math.Max(8, 44 - 2 * (int)Difficulty));
        if (Ship == 0)
            roll *= 2;
        var rank = Catalog.IndexOf(PoliceScore, Catalog.RecordScores);
        var policeMod = Math.Max(1, 3 - Math.Min(rank, 5));
        var pirates = (int)politics.Pirates;
        var police = (int)politics.Police * policeMod;
        var traders = (int)politics.Traders;
        if (!Raided && roll < pirates)
            return StartOpponent(EncounterKind.Pirate, dest);
        if (roll < pirates + police)
            return StartPolice(dest);
        if (roll < pirates + police + traders)
            return StartTrader(dest);
        if (Artifact && Rng.Next(20) <= 3)
            return StartOpponent(EncounterKind.Pirate, dest);
        return false;
    }

    bool StartPolice(StarSystem dest)
    {
        if (PoliceScore < -5)
        {
            Encounter = MakeShip(EncounterKind.Police, dest, hostile: true);
            Encounter.Detail = "Police demand that you heave to.";
            return true;
        }

        if (!Inspected && (PoliceScore < 5 && Rng.Next(100) < 30 || Rng.Next(40) == 0))
        {
            Inspected = true;
            if (!HasContraband(dest.PoliticsInfo))
            {
                Say("Police inspect the hold, find nothing illegal, and send you on.");
                return false;
            }

            Encounter = MakeShip(EncounterKind.Police, dest, hostile: false);
            Encounter.Fine = ContrabandValue(dest);
            Encounter.Detail = "Police find contraband. Submit, fight, or try a bribe.";
            return true;
        }

        return false;
    }

    bool StartTrader(StarSystem dest)
    {
        Encounter = MakeShip(EncounterKind.Trader, dest, hostile: false);
        if (Rng.Next(100) < 25)
        {
            Encounter.Fleeing = true;
            Encounter.Detail = "A trader spots you and starts to run.";
            return true;
        }

        var good = Rng.Next(Catalog.Goods.Length);
        Encounter.TradeGood = good;
        Encounter.TradeQty = 1 + Rng.Next(5);
        var basis = Sell[good] > 0 ? Sell[good] : Math.Max(1, Catalog.Goods[good].PriceLow);
        Encounter.TradePrice = Math.Max(1, basis);
        Encounter.Cargo[good] = Encounter.TradeQty;
        Encounter.Detail = $"A trader hails you, offering {Catalog.Goods[good].Name} at {Encounter.TradePrice:N0} cr.";
        return true;
    }

    bool StartOpponent(EncounterKind kind, StarSystem dest)
    {
        Encounter = MakeShip(kind, dest, hostile: true);
        if (kind == EncounterKind.Pirate && Reputation >= 300 && Rng.Next(100) < 40)
        {
            Encounter.Hostile = false;
            Encounter.Fleeing = true;
            Encounter.Detail = "The pirate ship thinks better of it and turns to run.";
        }
        else if (kind == EncounterKind.Pirate)
        {
            Encounter.Detail = "Pirates demand your cargo.";
        }

        return true;
    }

    Encounter MakeShip(EncounterKind kind, StarSystem dest, bool hostile)
    {
        var politics = dest.PoliticsInfo;
        var ship = PickOpponent(kind, politics);
        var spec = Catalog.Ships[ship];
        var shield = 0;
        if (spec.Shields > 0 && Difficulty >= Difficulty.Normal)
            shield = Catalog.Shields[0].Power * Math.Min(spec.Shields, Difficulty >= Difficulty.Hard ? 2 : 1);
        var power = 0;
        var slots = Math.Max(1, spec.Weapons);
        for (var i = 0; i < spec.Weapons; i++)
            power += Difficulty >= Difficulty.Hard && i == 0 ? Catalog.Weapons[1].Power : Catalog.Weapons[0].Power;
        if (spec.Weapons == 0)
            power = 0;

        var skillBump = (int)Difficulty - 2;
        var encounter = new Encounter
        {
            Kind = kind,
            Ship = ship,
            Title = $"{kind.ToString().ToLowerInvariant()} {spec.Name}",
            Hull = spec.Hull,
            MaxHull = spec.Hull,
            Shield = shield,
            MaxShield = shield,
            Power = power,
            Pilot = Math.Clamp(RandomSkill() + skillBump, 1, 13),
            Fighter = Math.Clamp(RandomSkill() + skillBump, 1, 13),
            Hostile = hostile,
            Bribe = politics.Bribe <= 0 ? 0 : Math.Max(100, (8 - politics.Bribe) * 150)
        };
        if (spec.Bays > 0 && kind != EncounterKind.Police)
        {
            var good = Rng.Next(Catalog.Goods.Length);
            encounter.Cargo[good] = 1 + Rng.Next(Math.Max(1, spec.Bays / 4));
        }

        _ = slots;
        return encounter;
    }

    Encounter MakeMonster()
    {
        var spec = Catalog.Ships[10];
        var bump = (int)Difficulty;
        return new Encounter
        {
            Kind = EncounterKind.Monster,
            Ship = 10,
            Title = "space monster",
            Detail = "The space monster turns toward your ship.",
            Hull = spec.Hull,
            MaxHull = spec.Hull,
            Power = Catalog.Weapons[2].Power * spec.Weapons,
            Pilot = 8 + bump,
            Fighter = 8 + bump,
            Hostile = true
        };
    }

    int PickOpponent(EncounterKind kind, Politics politics)
    {
        Activity Role(ShipSpec spec) => kind switch
        {
            EncounterKind.Police => spec.Police,
            EncounterKind.Trader => spec.Traders,
            _ => spec.Pirates
        };
        var activity = kind switch
        {
            EncounterKind.Police => politics.Police,
            EncounterKind.Trader => politics.Traders,
            _ => politics.Pirates
        };

        var candidates = new List<int>();
        for (var i = 0; i < 10; i++)
        {
            var spec = Catalog.Ships[i];
            var shipActivity = Role(spec);
            if (shipActivity == Activity.NA)
                continue;
            if ((int)activity + (int)Difficulty >= (int)shipActivity)
                candidates.Add(i);
        }

        if (candidates.Count == 0)
            return 1;
        var total = candidates.Sum(index => Math.Max(1, Catalog.Ships[index].Occurrence));
        var roll = Rng.Next(total);
        foreach (var index in candidates)
        {
            roll -= Math.Max(1, Catalog.Ships[index].Occurrence);
            if (roll < 0)
                return index;
        }

        return candidates[^1];
    }

    void PlayerAttack()
    {
        var encounter = Encounter!;
        if (encounter.Destroyed || encounter.Disabled)
            return;
        if (WeaponPower() <= 0)
        {
            Note("You have nothing to fire.");
            if (encounter.Hostile)
                EnemyTurn();
            return;
        }

        var hit = Rng.Next(100) < Math.Clamp(45 + (Skill(1) - encounter.Pilot) * 4, 8, 92);
        if (!hit)
        {
            Note($"Your shot misses the {encounter.Title}.");
        }
        else
        {
            var damage = WeaponPower();
            var absorbed = Math.Min(encounter.Shield, damage);
            encounter.Shield -= absorbed;
            encounter.Hull -= damage - absorbed;
            Note(damage - absorbed <= 0
                ? $"The {encounter.Title}'s shields hold."
                : $"You hit the {encounter.Title}.");
            if (encounter.Hull <= 0)
            {
                if (HasDisabler() && encounter.Kind != EncounterKind.Monster)
                {
                    encounter.Hull = 0;
                    encounter.Disabled = true;
                    encounter.Hostile = false;
                    Note($"The {encounter.Title} is disabled.");
                    ScoreOpponent(destroyed: false);
                }
                else
                {
                    encounter.Hull = 0;
                    encounter.Destroyed = true;
                    encounter.Hostile = false;
                    Note($"The {encounter.Title} breaks apart.");
                    ScoreOpponent(destroyed: true);
                    if (encounter.Cargo.All(amount => amount <= 0))
                        CloseEncounter();
                }
                return;
            }
        }

        if (encounter.Hostile)
            EnemyTurn();
    }

    void EnemyTurn()
    {
        var encounter = Encounter;
        if (encounter is null || !encounter.Hostile || End != GameEnd.Playing)
            return;
        if (encounter.Fleeing && Rng.Next(100) < Math.Clamp(30 + (encounter.Pilot - Skill(0)) * 5, 5, 90))
        {
            Note($"The {encounter.Title} escapes.");
            CloseEncounter();
            return;
        }

        if (encounter.Power <= 0)
        {
            Note($"The {encounter.Title} cannot hurt you.");
            return;
        }

        var hit = Rng.Next(100) < Math.Clamp(40 + (encounter.Fighter - Skill(0)) * 4, 8, 92);
        if (!hit)
        {
            Note($"The {encounter.Title} misses.");
            return;
        }

        var damage = encounter.Power;
        var absorbed = Math.Min(ShieldCharge, damage);
        ShieldCharge -= absorbed;
        Hull -= damage - absorbed;
        Note(damage - absorbed <= 0 ? "Your shields hold." : $"The {encounter.Title} hits the hull.");
        if (Hull <= 0)
            ResolveShipLoss($"The {encounter.Title} finishes the ship.");
    }

    void TryFlee()
    {
        var encounter = Encounter!;
        var chance = Math.Clamp(30 + (Skill(0) - encounter.Pilot) * 5, 5, 95);
        if (Rng.Next(100) < chance)
        {
            if (encounter.Kind == EncounterKind.Police && encounter.Hostile)
                PoliceScore -= 2;
            Note("You get away.");
            CloseEncounter();
            return;
        }

        Note("You fail to break away.");
        if (encounter.Hostile)
            EnemyTurn();
    }

    void Surrender()
    {
        var encounter = Encounter!;
        if (encounter.Kind == EncounterKind.Monster)
        {
            Note("The monster does not accept surrender.");
            EnemyTurn();
            return;
        }
        if (encounter.Kind == EncounterKind.Police)
        {
            Submit();
            return;
        }
        if (encounter.Kind == EncounterKind.Trader)
        {
            PoliceScore -= 2;
        }

        var taken = Math.Min(Cash, Math.Max(0, Cash / 10));
        Cash -= taken;
        StealCargo();
        Raided = true;
        PoliceScore -= 1;
        Note("They take a cut of your cash and cargo, then break off.");
        CloseEncounter();
    }

    void Bribe()
    {
        var encounter = Encounter!;
        if (encounter.Bribe <= 0)
        {
            Note("They will not be bribed.");
            return;
        }
        if (Cash < encounter.Bribe)
        {
            Note($"The bribe is {encounter.Bribe:N0} cr.");
            return;
        }

        Cash -= encounter.Bribe;
        Note("Credits change hands. They look away.");
        CloseEncounter();
    }

    void Submit()
    {
        var encounter = Encounter!;
        var fine = Math.Max(encounter.Fine, encounter.Kind == EncounterKind.Police ? 200 : 0);
        if (Cash >= fine)
            Cash -= fine;
        else
        {
            Debt += fine - Cash;
            Cash = 0;
        }

        DumpIllegal();
        PoliceScore -= 1;
        Note($"You submit. The fine is {fine:N0} cr.");
        CloseEncounter();
    }

    void Plunder()
    {
        var encounter = Encounter!;
        var taken = 0;
        for (var good = 0; good < encounter.Cargo.Length && FreeBays > 0; good++)
        {
            var move = Math.Min(FreeBays, encounter.Cargo[good]);
            if (move <= 0)
                continue;
            Cargo[good] += move;
            encounter.Cargo[good] -= move;
            taken += move;
        }

        Note(taken == 0 ? "There is nothing left to take." : $"You take {taken} units of cargo.");
        if (encounter.Kind == EncounterKind.Trader)
            PoliceScore -= 2;
        CloseEncounter();
    }

    void TradeWithShip(bool buying)
    {
        var encounter = Encounter!;
        var good = encounter.TradeGood;
        if (good < 0 || encounter.TradeQty <= 0)
        {
            Note("They have nothing to deal.");
            return;
        }

        if (buying)
        {
            if (FreeBays <= 0 || Cash < encounter.TradePrice)
            {
                Note("You cannot take the goods.");
                return;
            }
            var qty = Math.Min(encounter.TradeQty, Math.Min(FreeBays, Cash / encounter.TradePrice));
            Cash -= qty * encounter.TradePrice;
            Cargo[good] += qty;
            Paid[good] += qty * encounter.TradePrice;
            encounter.TradeQty -= qty;
            Note($"You buy {qty} {Catalog.Goods[good].Name}.");
        }
        else
        {
            if (Cargo[good] <= 0)
            {
                Note($"You have no {Catalog.Goods[good].Name}.");
                return;
            }
            var qty = Math.Min(Cargo[good], encounter.TradeQty);
            RemoveCargo(good, qty);
            Cash += qty * encounter.TradePrice;
            encounter.TradeQty -= qty;
            Note($"You sell {qty} {Catalog.Goods[good].Name}.");
        }

        if (encounter.TradeQty <= 0)
            CloseEncounter();
    }

    void ScoreOpponent(bool destroyed)
    {
        var encounter = Encounter!;
        if (encounter.Scored)
            return;
        encounter.Scored = true;
        Reputation += encounter.Ship / 2 + 1;
        switch (encounter.Kind)
        {
            case EncounterKind.Pirate:
                PoliceScore += destroyed ? 1 : -1;
                if (destroyed && PoliceScore >= -5)
                {
                    var bounty = Math.Max(100, Catalog.Ships[encounter.Ship].Price / 20);
                    Cash += bounty;
                    Note($"A pirate bounty of {bounty:N0} cr. is paid.");
                }
                break;
            case EncounterKind.Police:
                PoliceScore += destroyed ? -6 : -3;
                break;
            case EncounterKind.Trader:
                PoliceScore += destroyed ? -4 : -2;
                break;
            case EncounterKind.Monster when destroyed:
                Cash += 15_000;
                MonsterAlive = false;
                Reputation += 10;
                if (Systems[Catalog.Acamar].Event == PortEvent.Monster)
                    Systems[Catalog.Acamar].Event = PortEvent.None;
                Note("The authorities pay 15,000 cr. for the monster.");
                break;
        }
    }

    void CloseEncounter()
    {
        Encounter = null;
        if (End != GameEnd.Playing)
        {
            Traveling = false;
            return;
        }
        if (PodEscape)
        {
            PodEscape = false;
            if (Traveling)
                Arrive();
            return;
        }
        if (!Traveling)
            return;
        Clicks--;
        Step();
    }

    void ResolveShipLoss(string why)
    {
        Encounter = null;
        if (EscapePod)
        {
            if (Insurance)
            {
                Cash += Math.Max(0, BaseWorth(true));
                NoClaim = 0;
            }

            var lostJarek = Crew.Any(member => member.Hired && member.Role == CrewRole.Jarek);
            foreach (var member in Crew.Where(member => member.Hired))
            {
                member.Hired = false;
                member.System = member.Home;
            }
            if (lostJarek)
                JarekQuest = 0;
            if (Artifact)
            {
                Artifact = false;
                ArtifactQuest = 0;
            }

            if (Cash > Catalog.FleaCost)
                Cash -= Catalog.FleaCost;
            else
            {
                Debt += Catalog.FleaCost - Cash;
                Cash = 0;
            }

            BecomeFlea();
            Days += 3;
            Say($"The escape pod fires. {why} Three days later the Corps sells you a Flea.");
            if (Traveling)
                Arrive();
            return;
        }

        End = GameEnd.Killed;
        Traveling = false;
        Hull = 0;
        Say($"The hull fails. {why}");
    }

    void BecomeFlea()
    {
        Ship = 0;
        Hull = Spec.Hull;
        Fuel = Spec.Fuel;
        Weapons = [-1, -1, -1];
        Shields = [-1, -1, -1];
        Gadgets = [-1, -1, -1];
        Cargo = new int[Catalog.Goods.Length];
        Paid = new int[Catalog.Goods.Length];
        EscapePod = false;
        Insurance = false;
        ShieldCharge = 0;
        Tribbles = 0;
    }

    void BuyInto(int[] slots, int capacity, int id, string name, int basePrice, TechLevel tech, Action<int> assign)
    {
        if (capacity <= 0)
        {
            Say($"This {Spec.Name} has no free mount for that.");
            return;
        }
        if (Here.Tech < tech)
        {
            Say($"{name} is not sold at this tech level.");
            return;
        }

        var slot = -1;
        for (var i = 0; i < capacity; i++)
            if (slots[i] < 0)
            {
                slot = i;
                break;
            }
        if (slot < 0)
        {
            Say("There is no empty slot.");
            return;
        }

        var price = GearPrice(basePrice);
        if (Debt > 0)
        {
            Say("Clear your debt before buying equipment.");
            return;
        }
        if (Cash < price)
        {
            Say($"{name} costs {price:N0} cr.");
            return;
        }

        Cash -= price;
        assign(slot);
        Say($"Installed {name} for {price:N0} cr.");
    }

    void SellFrom(int[] slots, int capacity, int slot, Func<int, string> name, Func<int, int> price)
    {
        if (slot < 0 || slot >= capacity || slots[slot] < 0)
            return;
        var refund = price(slots[slot]) * 3 / 4;
        var label = name(slots[slot]);
        slots[slot] = -1;
        Cash += refund;
        Say($"Sold the {label} for {refund:N0} cr.");
    }

    bool HasContraband(Politics politics)
    {
        if (politics.Reaction <= 0)
            return false;
        return Cargo[(int)Good.Firearms] > 0 || Cargo[(int)Good.Narcotics] > 0;
    }

    int ContrabandValue(StarSystem dest)
    {
        var politics = dest.PoliticsInfo;
        var total = 0;
        foreach (var good in new[] { (int)Good.Firearms, (int)Good.Narcotics })
        {
            if (Cargo[good] <= 0)
                continue;
            var price = Catalog.StandardPrice(Catalog.Goods[good], good, dest.Tech, dest.Size, politics, dest.Resource);
            total += Cargo[good] * Math.Max(price, 1);
        }

        return Math.Max(200, total);
    }

    void DumpIllegal()
    {
        Cargo[(int)Good.Firearms] = 0;
        Paid[(int)Good.Firearms] = 0;
        Cargo[(int)Good.Narcotics] = 0;
        Paid[(int)Good.Narcotics] = 0;
    }

    void StealCargo()
    {
        var guard = 0;
        var left = Math.Max(1, UsedBays / 2);
        while (left > 0 && UsedBays > 0 && guard++ < 100)
        {
            var good = 0;
            for (var i = 1; i < Cargo.Length; i++)
                if (Cargo[i] > Cargo[good])
                    good = i;
            if (Cargo[good] <= 0)
                break;
            RemoveCargo(good, 1);
            left--;
        }
    }

    void RemoveCargo(int good, int qty)
    {
        qty = Math.Min(qty, Cargo[good]);
        if (qty <= 0)
            return;
        var left = Cargo[good] - qty;
        Paid[good] = left <= 0 ? 0 : Paid[good] * left / Cargo[good];
        Cargo[good] = left;
    }

    int SkillBase(int slot) => slot switch
    {
        0 => Pilot,
        1 => Fighter,
        2 => Trader,
        _ => Engineer
    };

    void AddSkill(int slot, int amount)
    {
        switch (slot)
        {
            case 0:
                Pilot = Math.Min(Catalog.MaxSkill, Pilot + amount);
                break;
            case 1:
                Fighter = Math.Min(Catalog.MaxSkill, Fighter + amount);
                break;
            case 2:
                Trader = Math.Min(Catalog.MaxSkill, Trader + amount);
                break;
            default:
                Engineer = Math.Min(Catalog.MaxSkill, Engineer + amount);
                break;
        }
    }
}
