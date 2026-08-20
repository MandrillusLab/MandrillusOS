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

## Phase 1 — Kernel Core (x86, 32-bit)

Tracked as GitHub Issues under the **"Phase 1 — Kernel Core"** milestone.

- ⏳ GDT (Global Descriptor Table) setup — #1
- ⏳ IDT (Interrupt Descriptor Table) + basic interrupt handling — #2
- ⏳ Physical memory manager (page allocator) — #3
- ⏳ Virtual memory / paging — #4
- ⏳ Timer interrupt (PIT/IRQ0) — #5
- ⏳ Keyboard driver (IRQ1) — #6
- ⏳ Text-mode video driver (VGA) — #7
- ⏳ Minimal interactive shell (boot + interrupts + keyboard + output tied together) — #8

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

---

2026 - *"Mandrillus Systems — evolution, one bit at a time."*
