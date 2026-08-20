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

> **Note:** Investigation confirmed (via [MOSA docs](https://www.mosa-project.org/a-dive-into-baremetal.html) and the actual boot log) that most of Phase 1's low-level infrastructure is already provided by the MOSA BareMetal kernel that `Mandrillus.Kernel.x86` builds on. Items below marked *(inherited)* are not original work — they're documented here for traceability and to make the architecture explicit.

- ✅ GDT (Global Descriptor Table) setup *(inherited from MOSA BareMetal)* — #1
- ✅ IDT (Interrupt Descriptor Table) + basic interrupt handling *(inherited from MOSA BareMetal)* — #2
- ✅ Physical memory manager (page allocator) *(inherited from MOSA BareMetal)* — #3
- ✅ Virtual memory / paging *(inherited from MOSA BareMetal)* — #4
- ⏳ Timer interrupt (PIT/IRQ0) — investigation in progress, may also be inherited — #5
- ✅ Keyboard driver *(inherited from MOSA BareMetal — `StandardKeyboard`)* — #6
- ✅ Text-mode video driver *(inherited from MOSA BareMetal — `Console` API)* — #7
- ⏳ Minimal interactive shell (original work — ties keyboard + console output into a command loop) — #8

## Phase 2 — System Services

- 💭 Process/task management
- 💭 Basic scheduler (cooperative → preemptive)
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
