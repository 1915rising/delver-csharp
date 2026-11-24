namespace Delver;

/// <summary>Base class for anything that occupies a tile, has health, and can fight.</summary>
public abstract class Entity
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int MinDamage { get; set; }
    public int MaxDamage { get; set; }
    public char Symbol { get; protected set; }
    public string Name { get; protected set; } = "";

    public bool IsAlive => Hp > 0;

    /// <summary>Rolls one attack's damage, inclusive of both bounds.</summary>
    public int RollDamage(Random rng) => rng.Next(MinDamage, MaxDamage + 1);
}
