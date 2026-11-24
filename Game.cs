using System.Text;

namespace Delver;

/// <summary>
/// Owns the run: the turn loop, rendering, input, combat, and monster AI.
/// The game is strictly turn-based — the world only moves when the player does.
/// </summary>
public class Game
{
    private readonly Random _rng;
    private readonly Player _player = new();
    private readonly List<Monster> _monsters = new();
    private readonly List<string> _turnMessages = new();

    private Dungeon _dungeon;
    private int _depth = 1;
    private int _turns;
    private bool _quit;

    public Game(int? seed)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        _dungeon = new Dungeon(_rng, _depth);
        PlacePlayerAtStart();
        SpawnMonsters();
    }

    public void Run()
    {
        while (_player.IsAlive && !_quit)
        {
            Render();
            char command = ReadCommand();

            _turnMessages.Clear();
            if (HandleCommand(command))
            {
                _turns++;
                ActMonsters();
                _monsters.RemoveAll(m => !m.IsAlive);
            }
        }

        RenderGameOver();
    }

    // ---- Setup -------------------------------------------------------------

    private void PlacePlayerAtStart()
    {
        _player.X = _dungeon.PlayerStart.X;
        _player.Y = _dungeon.PlayerStart.Y;
    }

    private void SpawnMonsters()
    {
        _monsters.Clear();
        int count = 4 + _depth * 2;

        // Candidate spots: floor tiles a fair distance from the player's start.
        var spots = _dungeon.FloorTiles()
            .Where(p => Math.Abs(p.X - _player.X) + Math.Abs(p.Y - _player.Y) > 8)
            .OrderBy(_ => _rng.Next())
            .Take(count);

        foreach (var (x, y) in spots)
        {
            var monster = Monster.CreateRandom(_rng, _depth);
            monster.X = x;
            monster.Y = y;
            _monsters.Add(monster);
        }
    }

    private void Descend()
    {
        _depth++;
        _dungeon = new Dungeon(_rng, _depth);
        PlacePlayerAtStart();
        SpawnMonsters();

        // A short breather between floors: descending restores a little health.
        _player.Hp = Math.Min(_player.MaxHp, _player.Hp + 4);
        _turnMessages.Add($"You descend to depth {_depth}.");
    }

    // ---- Input -------------------------------------------------------------

    /// <summary>
    /// Reads one command character. Uses key events on a real terminal, and
    /// falls back to a character stream when stdin is piped (scripts, tests).
    /// </summary>
    private char ReadCommand()
    {
        if (Console.IsInputRedirected)
        {
            int ch;
            do
            {
                ch = Console.In.Read();
                if (ch < 0)
                    return 'q'; // End of piped input: stop cleanly.
            } while (char.IsWhiteSpace((char)ch));
            return char.ToLowerInvariant((char)ch);
        }

        ConsoleKeyInfo key = Console.ReadKey(intercept: true);
        return key.Key switch
        {
            ConsoleKey.W or ConsoleKey.UpArrow => 'w',
            ConsoleKey.A or ConsoleKey.LeftArrow => 'a',
            ConsoleKey.S or ConsoleKey.DownArrow => 's',
            ConsoleKey.D or ConsoleKey.RightArrow => 'd',
            ConsoleKey.Q => 'q',
            ConsoleKey.Spacebar => '.',
            _ => '\0',
        };
    }

    /// <summary>Returns true if the command consumed a turn.</summary>
    private bool HandleCommand(char command)
    {
        switch (command)
        {
            case 'q':
                _quit = true;
                return false;
            case '.': // Wait in place.
                return true;
            case 'w': return TryMovePlayer(0, -1);
            case 'a': return TryMovePlayer(-1, 0);
            case 's': return TryMovePlayer(0, 1);
            case 'd': return TryMovePlayer(1, 0);
            default:
                return false; // Unrecognized keys don't consume a turn.
        }
    }

    // ---- Player actions ----------------------------------------------------

    private bool TryMovePlayer(int dx, int dy)
    {
        int nx = _player.X + dx;
        int ny = _player.Y + dy;

        // Bump-to-attack: moving into a monster attacks it instead.
        Monster? target = MonsterAt(nx, ny);
        if (target != null)
        {
            int damage = _player.RollDamage(_rng);
            target.Hp -= damage;
            _turnMessages.Add(target.IsAlive
                ? $"You hit the {target.Name} for {damage}."
                : $"You hit the {target.Name} for {damage} and it dies!");
            if (!target.IsAlive)
                _player.Kills++;
            return true;
        }

        if (!_dungeon.IsFloor(nx, ny))
            return false; // Walking into a wall doesn't consume a turn.

        _player.X = nx;
        _player.Y = ny;

        if (_dungeon.TakeGold(nx, ny))
        {
            _player.Gold++;
            _turnMessages.Add("You pick up a gold coin.");
        }

        if ((nx, ny) == _dungeon.Exit)
            Descend();

        return true;
    }

    // ---- Monsters ----------------------------------------------------------

    private Monster? MonsterAt(int x, int y) =>
        _monsters.FirstOrDefault(m => m.IsAlive && m.X == x && m.Y == y);

    private void ActMonsters()
    {
        foreach (Monster monster in _monsters.Where(m => m.IsAlive))
        {
            if (!_player.IsAlive)
                return;

            int dx = _player.X - monster.X;
            int dy = _player.Y - monster.Y;
            int chebyshev = Math.Max(Math.Abs(dx), Math.Abs(dy));

            if (chebyshev == 1 || (dx == 0 && dy == 0))
            {
                AttackPlayer(monster);
            }
            else if (chebyshev <= monster.SightRange)
            {
                StepToward(monster, dx, dy);
            }
            else if (_rng.Next(4) == 0)
            {
                // Unaware monsters mostly idle, sometimes shuffle.
                StepToward(monster, _rng.Next(-1, 2), _rng.Next(-1, 2));
            }
        }
    }

    /// <summary>
    /// Moves a monster one tile toward a target offset, preferring the axis
    /// with the larger gap and falling back to the other axis when blocked.
    /// </summary>
    private void StepToward(Monster monster, int dx, int dy)
    {
        int stepX = Math.Sign(dx);
        int stepY = Math.Sign(dy);

        var attempts = Math.Abs(dx) >= Math.Abs(dy)
            ? new[] { (stepX, 0), (0, stepY) }
            : new[] { (0, stepY), (stepX, 0) };

        foreach (var (sx, sy) in attempts)
        {
            if (sx == 0 && sy == 0)
                continue;

            int nx = monster.X + sx;
            int ny = monster.Y + sy;

            if (nx == _player.X && ny == _player.Y)
            {
                AttackPlayer(monster);
                return;
            }

            if (_dungeon.IsFloor(nx, ny) && MonsterAt(nx, ny) == null)
            {
                monster.X = nx;
                monster.Y = ny;
                return;
            }
        }
    }

    private void AttackPlayer(Monster monster)
    {
        int damage = monster.RollDamage(_rng);
        _player.Hp -= damage;
        _turnMessages.Add(_player.IsAlive
            ? $"The {monster.Name} hits you for {damage}."
            : $"The {monster.Name} hits you for {damage}. You die!");
    }

    // ---- Rendering -----------------------------------------------------------

    private void Render()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"== DELVER ==   Depth {_depth}");
        sb.AppendLine(new string('-', Dungeon.Width));

        for (int y = 0; y < Dungeon.Height; y++)
        {
            for (int x = 0; x < Dungeon.Width; x++)
                sb.Append(TileChar(x, y));
            sb.AppendLine();
        }

        sb.AppendLine(new string('-', Dungeon.Width));
        sb.AppendLine($"HP: {_player.Hp}/{_player.MaxHp}   Gold: {_player.Gold}   " +
                      $"Kills: {_player.Kills}   Turns: {_turns}   Score: {_player.Score(_depth)}");
        sb.AppendLine(_turnMessages.Count > 0
            ? string.Join(" ", _turnMessages)
            : "Descend. Get rich. Don't die.");
        sb.AppendLine("Move: WASD / arrows   Wait: Space   Quit: Q");

        if (!Console.IsOutputRedirected)
        {
            try { Console.Clear(); } catch (IOException) { /* Non-fatal on odd terminals. */ }
        }

        Console.Write(sb.ToString());
    }

    private char TileChar(int x, int y)
    {
        if (x == _player.X && y == _player.Y)
            return _player.Symbol;

        Monster? monster = MonsterAt(x, y);
        if (monster != null)
            return monster.Symbol;

        if ((x, y) == _dungeon.Exit)
            return '>';
        if (_dungeon.HasGold(x, y))
            return '*';
        return _dungeon.IsFloor(x, y) ? '.' : '#';
    }

    private void RenderGameOver()
    {
        Console.WriteLine();
        Console.WriteLine("===================================");
        Console.WriteLine(_quit ? "  You leave the dungeon... for now." : "  GAME OVER");
        Console.WriteLine("===================================");
        Console.WriteLine($"  Depth reached : {_depth}");
        Console.WriteLine($"  Gold collected: {_player.Gold}");
        Console.WriteLine($"  Monsters slain: {_player.Kills}");
        Console.WriteLine($"  Turns taken   : {_turns}");
        Console.WriteLine($"  FINAL SCORE   : {_player.Score(_depth)}");
        Console.WriteLine("===================================");
    }
}
