using System;
using Mandrillus.Kernel.Hardware;

namespace Mandrillus.Kernel.Shell.Commands;

public static class UptimeCommand
{
    public static void Execute(string[] args)
    {
        Console.WriteLine("Ticks: " + (uint)SystemTimer.Ticks);
        Console.WriteLine("Frequency (Hz): " + SystemTimer.FrequencyHz);

        // Mosa.Korlib has no double/float-to-string formatting anywhere
        // (confirmed: no Double.ToString() override, no Numbers.cs entry,
        // exhaustive search found nothing). "text" + double silently hangs
        // via ValueType.ToString() -> GetType().ToString() (boxing +
        // reflection). Format manually from integer parts instead.
        if (SystemTimer.FrequencyHz == 0)
        {
            Console.WriteLine("Uptime: unknown (frequency is zero)");
            return;
        }

        var wholeSeconds = (uint)(SystemTimer.Ticks / SystemTimer.FrequencyHz);
        var remainderTicks = (uint)(SystemTimer.Ticks % SystemTimer.FrequencyHz);

        // remainderTicks is a raw tick count (0..FrequencyHz-1), NOT a
        // base-10 fraction of a second - printing it directly after the
        // decimal point (e.g. "7.36") is numerically wrong (caught in code
        // review via GitHub Copilot's automated PR review): at 250 Hz, a
        // remainder of 36 ticks is 36/250 = 0.144s, not "0.36s". Scale to
        // hundreths via pure integer arithmetic instead - never converting
        // to double, same category of workaround as the ConvertU64ToR8 gap.
        var hundreths = (remainderTicks * 100) / SystemTimer.FrequencyHz;

        // Zero-paid manually to 2 digits - string.Format/PadLeft support in
        // Mosa.Korlib is unconfirmed, so sticking to the same manual-loop
        // style already used elsewhere (see EchoCommand.cs's string.Join
        // workaround).
        var hundrethsString = hundreths < 10 ? "0" + hundreths : hundreths.ToString();

        Console.WriteLine("Uptime (seconds): " + wholeSeconds + "." + hundrethsString);
    }
}
