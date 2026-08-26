using System;

namespace Mandrillus.Kernel.Shell.Commands;

/// <summary>
/// Built-in 'clear' command - clears the console screen.
/// </summary>
public static class ClearCommand
{
    public static void Execute(string[] args)
    {
        Console.Clear();
    }
}
