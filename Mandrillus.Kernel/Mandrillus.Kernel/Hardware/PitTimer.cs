using Mosa.DeviceSystem.Framework;

namespace Mandrillus.Kernel.Hardware;

/// <summary>
/// Driver for the PIT (Programable Interval Timer, 8253/8254 chip), channel 0.
/// Registers on IRQ0 alongside MOSA's own Scheduler.ClockInterrupt (Option B from
/// the Issue #9 design investigation - confirmed via direct inspection of
/// Mosa.Kernel.BareMetal.x86/IDT.cs and Mosa.DeviceSystem/Services/DeviceService.cs
/// that DeviceService.IRQDispatch supports multiple handlers per IRQ without
/// modifying MOSA itself). Both this driver's OnInterrupt() and the Scheduler's
/// ClockInterrupt run unconditionally on every IRQ0 tick.
/// 
/// Design note (deliberate departure from Cosmos.HAL/PIT.cs, used only as design
/// inspiration, not copied code): Cosmos always rearms the countdown to the
/// maximum value (65535) on every IRQ, regardless of the configured "logical"
/// frequency - this caps real granularity at ~54.9ms even though its PITTimer API
/// accepts nanosecond values. This driver avoids  that limitation by programming
/// the PIT to the actual target frequency up front (see Initialize()), so the
/// hardware itself generates ticks at that real rate - no per-IRQ reprogramming
/// or rearming logic is needed here, since channel 0 in Mode 2 (rate generator)
/// free-runs at the configured divisor automatically.
/// 
/// Hardware constraint (not an architecture choice): the 8253/8254 has a single
/// physical channel 0 - this driver and MOSA's Scheduler.ClockInterrupt share the
/// same underlying frequency. This is unrelated to 32-bit vs 64-bit or BIOS
/// concenrns; it's a property of the chip itself.
/// </summary>
public class PitTimer : BaseDeviceDriver
{
    // Canonical PIT ports (see constraints.md#hardware-do-pit-fatos-não-decisão-de-projeto).
    // Mapped via Device.Resources, not Platform.IO/HAL directly, since this driver
    // goes through the ISA device framework: BasePort (0x40) becomes region index 0,
    // AltBasePort (0x43) becomes region index 1 - confirmed against the same pattern
    // Standard.Keyboard.cs uses for its data/status ports.
    private const int Data0Index = 0;
    private const int CommandIndex = 1;


    // The PIT's base oscillator frequency (Hz). This is the well-known hardware
    // constant for the 8253/8254 chip (~1.193282 MHz), not something Mandrillus
    // chooses. Used to compute the divisor for a target frequency.
    private const uint PitBaseFrequancyHz = 1193182;

    // Target tick rate for SystemTimer. 1000 Hz (1ms resolution) is a common,
    // reasonable choice for a general-purpose system tick - adjust here if a
    // different resolution is needed later. Must fit in the PIT's 16-bit divisor
    // (i.e. PitBaseFrequencyHz / TargetFrequencyHz must be <= 65535).
    private const uint TargetFrequencyHz = 1000;

    private Mosa.DeviceSystem.HardwareAbstraction.IOPortReadWrite data0Port;
    private Mosa.DeviceSystem.HardwareAbstraction.IOPortWrite commandPort;

    public override void Initialize()
    {
        Device.Name = "PIT Timer";

        data0Port = Device.Resources.GetIOPortReadWrite(Data0Index, 0);
        commandPort = Device.Resources.GetIOPortWrite(CommandIndex, 0);

        var divisor = PitBaseFrequancyHz / TargetFrequencyHz;

        // Mode 2 (rate generator) + 16-bit binary, both LSB and MSB access.
        // Command byte layout (8253/8254): channel select | access mode | operating mode | BCD/binary.
        // 0x34 = channel 0, lobyte/hibyte access, mode 2 (rate generator), binary mode.
        commandPort.Write8(0x34);

        // Programming sequence is mode -> LSB -> MSB (already written mode above).
        data0Port.Write8((byte)(divisor & 0xFF));
        data0Port.Write8((byte)(divisor >> 8));

        SystemTimer.FrequencyHz = TargetFrequencyHz;
        SystemTimer.Ticks = 0;
    }

    public override void Probe() => Device.Status = DeviceStatus.Available;

    public override void Start() => Device.Status = DeviceStatus.Online;

    public override bool OnInterrupt()
    {
        // Deriberately minimal: no memory allocation here, consistent with the
        // same caution already documented for Schedule-adjacent interrupt code
        // (see the Issue #9 IRQ handler investigation notes) - this runs on every
        // IRQ0 tick, right alongside Schedule.ClockInterrupt.
        SystemTimer.Ticks++;

        return true;
    }
}
