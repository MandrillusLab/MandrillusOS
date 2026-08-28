# Estado atual do projeto

[← voltar ao CLAUDE.md](../../CLAUDE.md)

Use este arquivo para não sugerir algo já decidido, já descartado, ou fora de fase. Estado completo de fases/issues fica no [ROADMAP.md](../../ROADMAP.md) — aqui é o resumo de trabalho.

## Prática de Git disciplinado

O Mandrillus também serve para Leandro praticar Git disciplinado como habilidade (não só como necessidade do projeto): branches por feature, PR mesmo trabalhando sozinho, squash merge, mensagens no padrão Conventional Commits. Fluxo e regras completos em [versioning.md](versioning.md).

**Testes automatizados (xUnit) ficam fora do escopo do Mandrillus, decisão fechada:** `Mandrillus.Kernel` compila contra `Mosa.Korlib` (corlib restrito), que não roda sob xUnit/.NET host normal. O próprio MOSA Project não usa xUnit convencional para testar código bare-metal — usa o tooling de teste próprio dele (`Mosa.UnitTests`, compilado e executado bare-metal/emulado). Mandrillus usa esse tooling do MOSA como está, sem arquitetura de teste própria. Prática de xUnit de verdade fica reservada para outro projeto, com runtime .NET completo — não misturar essa frente aqui.

## Fase atual

Fase 1 fechada.

- ✅ Herdados do MOSA BareMetal, não são trabalho original: GDT, IDT, gerência de memória, driver de teclado, saída de console/vídeo (Issues #1–4, #6–7)
- ✅ **Issue #8 (shell Drill) — fechada.** Único entregável original da Fase 1, concluído.
- ⏭️ Issue #9 (PIT timer) inicia a Fase 2 — design fechado e revalidado, implementação **não iniciada**

## Issue #8 — Drill

Arquitetura já definida, sendo digitada manualmente linha a linha (escolha deliberada, para internalizar estrutura e raciocínio). **Uma IA em modo chat não deve gerar o arquivo inteiro** — o valor está em discutir/explicar trechos.

- Loop REPL não-bloqueante com `HAL.Yield()`
- Registro de comandos via duas `List<T>` paralelas (não `Dictionary`, ver [constraints.md](constraints.md#korlib))
- Buffer circular de histórico (32 entradas), navegação por setas via `KeyType`, supressão de duplicatas consecutivas
- Builtins: `help`, `clear`, `echo`, `history`
- Localização: `Drill.cs` / `DrillCommand.cs` em **`Mandrillus.Kernel`**, namespace `Mandrillus.Kernel.Shell`, pasta `Shell/` — confirmado platform-agnostic
- `Drill.Start()` chamado a partir de `EntryPoint()` em `Program.cs`, não de `Boot.cs`

**Status: fechada, com correção pós-fechamento.** Implementação completa. Um bug foi encontrado depois de fechada: `echo` com argumentos travava o sistema por causa de uma falha silenciosa do `Array.Copy` no `Mosa.Korlib` (ver [constraints.md](constraints.md#korlib)). Corrigido substituindo `Array.Copy` por loop manual em `Dispatch()`, dentro de `Drill.cs`. Nenhum outro problema conhecido na implementação do Drill até o momento.

## Issue #9 — PIT {#issue-9-pit}

Decisão de design **fechada**, revalidada contra o `master` atual do MOSA antes do início da implementação (ver [constraints.md](constraints.md#hardware-do-pit-fatos-não-decisão-de-projeto) para o detalhe da reconfirmação). **Opção B escolhida:** driver de timer separado com `IRQ=0` via `DeviceService`/`ISADeviceDriverRegistryEntry`, rodando ao lado (não substituindo) `Scheduler.ClockInterrupt`. Ambos disparam a cada IRQ0 via `DeviceService.IRQDispatch` (`List<Device>[]`, suporta múltiplos handlers por IRQ).

- Inspiração de design: `Cosmos.HAL/PIT.cs` (portas canônicas, sequência modo→LSB→MSB) — **evitar** a limitação do Cosmos de sempre rearmar para 65535 em vez de reprogramar para a deadline mais próxima (cap de granularidade ~54.9ms)
- Reverificado contra `Source/Mosa.Kernel.BareMetal.x86/IDT.cs` e `Source/Mosa.DeviceSystem/Services/DeviceService.cs` — nenhuma mudança relevante desde a investigação original; decisão continua tecnicamente válida
- `ISADeviceDriverRegistryEntry.AutoLoad` não precisa ser configurado — confirmado que essa propriedade não é lida em nenhum lugar do fluxo de start automático atual (ver constraints.md)
- Reverificar novamente contra `IDT.cs`/`DeviceService.cs` a cada bump de versão do MOSA

**Status: design revalidado, implementação ainda não iniciada.**

**Proveniência da investigação:** a tabela completa de fontes (código MOSA, discussões do Discord, Cosmos+xv6) que embasou essa decisão, e a seção de atribuição de design (por que cada referência foi consultada e por que não foi apenas copiada), estão documentadas no [ROADMAP.md](../../ROADMAP.md) e no [README.md](../../README.md#design-references-credit-where-its-due) — confirmado presente e correto no `master` atual, não duplicado aqui para evitar desatualização entre os dois lugares.

## Filesystem (issue futura, pós #8 e #9)

`Mosa.FileSystem` (FAT + VFS) já disponível transitivamente via `Mosa.Kernel.BareMetal` — sem NuGet novo. Falta: driver IDE em `Boot.cs`, descoberta de partição via `DeviceService.GetDevices<IPartitionDevice>()`, `FatFileSystem`/`FatFileStream`, imagem de boot formatada em FAT. Padrão de referência: `Source/Mosa.BareMetal.CoolWorld/Console/Apps/ShowFS.cs`. Provável Fase 2/3; desbloqueia recursos avançados do Drill (pipes, scripting) hoje deliberadamente adiados.

## Adiamentos deliberados (não esquecimentos)

- **Separação app/projeto:** quando existir uma segunda aplicação real além do Drill, separar em projetos próprios (`Mandrillus.Drill` ou `Mandrillus.App.*`) referenciando `Mandrillus.Kernel`/`Mandrillus.Kernel.x86`, ecoando o padrão App/Shell do CoolWorld. Setup de projeto único atual é intencional.
- **F# como DSL/scripting** para futuras apps Mandrillus Systems — atrelado a fases de infraestrutura futuras, não discutir em termos de implementação agora.

## Planejado, não iniciado

Website/blog Mandrillus Systems: ASP.NET Core, C# ou F#. Artigos de KB separados por problema, frontmatter estruturado para metadados de versão machine-parseable.
