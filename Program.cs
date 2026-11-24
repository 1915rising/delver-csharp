namespace Delver;

/// <summary>Entry point. Usage: Delver [--seed &lt;int&gt;]</summary>
public static class Program
{
    public static void Main(string[] args)
    {
        int? seed = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--seed" && int.TryParse(args[i + 1], out int parsed))
                seed = parsed;
        }

        var game = new Game(seed);
        game.Run();
    }
}
