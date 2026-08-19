**English** · **[Português (Brasil)](./README.pt-BR.md)**

---

# Mandrillus OS

> A bare-metal operating system written entirely in C#, built on top of the [MOSA Project](https://github.com/mosa/MOSA-Project) — the foundational core of the **Mandrillus Systems** ecosystem.

---

## Why an operating system in C#?

The short answer: most .NET developers never need to think below the runtime layer. The CLR abstracts away memory management, thread scheduling, and hardware access — and that abstraction is exactly what makes C# productive for business applications. But it's also what makes it rare to find .NET developers who understand *what actually happens underneath* that abstraction.

Mandrillus OS exists to close that gap. It's not intended for production use — it's a deliberate exercise in stripping away .NET's comfort layers (runtime-managed garbage collector, full BCL, filesystem, drivers) and rebuilding, piece by piece, exactly what's strictly necessary for a kernel to boot, manage memory, and execute code.

Projects like [Cosmos OS](https://github.com/CosmosOS/Cosmos) and the [MOSA Project](https://github.com/mosa/MOSA-Project) prove this is possible: both compile IL (Intermediate Language) directly into native machine code via AOT (Ahead-of-Time) compilation, removing the dependency on a traditional runtime at execution time. Mandrillus OS uses MOSA as its compilation base and builds its own identity from there — both in terms of the kernel itself and, eventually, the application ecosystem running on top of it.

## What this project demonstrates

This isn't a "CRUD with a database" project. It exists to showcase skills that rarely show up in conventional .NET portfolios:

- **Low-level understanding**: manual memory management, bare-metal boot, no host operating system
- **AOT and IL compilation**: practical understanding of how C# gets translated into machine code, not just JIT at runtime
- **Architecture without a safety net**: no full runtime-managed GC, no mature framework handling exceptions, no full BCL available — design decisions have to be made explicitly
- **Unconventional toolchain**: MOSA, QEMU emulation, bootable disk image generation

## References and inspiration

| Project | What Mandrillus draws from it |
|---|---|
| [MOSA Project](https://github.com/mosa/MOSA-Project) | AOT compilation toolchain (IL → native code), kernel templates, base of the current project |
| [Cosmos OS](https://github.com/CosmosOS/Cosmos) | Conceptual architecture reference — proof that a managed OS in C# is viable in an educational setting |

Mandrillus OS isn't a fork of either — it's built on MOSA as its compilation toolchain, but the kernel architecture, design decisions, and application roadmap are developed independently.

## Current status

🚧 **Early stage** — basic kernel generated from the `mosakrnl` template, with minimal boot ("Hello World") validated on **two different hypervisors**: QEMU and Hyper-V (Generation 1). No functionality beyond minimal boot yet.

Validating boot across more than one hypervisor isn't redundancy — it's evidence of robustness. QEMU (without KVM) is largely a software emulator, more tolerant of bootloader quirks. Hyper-V is a native (type 1) hypervisor, with direct access to CPU virtualization extensions, and historically stricter about what it accepts to boot. Mandrillus OS already runs on both, alongside production operating systems on the same Hyper-V host.

## Architecture (initial view)

```
Mandrillus/                              # Solution
├── Mandrillus.Kernel/                   # Platform-agnostic kernel project
│   ├── Mandrillus.Kernel.csproj
│   ├── Program.cs                       # Entry point, boot options, EntryPoint()
│   └── Boot.cs
├── Mandrillus.Kernel.x86/               # Executable project, x86 target
│   ├── Mandrillus.Kernel.x86.csproj
│   └── bin/
│       └── Tools/                       # Mosa.Tool.Launcher / Launcher.Console
└── README.md
```

The MOSA template (`mosakrnl`) generates this split into two projects: a platform-agnostic kernel (`Mandrillus.Kernel`) and a project per target architecture (`Mandrillus.Kernel.x86`), which references the first and adds platform-specific packages (`Mosa.Platform.x86`).

As the kernel evolves, this section will expand to reflect the separation between the boot layer, memory management, basic drivers (keyboard, video), and the internal API surface that future ecosystem applications will consume.

## The Mandrillus Systems ecosystem

Mandrillus OS is the core of a larger ecosystem of native applications, all planned to run on top of this kernel:

- **Simple text editor** — plain text file editing
- **Code editor** — basic syntax highlighting and editing for development inside the OS itself
- **Drawing application** — simple graphics manipulation, validating the kernel's video layer
- **Native office suite** — a minimal set of productivity tools (text, simple spreadsheet)

Each of these applications will be treated as a separate project within the Mandrillus repository/organization, with its own documentation — but all of them depend directly on the stability and API surface exposed by the kernel documented here.

## Getting started

### Prerequisites

- .NET 10 SDK
- Visual Studio 2022+ (or 2026) recommended for development on Windows; Linux: any editor + the `dotnet` CLI
- [QEMU](https://www.qemu.org/) installed (or use the bundled binaries in `Tools/QEMU`, if present)
- Optional, to test on Hyper-V: Windows with Hyper-V enabled

### ⚠️ Note on MOSA package versions

The MOSA NuGet packages (`Mosa.Platform`, `Mosa.Platform.x86`, `Mosa.DeviceSystem`, `Mosa.Tools.Package`) are **pinned to version `2.6.1.1669`** in this project, instead of using `Version="*"`. This isn't accidental: more recent builds (`2.6.1.1694` onward, including the latest published at the time of writing, `2.6.1.1724`) have a packaging regression that breaks resolution of the `Mosa.Compiler.Platforms` assembly, preventing both `Mosa.Tool.Launcher` (GUI) and `Mosa.Tool.Launcher.Console` from working (`System.IO.FileNotFoundException`).

This issue was isolated through manual bisection across the builds published on NuGet and reported to the MOSA Project community. If the package is fixed in a future version, this note should be revisited and the pin removed.

### Setup

```bash
dotnet new install Mosa.Templates
dotnet new mosakrnl -o Mandrillus
cd Mandrillus
dotnet build
```

After generating the project, adjust the `PackageReference` entries in the `.csproj` files to the pinned version (see note above), then run `dotnet restore` and `dotnet build` again.

### Running in QEMU

From inside the `bin` folder of the x86 project, run the Launcher (GUI or Console) pointing at the compiled DLL:

```powershell
cd Mandrillus.Kernel.x86\bin
Tools\Mosa.Tool.Launcher.exe Mandrillus.Kernel.x86.dll
# or, without a GUI:
Tools\Mosa.Tool.Launcher.Console.exe Mandrillus.Kernel.x86.dll
```

The Launcher compiles the DLL into native x86 code, generates a bootable disk image, and invokes QEMU automatically with the correct arguments.

### Running on Hyper-V (Generation 1)

Mandrillus OS has also been validated running on Hyper-V, alongside other operating systems on the same host. Steps:

1. In the Launcher, change **Image Format** from `IMG (.img)` to `Microsoft (.vhd)` before compiling.
   > ⚠️ In the current MOSA version (`2.6.1.1669`), this option doesn't actually produce a `.vhd` — the pipeline still only outputs `.bin`/`.img`, regardless of the UI selection. Manual conversion is required (next step).
2. Convert the generated `.img` to `.vhd` using `qemu-img` (already available alongside QEMU):
   ```powershell
   qemu-img convert -f raw -O vpc Mandrillus.Kernel.x86.img Mandrillus.Kernel.x86.vhd
   ```
   Alternatively, tools like [StarWind V2V Converter](https://www.starwindsoftware.com/starwind-v2v-converter) also handle this conversion.
3. In **Hyper-V Manager**, create a new VM as **Generation 1**, with Secure Boot disabled, and attach the generated `.vhd` as the boot disk.
4. Start the VM — the kernel should boot and display debug/console output, the same way it does in QEMU.

## License

The code in this repository is licensed under the [MIT License](./LICENSE).

Mandrillus OS depends on the [MOSA Project](https://github.com/mosa/MOSA-Project) (New BSD License) as its compilation toolchain. That license's terms, including the copyright notice required for redistribution, are reproduced in [THIRD-PARTY-LICENSES.md](./THIRD-PARTY-LICENSES.md).

---

*Mandrillus Systems — evolution, one bit at a time.*
