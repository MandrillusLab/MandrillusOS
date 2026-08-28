using System;

namespace Mandrillus.Kernel.Shell;

/// <summary>
/// Delegate for a Drill command handler.
/// </summary>
/// <param name="args">Tokenized arguments (command line excluded).</param>
public delegate void DrillCommandHandler(string[] args);

/// <summary>
/// Represents a single registrable Drill command: name, handler, and help text.
/// </summary>
public readonly struct DrillCommand
{
    public readonly string Name;
    public readonly string Description;
    public readonly DrillCommandHandler Handler;

    public DrillCommand(string name, string description, DrillCommandHandler handler)
    {
        Name = name;
        Description = description;
        Handler = handler;
    }
}
