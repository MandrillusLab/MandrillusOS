# Estado atual do projeto

[← voltar ao CLAUDE.md](../../CLAUDE.md)

Use este arquivo para não sugerir algo já decidido, já descartado, ou fora de fase. Estado completo de fases/issues fica no [ROADMAP.md](../../ROADMAP.md) — aqui é o resumo de trabalho.

## Fase atual

Fase 1 quase fechada.

- ✅ Herdados do MOSA BareMetal, não são trabalho original: GDT, IDT, gerência de memória, driver de teclado, saída de console/vídeo (Issues #1–4, #6–7)
- 🔨 **Issue #8 (shell Drill) — único entregável original restante da Fase 1**
- ⏭️ Issue #9 (PIT timer) inicia a Fase 2 — design já fechado, implementação não iniciada

## Issue #8 — Drill

Arquitetura já definida, sendo digitada manualmente linha a linha (escolha deliberada, para internalizar estrutura e raciocínio). **Uma IA em modo chat não deve gerar o arquivo inteiro** — o valor está em discutir/explicar trechos.

- Loop REPL não-bloqueante com `HAL.Yield()`
- Registro de comandos via duas `List<T>` paralelas (não `Dictionary`, ver [constraints.md](constraints.md#korlib))
- Buffer circular de histórico (32 entradas), navegação por setas via `KeyType`, supressão de duplicatas consecutivas
- Builtins: `help`, `clear`, `echo`, `history`
- Localização: `Drill.cs` / `DrillCommand.cs` em **`Mandrillus.Kernel`**, namespace `Mandrillus.Kernel.Shell`, pasta `Shell/` — confirmado platform-agnostic
- `Drill.Start()` chamado a partir de `EntryPoint()` em `Program.cs`, não de `Boot.cs`

## Issue #9 — PIT {#issue-9-pit}

Decisão de design **fechada**, implementação pendente. **Opção B escolhida:** driver de timer separado com `IRQ=0` via `DeviceService`/`ISADeviceDriverRegistryEntry`, rodando ao lado (não substituindo) `Scheduler.ClockInterrupt`. Ambos disparam a cada IRQ0 via `DeviceService.IRQDispatch` (`List<Device>[]`, suporta múltiplos handlers por IRQ).

- Inspiração de design: `Cosmos.HAL/PIT.cs` (portas canônicas, sequência modo→LSB→MSB) — **evitar** a limitação do Cosmos de sempre rearmar para 65535 em vez de reprogramar para a deadline mais próxima (cap de granularidade ~54.9ms)
- Reverificar contra `IDT.cs` a cada bump de versão do MOSA

## Filesystem (issue futura, pós #8 e #9)

`Mosa.FileSystem` (FAT + VFS) já disponível transitivamente via `Mosa.Kernel.BareMetal` — sem NuGet novo. Falta: driver IDE em `Boot.cs`, descoberta de partição via `DeviceService.GetDevices<IPartitionDevice>()`, `FatFileSystem`/`FatFileStream`, imagem de boot formatada em FAT. Padrão de referência: `Source/Mosa.BareMetal.CoolWorld/Console/Apps/ShowFS.cs`. Provável Fase 2/3; desbloqueia recursos avançados do Drill (pipes, scripting) hoje deliberadamente adiados.

## Adiamentos deliberados (não esquecimentos)

- **Separação app/projeto:** quando existir uma segunda aplicação real além do Drill, separar em projetos próprios (`Mandrillus.Drill` ou `Mandrillus.App.*`) referenciando `Mandrillus.Kernel`/`Mandrillus.Kernel.x86`, ecoando o padrão App/Shell do CoolWorld. Setup de projeto único atual é intencional.
- **F# como DSL/scripting** para futuras apps Mandrillus Systems — atrelado a fases de infraestrutura futuras, não discutir em termos de implementação agora.

## Planejado, não iniciado

Website/blog Mandrillus Systems: ASP.NET Core, C# ou F#. Artigos de KB separados por problema, frontmatter estruturado para metadados de versão machine-parseable.
