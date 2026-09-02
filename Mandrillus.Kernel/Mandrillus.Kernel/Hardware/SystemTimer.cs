namespace Mandrillus.Kernel.Hardware;

/// <summary>
/// System-wide ticker counter and uptime, driven by <see cref="PitTimer"/>'s IRQ0 handler.
/// This is the minimal API for Issue #9 - a raw incrementing counter plus a few
/// conveniences for reading elapsed time. Deliberately does NOT include a
/// callback/scheduled-timer API (no PITTimer/OnTrigger-style registration) -
/// that's out of scope for now; see Cosmos.HAL/PIT.cs in the design references
/// for that pattern if/when it's needed later.
/// </summary>
public static class SystemTimer
{
    /// <summary>
    /// Raw tick count, incremented once per PIT IRQ0. Never reset - this is the
    /// single source of truth both for <see cref="UptimeSeconds"/> and for any
    /// interval measured via <see cref="StartMeasuring"/>/<see cref="ElapsedSeconds"/>.
    /// Only <see cref="PitTimer"/> should write to this; everything else should
    /// treat it as read-only.
    /// </summary>
    public static ulong Ticks { get; internal set; }

    /// <summary>
    /// The real frequency (Hz) the PIT's channel 0 is currently programmed to.
    /// Set once by <see cref="PitTimer"/> during initialization. Needed to convert
    /// raw <see cref="Ticks"/> into real time.
    /// </summary>
    public static uint FrequencyHz { get; internal set; }

    /// <summary>
    /// Time elapsed since boot, in seconds. Derived from <see cref="Ticks"/> and
    /// <see cref="FrequencyHz"/> - not itself a stored value.
    /// 
    /// IMPORTANT - Mosa.Compiler.x86 compiler limitation (confirmed via build
    /// error + direct source inspection of Source/Mosa.Compiler.x86/Transforms/BaseIR/):
    /// (1) ConvertU64ToR8 (ulong -> double) has NO implementation in MOSA's
    /// compiler at all - neither x86 nor x64 - so `(double)Ticks` fails to
    /// compile outright ("Missing Code Transformation").
    /// (2) Even ConvertI64ToR8 (long -> double), which IS implemented for x86,
    /// silently discards the high 32 bits of the value (its Transform() splits
    /// the operand and only converts the low half via Cvtsi2sd32) - so casting
    /// through `(double)(long)Ticks` would compile, but silently produce a
    /// WRONG result once Ticks exceeds ~2^31 (24.9 days of uptime at 1000 Hz).
    /// ConvertU32ToR8 (uint -> double), by contrast, IS correctly implemented
    /// for x86 (uses the 32-bit value directly, nothing discarded).
    /// 
    /// Workaround: do the division in integer (ulong/uint) arithmetic FIRST -
    /// which never touches R8 conversion - to get whole seconds and a small
    /// remainder, and only convert to double once the values are small enough
    /// to be safe. wholeSeconds only risks the same ~2^31 boundary after
    /// ~68 years of continuous uptime - not a practical concern. remainderTicks
    /// is always < FrequencyHz (e.g. < 1000), comfortably within safe range.
    /// </summary>
    public static double UptimeSeconds
    {
        get
        {
            if (FrequencyHz == 0)
                return 0;

            var wholeSeconds = Ticks / FrequencyHz;
            var remainderTicks = (uint)(Ticks % FrequencyHz);

            return (double)(long)wholeSeconds + (double)remainderTicks / FrequencyHz;
        }
    }

    /// <summary>
    /// Captures the current <see cref="Ticks"/> value as the start of a local
    /// interval measurement, without touching the global tick count (so it never
    /// affects <see cref="UptimeSeconds"/> or any other in-flight measurement).
    /// Pair with <see cref="ElapsedSeconds"/>:
    /// <code>
    /// var start = SystemTimer.StartMeasuring();
    /// // ... work to measure ...
    /// var elapsed = SystemTimer.ElapsedSeconds(start);
    /// </code>
    /// </summary>
    public static ulong StartMeasuring() => Ticks;

    /// <summary>
    /// Converts the difference between the current <see cref="Ticks"/> and a
    /// previously captured <paramref name="start"/> (from <see cref="StartMeasuring"/>)
    /// into elapsed seconds. Assumes <see cref="Ticks"/> has not wrapped around
    /// since <paramref name="start"/> was captured - with a ulong counter and any
    /// realistic PIT frequency, that would take billions of years, so  this is not
    /// a practical concern.
    /// 
    /// Same MOSA x86 compiler workaround as <see cref="UptimeSeconds"/> applies
    /// here - see that property's remarks for the full explanation. The elapsed
    /// tick delta is divided into whole-seconds/remainder using integer
    /// arithmetic before any conversion to double.
    /// </summary>
    /// <param name="start"></param>
    /// <returns></returns>
    public static double ElapsedSeconds(ulong start)
    {
        if (FrequencyHz == 0)
            return 0;

        var elapsedTicks = Ticks - start;
        var wholeSeconds = elapsedTicks / FrequencyHz;
        var remainderTicks = (uint)(elapsedTicks % FrequencyHz);

        return (double)(long)wholeSeconds + (double)remainderTicks / FrequencyHz;
    }
}
