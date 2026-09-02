using System;
using Mandrillus.Kernel.Hardware;

namespace Mandrillus.Kernel.Shell.Commands;

public static class UptimeCommand
{
    public static void Execute(string[] args)
    {
        Console.WriteLine("Ticks: " + SystemTimer.Ticks);
        Console.WriteLine("Frequency (Hz): " + SystemTimer.FrequencyHz);

        // Mosa.Korlib has no double/float-to-string formatting anywhere
        // (confirmed: no Double.ToString() override, no Numbers.cs entry,
        // exhaustive search found nothing). "text" + double silently hangs
        // via ValueType.ToString() -> GetType().ToString() (boxing +
        // reflection). Format manually from integer parts instead.
        var wholeSeconds = SystemTimer.Ticks / SystemTimer.FrequencyHz;
        var remainderTicks = SystemTimer.Ticks % SystemTimer.FrequencyHz;

        Console.WriteLine("Uptime (seconds): " + wholeSeconds + "." + remainderTicks);
    }
}
