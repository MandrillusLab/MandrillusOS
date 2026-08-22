*Read this in other languages: [Português](ROADMAP.pt-BR.md)*

# Mandrillus Roadmap

This document tracks the development trajectory of **Mandrillus OS** and the broader **Mandrillus Systems** ecosystem — a small suite of applications built around a C#-native operating system, developed with the [MOSA Project](https://github.com/mosa/MOSA-Project).

Status legend: ✅ Done · 🚧 In Progress · ⏳ Planned · 💭 Concept

---

## Phase 0 — Foundation

- ✅ Solution structure: `Mandrillus.Kernel` (platform-agnostic) + `Mandrillus.Kernel.x86` (x86 target)
- ✅ MOSA template scaffolding (`mosakrnl`)
- ✅ MOSA packages pinned to `2.6.1.1669` (packaging regression workaround for 2.6.1.1694+)
- ✅ Boot validated on QEMU (via Mosa.Tool.Launcher)
- ✅ Boot validated on Hyper-V (Generation 1, Secure Boot disabled, manual `.img` → `.vhd` conversion)
- ✅ Licensing: MIT (Mandrillus Systems) + `THIRD-PARTY-LICENSES.md` (MOSA New BSD)
- ✅ Bilingual README (`README.md` / `README.pt-BR.md`)

## Phase 1 — Kernel Core (x86, 32-bit)

Tracked as GitHub Issues under the **"Phase 1 — Kernel Core"** milestone.

