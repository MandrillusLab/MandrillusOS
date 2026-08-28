using System;

namespace Mandrillus.Kernel.Shell.Commands;

/// <summary>
/// Built-in 'help'command - lists every registered command with its description.
/// </summary>
public static class HelpCommand
{
    public static void Execute(string[] args)
    {
        Console.WriteLine("Available commands:");

        for (var i = 0; i < Drill.CommandList.Count; i++)
        {
            Console.WriteLine("  " + Drill.CommandList[i].Name + " - " + Drill.CommandList[i].Description);
        }
    }
}
