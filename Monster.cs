namespace Delver;

/// <summary>
/// A dungeon monster. Movement and combat live in <see cref="Game"/>;
/// this class is the stat block plus the depth-aware factory.
/// </summary>
public class Monster : Entity
{
    /// <summary>How far (Chebyshev distance) the monster can sense the player.</summary>
    public int SightRange { get; private set; }

    private Monster(string name, char symbol, int hp, int minDamage, int maxDamage, int sightRange)
    {
        Name = name;
        Symbol = symbol;
        MaxHp = hp;
        Hp = hp;
        MinDamage = minDamage;
        MaxDamage = maxDamage;
        SightRange = sightRange;
    }

    /// <summary>
    /// Picks a monster kind for the given depth. Rats dominate the shallows;
    /// orcs show up as you go deeper. Everything gains a little health with depth.
    /// </summary>
    public static Monster CreateRandom(Random rng, int depth)
    {
        int bonusHp = depth - 1;
        int roll = rng.Next(100);

        if (depth >= 2 && roll < 25)
            return new Monster("orc", 'o', 11 + bonusHp, 2, 4, 8);
        if (roll < 60)
            return new Monster("goblin", 'g', 6 + bonusHp, 1, 3, 8);
        return new Monster("rat", 'r', 3 + bonusHp, 1, 2, 6);
    }
}
