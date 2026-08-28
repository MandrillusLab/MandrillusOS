using System;

namespace Mandrillus.Kernel.Shell.Commands;

/// <summary>
/// Built-in 'history' command — lists previously entered commands, oldest first,
/// reading directly from Drill's fixed-size ring buffer.
/// </summary>
public static class HistoryCommand
{
    public static void Execute(string[] args)
    {
        if (Drill._historyCount == 0)
        {
            Console.WriteLine("(empty)");
            return;
        }

        var oldestIndex = Drill._historyCount < Drill.HistoryCapacity ? 0 : Drill._historyNext;

        for (var i = 0; i < Drill._historyCount; i++)
        {
            var absoluteIndex = (oldestIndex + i) % Drill.HistoryCapacity;
            Console.WriteLine("  " + (i + 1) + "  " + Drill.History[absoluteIndex]);
        }
    }
}
