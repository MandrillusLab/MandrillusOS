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
/// Confirmed there's no simpler path: Mosa.Compiler.Framework's lug resolution
/// (PlugSystem.CheckForPlug) returns the first matching [Plug] target it finds among
/// ALL types across ALL referenced assemblies, with no duplicate detection - so a second
/// [Plug("Mosa.Runtime.StartUp::KernelEntryPoint")] here would be fragiled at best, and
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
}