> **Note:** Investigation confirmed (via [MOSA docs](https://www.mosa-project.org/a-dive-into-baremetal.html), the actual boot log, and direct inspection of `Source/Mosa.DeviceDriver/ISA/` in the MOSA repository) that most — but not all — of Phase 1's low-level infrastructure is already provided by the MOSA BareMetal kernel that `Mandrillus.Kernel.x86` builds on. Items below marked *(inherited)* are not original work.

- ✅ GDT (Global Descriptor Table) setup *(inherited from MOSA BareMetal)* — #1
- ✅ IDT (Interrupt Descriptor Table) + basic interrupt handling *(inherited from MOSA BareMetal)* — #2
- ✅ Physical memory manager (page allocator) *(inherited from MOSA BareMetal)* — #3
- ✅ Virtual memory / paging *(inherited from MOSA BareMetal)* — #4
- ✅ Keyboard driver *(inherited from MOSA BareMetal — `StandardKeyboard`)* — #6
- ✅ Text-mode video driver *(inherited from MOSA BareMetal — `Console` API)* — #7
- ⏳ Minimal interactive shell (original work — ties keyboard + console output into a command loop) — #8

> **Note:** the PIT timer item (originally #5 here) was moved to Phase 2 — see below. Investigation confirmed the shell has no dependency on timer work; the timer is a prerequisite of preemptive scheduling instead. Full reasoning in the note under Phase 2.
>
> **Heads-up for Issue #8:** public discussion on the [MOSA Discord](https://discord.gg/tRNMn3npsv) (`#general` channel) describes the BareMetal scheduler as cooperative, not preemptive — a blocking keyboard read call was reported to freeze the entire system because nothing forced a context switch while it blocked. The shell's main loop should follow the same pattern the community has converged on: check for a key without blocking indefinitely, rather than calling a blocking read directly. See the Phase 2 note below for the related timer/scheduler context.

## Phase 2 — System Services

> **Note on the PIT timer:** deeper investigation (two independent AI-assisted research passes over the current MOSA `master` source) confirmed there's no PIT/8253/8254 driver anywhere in MOSA — but also revealed IRQ0 is *not* idle: `Mosa.Kernel.BareMetal`'s `Scheduler` already consumes it (`Scheduler.ClockInterrupt`) at whatever default frequency the BIOS/QEMU left the PIT running at (~18.2Hz, unconfigured). `HAL.Sleep()` exists as an API but its body is an empty `// TODO`. So this isn't "add a timer that doesn't exist" — it's "take control of a timer that's already silently driving the scheduler." That's why it belongs here, as a Phase 2 prerequisite of real preemptive scheduling, rather than in Phase 1 (which has zero dependency on it — see Phase 1 note above).
>
> This is independently reflected in public discussion on the [MOSA Discord](https://discord.gg/tRNMn3npsv) (`#operating-system` channel): the community has noted the Scheduler's clock tick frequency isn't documented or deliberately configured, matching what the source investigation found. Separately (`#general` channel), the community has discussed that the scheduler is cooperative, not preemptive, and that multithreading exists as a framework feature but isn't exercised by the demo kernels.
>
> **Design decision (Issue #9):** direct inspection of `Mosa.Kernel.BareMetal.x86/IDT.cs` confirmed the timer driver can be registered as a separate device (`IRQ = 0` via `DeviceService`/`ISADeviceDriverRegistryEntry`), running alongside — not instead of — `Scheduler.ClockInterrupt`, without modifying MOSA itself. This decouples the timer's *code* from the Scheduler's, though both still share the PIT's single hardware channel-0 frequency (a physical constraint of the 8253/8254 chip, unrelated to CPU architecture or the BIOS). Full detail, including a documented residual risk tied to future MOSA versions, in Issue #9.
>
> **Design reference (not source):** a parallel investigation of Cosmos OS's `Cosmos.HAL/PIT.cs` (also two independent AI research passes, cross-checked against the canonical xv6 kernel's PIT implementation) confirmed Cosmos uses the same standard I/O ports (0x40/0x43) and mode→LSB→MSB write sequence, and offers a `PITTimer`/`OnTrigger` callback-based software timer layer worth using as inspiration for the same kind of decoupled design confirmed viable above — with a known limitation (fixed ~54.9ms rearm granularity, not reprogrammed to the nearest deadline) worth avoiding. This is inspiration only, not reusable code: Cosmos and MOSA use incompatible low-level abstractions (`Cosmos.Core.IOPort` vs. MOSA's `IOPortReadWrite`), so the two frameworks are not interoperable at that layer. Full technical detail in Issue #9.

- 💭 Process/task management
- 💭 Basic scheduler (cooperative → preemptive)
- ⏳ PIT timer: program a known frequency and take control of the scheduler's clock tick (shared IRQ0/vector 0x20) — #9
- 💭 Inter-process communication
- 💭 Filesystem support (start read-only, simple FS)
- 💭 Device abstraction layer

## Phase 3 — 64-bit Port

- 💭 x64 target evaluation once x86 kernel has scheduler + memory manager + drivers
- 💭 Long mode transition
- 💭 4-level paging
- 💭 Re-validate boot chain (Launcher config, image format handling) for x64

## Phase 4 — Mandrillus Systems Ecosystem (Applications)

Post-kernel-milestone work — requires a working process model and filesystem.

- 💭 Simple text editor
- 💭 Code editor
- 💭 Simple drawing/paint application
- 💭 Native lightweight office suite

---

## Notes

- Tooling: `Mosa.Tool.Launcher` (GUI) and `Mosa.Tool.Launcher.Console` (CLI, single-dash flags e.g. `-platform x86`, `-emulator qemu`, `-destination <path>`).
- Target platform for Phases 0–2: **x86 (32-bit)** — chosen for toolchain maturity, simpler paging, and better-documented protected mode setup within MOSA.
- Tagline: *"Mandrillus Systems — evolution, one bit at a time."*
- **Architecture note:** `Mandrillus.Kernel.x86` is built on the **MOSA BareMetal kernel** (`Mosa.Kernel.BareMetal.x86.dll` / `Mosa.Kernel.BareMetal.dll`, referenced via the `Mosa.Platform.x86` and `Mosa.Platform` packages). BareMetal handles GDT, IDT, physical/virtual memory management, scheduling primitives, HAL, and device driver registration (including keyboard and console/video output) automatically during boot, before any Mandrillus-specific code runs. Mandrillus's original work starts at the application layer — the interactive shell and beyond. See `README.md` for a clear breakdown of inherited vs. original components.
- **PIT/timer research summary (Phase 2, Issue #9):**

  | Source | What it confirmed |
  |---|---|
  | MOSA source (`Source/Mosa.DeviceDriver/ISA/`, static analysis) | No PIT/8253/8254 driver anywhere in MOSA |
  | MOSA source (`Mosa.Kernel.BareMetal`, `Scheduler.cs`, `IDT.cs`) | IRQ0/vector `0x20` already consumed by `Scheduler.ClockInterrupt`, at an unconfigured BIOS/QEMU-default frequency |
  | MOSA source (`Mosa.Kernel.BareMetal.x86/IDT.cs`, direct inspection of `ProcessInterrupt`) | Confirmed a separate device driver can register on `IRQ = 0` via `DeviceService`, running alongside `Scheduler.ClockInterrupt` without modifying MOSA — resolved an earlier contradiction between two research passes |
  | MOSA community, [Discord](https://discord.gg/tRNMn3npsv), public discussion | Interrupt frequency is undocumented; scheduler is cooperative, not preemptive; blocking calls can freeze the whole system |
  | Cosmos OS source (`Cosmos.HAL/PIT.cs`), cross-checked vs. xv6 | Confirms canonical port/sequence usage; `PITTimer`/`OnTrigger` callback pattern is a useful design reference, with a known granularity limitation to avoid repeating |

  Full detail and citations in Issue #9.
