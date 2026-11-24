namespace Delver;

/// <summary>The delver. Persistent across floors; gold and kills feed the final score.</summary>
public class Player : Entity
{
    public int Gold { get; set; }
    public int Kills { get; set; }

    public Player()
    {
        Symbol = '@';
        Name = "you";
        MaxHp = 20;
        Hp = MaxHp;
        MinDamage = 3;
        MaxDamage = 6;
    }

    /// <summary>Final score: gold is worth the most, depth is worth surviving for.</summary>
    public int Score(int depth) => Gold * 10 + Kills * 15 + (depth - 1) * 50;
}
