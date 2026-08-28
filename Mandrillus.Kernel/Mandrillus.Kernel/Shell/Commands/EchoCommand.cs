using System;

namespace Mandrillus.Kernel.Shell.Commands;

/// <summary>
/// Built-in 'echo' command - prints back the given arguments, joined with spaces.
/// </summary>
public static class EchoCommand
{
    public static void Execute(string[] args)
    {
        // Mosa.Korlib (the corlib actually pulled in via Mosa.Runtime on the
        // BareMetal target) does not implement string.Join — only
        // string.Concat and string.Format overloads (no separator support).
        // string.Join exists solely in Mosa.TinyCoreLib, an alternative
        // corlib this project does not reference. Joining manually with
        // a loop instead. See Drill.cs's Dictionary<TKey,TValue> comment
        // for the same category of Korlib-vs-TinyCoreLib API gap.
        var joined = string.Empty;

        for (var i = 0; i < args.Length; i++)
        {
            if (i > 0)
                joined += " ";

            joined += args[i];
        }

        Console.WriteLine(joined);
    }
}
