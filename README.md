# Delver

A tiny text roguelike for the terminal, written in C#. Descend through procedurally generated dungeon floors, fight monsters, grab gold, and see how deep you can get before the dungeon gets you.

```
== DELVER ==   Depth 1
----------------------------------------------------------------
#####..........#############..........##########################
#####..........#############..........##########################
#####....@.....######..........*..........####################
#####..........######..........##########..........###########
#####....*.....######..........##########....g...............#
#####..........######..........##########..........###########
########.####################..........##########..........###
########.####################..........##################..###
########.############......##################..........##..###
########.############......##################....>.....#######
########.############......##################..........#######
----------------------------------------------------------------
HP: 20/20   Gold: 0   Kills: 0   Turns: 12   Score: 0
Descend. Get rich. Don't die.
```

## How to play

You are the `@`. Every floor is generated fresh: a set of rooms connected by corridors, scattered gold (`*`), monsters, and one exit (`>`) in the farthest room. Stepping on the exit generates the next, deeper floor — monsters get tougher and more numerous the deeper you go. Descending restores a little health, but not much. Death is permanent; your score is tallied when you fall or walk away.

### Controls

| Key | Action |
| --- | --- |
| `W` `A` `S` `D` or arrow keys | Move (walk into a monster to attack it) |
| `Space` | Wait one turn |
| `Q` | Quit |

Combat is bump-to-attack: just walk into a monster. Monsters chase you when you get close, and they hit back — rats are a nuisance, goblins are a threat, and orcs are a problem.

### Scoring

- Gold coin: 10 points
- Monster slain: 15 points
- Each new depth reached: 50 points

## Build and run

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```sh
dotnet run
```

For a reproducible dungeon, pass a seed:

```sh
dotnet run -- --seed 42
```

To build a standalone executable:

```sh
dotnet build -c Release
```

## Features

- Procedural generation: non-overlapping rooms joined by L-shaped corridors, fully connected by construction
- Turn-based movement and bump-to-attack combat with randomized damage
- Three monster types with simple chase AI and depth scaling
- Gold pickups, a persistent score, and endless descending floors
- Deterministic runs via `--seed`
- Plays in any terminal; also accepts piped input, so runs can be scripted

## Project layout

| File | Role |
| --- | --- |
| `Program.cs` | Entry point and `--seed` argument parsing |
| `Game.cs` | Turn loop, rendering, input, combat, monster AI |
| `Dungeon.cs` | Floor generation: rooms, corridors, exit, gold |
| `Entity.cs` | Base class for creatures: position, health, damage |
| `Player.cs` | The player and the scoring formula |
| `Monster.cs` | Monster stat blocks and the depth-aware factory |

## Tech

C# / .NET 8, single console project, no external packages.

## License

MIT — see [LICENSE](LICENSE).
