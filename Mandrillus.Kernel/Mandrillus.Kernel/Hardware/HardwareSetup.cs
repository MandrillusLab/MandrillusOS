using System;
using System.Collections.Generic;
using Mosa.DeviceSystem.Framework;
using Mosa.DeviceSystem.Framework.ISA;
using Mosa.DeviceSystem.HardwareAbstraction;
using Mosa.DeviceSystem.Services;

namespace Mandrillus.Kernel.Hardware;

/// <summary>
/// Manual registration point for hardware drivers that MOSA's own boot pipeline
/// does not (and cannot, from Mandrillus code) register automatically.
/// 
/// Background (Issue #9 investigation): MOSA's Mosa.Kernel.BareMetal.Startup.EntryPoint()
/// - itself plugged onto Mosa.Runtime.Startup::KernelEntryPoint - registers and starts
/// every built-in ISA/PCI driver via Mosa.DeviceDriver.Setup.GetDeviceDriverRegistryEntries(),
/// a fixed list baked into the Mosa.Kernel.BareMetal NuGet package. That list is not
/// partial, has no extension hook, and runs entirely before Program.EntryPoint() (this
/// project's own code) is ever called - so there's no way to add an entry to it from here.
/// 
/// Confirmed there's no simpler path: Mosa.Compiler.Framework's plug resolution
/// (PlugSystem.CheckForPlug) returns the first matching [Plug] target it finds among
/// ALL types across ALL referenced assemblies, with no duplicate detection - so a second
/// [Plug("Mosa.Runtime.StartUp::KernelEntryPoint")] here would be fragile at best, and
/// would require reimplementing MOSA's entire boot sequence (GDT, IDT, memory, scheduler,
/// disk/keyboard drivers) just to add one timer. Not worth it for this.
/// 
/// Instead, this calls the same public API that Mosa.DeviceSystem.Services.ISADeviceService
/// uses internally for every entry in its own registry - DeviceService.Initialize(...) -
/// directly, for a single entry, from here. This runs the exact same pipeline
/// (Setup -> Initialize -> Probe -> Start -> AddInterruptHandler) that the automatic
/// path would have run, just triggered manually and later (from Program.EntryPoint(),
/// after MOSA's own boot has already made Kernel.ServiceManager available).
/// </summary>
public static class HardwareSetup
{
    /// <summary>
    /// Registers and starts <see cref="PitTimer"/> on IRQ0, alongside MOSA's own
    /// Scheduler.ClockInterrupt (Option B from the Issue #9 design - see
    /// constraints.md#hardware-do-pit-fatos-não-decisão-de-projeto). Call once from
    /// Program.cs's EntryPoint(), after Boot.cs has handed off control - same timing
    /// convention as Drill.Start().
    /// </summary>
    public static void RegisterPitTimer()
    {
        var deviceService = Mosa.Kernel.BareMetal.Kernel.ServiceManager.GetFirstService<DeviceService>();

        var entry = new ISADeviceDriverRegistryEntry
        {
            Name = "PIT Timer",
            BasePort = 0x40,
            PortRange = 1,
            AltBasePort = 0x43,
            AltPortRange = 1,
            IRQ = 0,
            Factory = () => new PitTimer()
        };

        // Mirrors exactly what ISADeviceService.Initialize() builds for each of its
        // own registry entries - BasePort becomes IOPortRegion[0] (data0Index in
        // PitTimer.cs), AltBasePort becomes IOPortRegion[1] (commandIndex).
        var ioPortRegions = new List<IOPortRegion>
        {
            new IOPortRegion(entry.BasePort, entry.PortRange),
            new IOPortRegion(entry.AltBasePort, entry.AltPortRange)
        };

        var hardwareResources = new HardwareResources(ioPortRegions, new List<AddressRegion>(), entry.IRQ);

        // autoStart: true drives the full Setup -> Initialize -> Probe -> Start ->
        // AddInterruptHandler sequence in one call - see DeviceService.StartDevice()
        // for the exact order this follows internally.
        deviceService.Initialize(entry, null, true, null, hardwareResources, DeviceBusType.ISA);
    }

    /// <summary>
    /// Workaround for a Hyper-V Generation 1-specific PS/2 controller quirk
    /// (confirmed empirically, Issue #9-adjacent investigation): on Hyper-V,
    /// the virtual 8042 controller's output buffer appears to start in a
    /// "stuck" stage after a full power cycle (Turn Off + Start - a plain
    /// "Reset" is NOT sufficient to repoduce or fix this) that silently
    /// prevents IRQ1 from ever firing for real keystrokes, even though the
    /// Controller Configuration Byte's IRQ1-enable bit (bit 0) is already
    /// correctly set by StandardMouse.Initialize() (Source.Mosa.DeviceDriver/
    /// ISA/StandardMouse.cs:53-65). QEMU never exhibits this - keyboard
    /// interrupts fire correctly there without this workaround.
    /// 
    /// Performing one read of the Controller Configuration Byte (command
    /// 0x20 to port 0x64, wait for OBF, read port 0x60) early in boot - the
    /// exact value read is irrelevant - reliably "kicks" the Hyper-V virtual
    /// controller into actually generating interrupts afterward. Confirmed
    /// reproducible across 3 consecutive full power cycles on Hyper-V.
    /// Mechanism is not fully understood (plausibly related to a known Hyper-V
    /// PS/2 emulation quirk where it only asserts an interrupt on pushing a
    /// byte into the datastream - see ToaruOS issue #296 for a similar
    /// finding on a different bare-metal OS). Call once from Program.cs's
    /// EntryPoint(), before Drill.Start() - harmless on QEMU even if this
    /// read times out there (keyboard already works fine on QEMU regardless).
    /// </summary>
    public static void KickHyperVPS2Controller()
    {
        var command = new Mosa.DeviceSystem.HardwareAbstraction.IOPortReadWrite(0x64);
        var data = new Mosa.DeviceSystem.HardwareAbstraction.IOPortReadWrite(0x60);

        command.Write8(0x20); // "read Controller Configuration Byte" - value unused, the read itself is what matters

        var timeout = 100000;
        while (timeout > 0 && (command.Read8() & 0x01) != 0x01)
            timeout--;

        // IMPORTANT: keep this as an early-return guard, NOT collapsed into
        // a single "if (timeout > 0) data.Read8();" - empirically, the two
        // shapes are logically identical C# but produced DIFFERENT compiled
        // behavior on QEMU (confirmed via real testing): the collapsed form
        // caused boot to hang indefinitely right after this function runs,
        // until a real keystroke arrived - consistent with data.Read8()
        // executing even when timeout == 0 in that compiled shape, wedging
        // QEMU's virtual 8042 the same way Hyper-V's gets wedged without the
        // kick. Root cause not confirmed (would need IL/asm inspection),
        // but treat this exact shape as load-bearing until investigated
        // further - possible MOSA compiler codegen quirk, same category as
        // the IsVolatile and ConvertU64ToR8/ConvertI64ToR8 gaps already
        // documented in constraints.md.
        if (timeout == 0)
            return;

        data.Read8(); // discard - consuming the byte is what unsticks Hyper-V's controller
    }
}
