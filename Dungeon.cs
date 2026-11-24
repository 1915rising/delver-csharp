namespace Delver;

public enum Tile
{
    Wall,
    Floor,
}

/// <summary>
/// One procedurally generated dungeon floor: rectangular rooms joined by
/// L-shaped corridors, with a start point, an exit, and scattered gold.
/// Generation is driven entirely by the supplied <see cref="Random"/>,
/// so a seed always produces the same floor.
/// </summary>
public class Dungeon
{
    public const int Width = 64;
    public const int Height = 22;

    private readonly Tile[,] _tiles = new Tile[Width, Height];
    private readonly HashSet<(int X, int Y)> _gold = new();

    public (int X, int Y) PlayerStart { get; private set; }
    public (int X, int Y) Exit { get; private set; }

    private sealed class Room
    {
        public int X, Y, W, H;
        public (int X, int Y) Center => (X + W / 2, Y + H / 2);

        public bool Overlaps(Room other, int padding) =>
            X - padding < other.X + other.W &&
            X + W + padding > other.X &&
            Y - padding < other.Y + other.H &&
            Y + H + padding > other.Y;
    }

    public Dungeon(Random rng, int depth)
    {
        var rooms = new List<Room>();

        // Scatter non-overlapping rooms.
        for (int attempt = 0; attempt < 40 && rooms.Count < 10; attempt++)
        {
            var room = new Room
            {
                W = rng.Next(4, 10),
                H = rng.Next(3, 7),
            };
            room.X = rng.Next(1, Width - room.W - 1);
            room.Y = rng.Next(1, Height - room.H - 1);

            if (rooms.Any(r => r.Overlaps(room, 1)))
                continue;

            for (int x = room.X; x < room.X + room.W; x++)
                for (int y = room.Y; y < room.Y + room.H; y++)
                    _tiles[x, y] = Tile.Floor;

            rooms.Add(room);
        }

        // Join the rooms in sequence so the whole floor is reachable.
        for (int i = 1; i < rooms.Count; i++)
            CarveCorridor(rooms[i - 1].Center, rooms[i].Center, rng);

        PlayerStart = rooms[0].Center;

        // The exit goes in the room farthest from the start.
        var exitRoom = rooms
            .OrderByDescending(r => DistanceSquared(r.Center, PlayerStart))
            .First();
        Exit = exitRoom.Center;

        // Gold: a handful of piles on random floor tiles, never on the start or exit.
        int goldCount = 6 + depth;
        for (int placed = 0, guard = 0; placed < goldCount && guard < 500; guard++)
        {
            var spot = (rng.Next(Width), rng.Next(Height));
            if (IsFloor(spot.Item1, spot.Item2) && spot != PlayerStart && spot != Exit)
            {
                _gold.Add(spot);
                placed++;
            }
        }
    }

    private static int DistanceSquared((int X, int Y) a, (int X, int Y) b)
    {
        int dx = a.X - b.X, dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    /// <summary>Carves an L-shaped corridor between two points, in a random axis order.</summary>
    private void CarveCorridor((int X, int Y) from, (int X, int Y) to, Random rng)
    {
        int x = from.X, y = from.Y;

        void StepHorizontal()
        {
            while (x != to.X)
            {
                _tiles[x, y] = Tile.Floor;
                x += Math.Sign(to.X - x);
            }
        }

        void StepVertical()
        {
            while (y != to.Y)
            {
                _tiles[x, y] = Tile.Floor;
                y += Math.Sign(to.Y - y);
            }
        }

        if (rng.Next(2) == 0)
        {
            StepHorizontal();
            StepVertical();
        }
        else
        {
            StepVertical();
            StepHorizontal();
        }

        _tiles[x, y] = Tile.Floor;
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public bool IsFloor(int x, int y) => InBounds(x, y) && _tiles[x, y] == Tile.Floor;

    public bool HasGold(int x, int y) => _gold.Contains((x, y));

    /// <summary>Removes the gold at a position, returning true if there was any.</summary>
    public bool TakeGold(int x, int y) => _gold.Remove((x, y));

    /// <summary>All walkable tiles, used when scattering monsters.</summary>
    public IEnumerable<(int X, int Y)> FloorTiles()
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (_tiles[x, y] == Tile.Floor)
                    yield return (x, y);
    }
}
