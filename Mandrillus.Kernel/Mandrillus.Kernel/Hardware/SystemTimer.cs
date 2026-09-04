using Mosa.DeviceSystem.HardwareAbstraction;

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
    // Ticks is a 64-bit counter written a word at a time by PitTimer.OnInterrupt()
    // (an IRQ handler) while normal code (e.g. UptimeCommand) reads it
    // concurrently. On this 32-bit target, a 64-bit read/write is two 32-bit
    // operations - a reader interrupted mid-read by IRQ0 could observe a torn
    // value if unprotected (rare in practice - only visible when the low 32
    // bits wrap and carry into the high 32 bits, ~every 2^32 ticks, ~199 days
    // at 250Hz - but a real correctness gap, flagged during Issue #9 code
    // review via GitHub Copilot's automated PR review).
    //
    // An earlier version of this fix used a seqlock, but Mosa.Korlib does not
    // define System.Runtime.CompilerServices.IsVolatile - the marker type the
    // C# compiler injects as a modreq for the `volatile` keyword - so `volatile`
    // simply isn't usable on this target (CS0518: confirmed via a real build
    // failure; a 7th confirmed Korlib/runtime gap, alongside Dictionary,
    // string.Join, Array.Copy, double/float-to-string, and the
    // ConvertU64ToR8/ConvertI64ToR8 compiler gap). Without volatile, a seqlock's
    // correctness guarantee doesn't actually hold - the compiler is free to
    // cache a "non-volatile" field read across loop iterations.
    //
    // Instead: protect only the READER side with HAL.DisableAllInterrupts()/
    // EnableAllInterrupts() (Mosa.DeviceSystem.HardwareAbstraction.HAL -
    // confirmed public, platform-agnostic, same plug-resolution pattern as
    // HAL.Yield() -> resolves through Mosa.Kernel.BareMetal.Platform.Interrupt
    // -> Native.Cli()/Sti() on x86). The WRITER (IncrementTicks(), called from
    // within PitTimer.OnInterrupt()) does NOT wrap itself in disable/enable:
    // it already runs inside an IRQ handler, where the CPU's interrupt-gate
    // mechanism has interrupts masked for the duration - calling
    // EnableAllInterrupts() there would prematurely re-enable interrupts
    // before this ISR's own IRET, which is not something to do casually.
    // HAL.DisableAllInterrupts()/EnableAllInterrupts() are a flat disable/
    // enable (no save/restore of the prior IF state) - fine here since these
    // critical sections are leaves, never nested.
    private static ulong _ticksValue;

    /// <summary>
    /// Raw tick count, incremented once per PIT IRQ0. Never reset - this is the
    /// single source of truth both for <see cref="UptimeSeconds"/> and for any
    /// interval measured via <see cref="StartMeasuring"/>/<see cref="ElapsedSeconds"/>.
    /// Reads are protected against torn values via a brief interrupt-disable
    /// window (see the remarks on <see cref="_ticksValue"> above) - never observes
    /// a partially-updated value.
    /// </summary>
    public static ulong Ticks
    {
        get
        {
            HAL.DisableAllInterrupts();
            var value = _ticksValue;
            HAL.EnableAllInterrupts();

            return value;
        }
    }

    /// <summary>
    /// Increments <see cref="Ticks"/> by one. Only <see cref="PitTimer.OnInterrupt"/>
    /// should call this - it's the sigle writer, and dit's assumed to already
    /// be running inside an IRQ handler (interrupts masked by the CPU for the
    /// duration), so no additional locking happens here - see the remarkson
    /// <see cref="_ticksValue"/> above for why.
    /// </summary>
    internal static void IncrementTicks()
    {
        _ticksValue++;
    }

    /// <summary>
    /// Resets <see cref="Ticks"/> to zero. Only <see cref="PitTimer.Initialize"/>
    /// shoudl call this, once, during driver setup (before interrupts are
    /// live) - no locking needed at that point, but wrapped for consistency
    /// and safety against futurre callers.
    /// </summary>
    internal static void ResetTicks()
    {
        HAL.DisableAllInterrupts();
        _ticksValue = 0;
        HAL.EnableAllInterrupts();
    }

    /// <summary>
    /// The real frequency (Hz) the PIT's channel 0 is currently programmed to.
    /// Set once by <see cref="PitTimer"/> during initialization, computed from
    /// the actual divisor used (not necessarily identical to the requested
    /// target - see PitTimer.Initialize()'s rounding note). Needed to convert
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

            var ticks = Ticks;
            var wholeSeconds = ticks / FrequencyHz;
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
