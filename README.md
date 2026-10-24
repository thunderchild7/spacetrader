# Space Trader

A Blazor WebAssembly port of the classic Space Trader trading game. You start in a Gnat with a pulse laser and 1,000 credits, trade across 128 named systems, and try to retire to a moon.

This is the Palm OS game by Pieter Spronck, as coded for Windows by Jay French. It is not [spacetraders.io](https://spacetraders.io/).

Play it at [thunderchild7.github.io/spacetrader](https://thunderchild7.github.io/spacetrader/) once GitHub Pages has published the `master` branch. The app is a standalone PWA: install it, and the service worker keeps a finished game available offline. Saves and high scores stay in this browser.

## Run it locally

```bash
dotnet run --project src/SpaceTrader.Wasm
```

The SDK in `global.json` is .NET 10. Open the printed localhost URL. Tests for the rules live in `tests/SpaceTrader.Core.Tests`:

```bash
dotnet test
```

## What this port plays

The headless rules are in `src/SpaceTrader.Core`. The Blazor UI is in `src/SpaceTrader.Wasm`. A new game uses the original galaxy size, price tables, ship list, politics, goods, and mercenary names.

- Skills take 20 points, from 1 to 10 each. Difficulty shifts the effective skill.
- Fuel cost is the distance in parsecs. Each warp is 20 clicks, and an encounter can interrupt the trip.
- Wormholes are a one-way cycle. The tax is 25 times the ship's fuel cost, and the jump spends no fuel.
- Encounters are pirates, police, traders, and the Acamar monster. You can attack, flee, surrender, bribe, submit, plunder, or trade.
- The yard trades the ship in from its hull, fuel, gear, and what you paid for the cargo. Normal gear and cargo do not move to the new ship. Crew must fit the new quarters. An escape pod transfers for 200 credits.
- Equipment is discounted by your trader skill. Selling gear returns three quarters of the list price.
- The bank charges 10% interest per warp. Insurance needs an escape pod.
- Debt of 75,000 credits is a warning. Debt above 100,000 grounds the ship: you can still sell and repay.
- Specials in this port: the beginner lottery, a 500,000-credit moon, erasing a police record, skill training, tribbles, the Japori antidote, the alien artifact, Jarek, the Acamar monster, and sealed canisters. Zeethibal can be hired at Kravat once your reputation is Dangerous (300).

The original Windows sources are kept, unmodified, in `legacy/`. They are a WinForms app and are not what the browser runs.

These original quests and extras are still only in `legacy/`: the dragonfly, the princess, the scarab, Gemulon, the reactor, the sculpture, the experiment and the singularity, the Marie Celeste, the full Wild unlock that leads to Zeethibal, famous captains, the custom shipyard, and the newspaper.

## License

Space Trader for Windows 2.01 is copyright (C) 2008 Jay French, with additional coding by David Pierron. The original game is by Pieter Spronck, Sam Anderson, Samuel Goldstein, and Matt Lee. This Blazor port is a derivative work under the GNU General Public License, version 2 or any later version. See `LICENSE`.
